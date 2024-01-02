/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.common;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.IOException;
import java.net.JarURLConnection;
import java.net.URL;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.Enumeration;
import java.util.LinkedHashSet;
import java.util.Set;
import java.util.function.Predicate;
import java.util.jar.JarEntry;
import java.util.jar.JarFile;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class ClassScanner {

    private static final Logger logger = LoggerFactory.getLogger(ClassScanner.class);

    private ClassScanner() {

    }

    /**
     * 从包package中获取所有的Class
     *
     * @param pkgName java包名,eg: com.wjybxx.game
     * @return classSet
     */
    public static Set<Class<?>> findAllClass(String pkgName) {
        return findClasses(pkgName, f -> true, f -> true);
    }

    /**
     * 加载指定包下符合条件的class
     *
     * @param pkgName         java包名,eg: com.wjybxx.game
     * @param classNameFilter 过滤要加载的类，避免加载过多无用的类 test返回true的才会加载
     * @param classFilter     对加载后的类进行再次确认 test返回true的才会添加到结果集中
     * @return 符合过滤条件的class文件
     */
    public static Set<Class<?>> findClasses(String pkgName, Predicate<String> classNameFilter, Predicate<Class<?>> classFilter) {
        //第一个class类的集合
        Set<Class<?>> classes = new LinkedHashSet<>();
        // 获取文件路径
        String pkgDirName = pkgName.replace('.', '/');
        try {
            ClassLoader classLoader = Thread.currentThread().getContextClassLoader();
            Enumeration<URL> urls = classLoader.getResources(pkgDirName);
            while (urls.hasMoreElements()) {
                URL url = urls.nextElement();
                String protocol = url.getProtocol();
                if ("file".equals(protocol)) {
                    // 如果是普通文件 file:
                    String filePath = URLDecoder.decode(url.getFile(), StandardCharsets.UTF_8);
                    findClassesByFile(classLoader, pkgName, filePath, classes, classNameFilter, classFilter);
                } else if ("jar".equals(protocol)) {
                    // 如果是jar包文件 jar:
                    JarFile jar = ((JarURLConnection) url.openConnection()).getJarFile();
                    findClassesByJar(classLoader, pkgName, jar, classes, classNameFilter, classFilter);
                }
            }
        } catch (IOException e) {
            logger.warn("scanner pkgName {} caught exception.", pkgName, e);
        }
        return classes;
    }

    /**
     * 从文件夹中加载class文件
     *
     * @param classLoader     类加载器
     * @param pkgName         java包名
     * @param pkgPath         文件夹路径
     * @param classes         结果输出
     * @param classNameFilter 过滤要加载的类，避免加载过多无用的类
     * @param classFilter     对加载后的类进行再次确认
     */
    private static void findClassesByFile(ClassLoader classLoader, String pkgName, String pkgPath, Set<Class<?>> classes,
                                          Predicate<String> classNameFilter, Predicate<Class<?>> classFilter) {
        // 获取此包的目录 建立一个File
        File dir = new File(pkgPath);
        if (!dir.exists() || !dir.isDirectory()) {
            return;
        }

        // 只接受文件夹和class文件
        File[] dirFiles = dir.listFiles(file -> file.isDirectory() || file.getName().endsWith(".class"));
        if (dirFiles == null) {
            return;
        }

        // 循环所有文件
        for (File file : dirFiles) {
            // 如果是目录 则继续扫描
            if (file.isDirectory()) {
                if (isHiddenFolder(file.getName())) {
                    continue;
                }
                findClassesByFile(classLoader, pkgName + "." + file.getName(), pkgPath + "/" + file.getName(), classes,
                        classNameFilter, classFilter);
                continue;
            }
            // 如果是java类文件 去掉后面的.class 只留下类名
            String className = pkgName + "." + file.getName().substring(0, file.getName().length() - ".class".length());

            // 不需要加载
            if (!classNameFilter.test(className)) {
                continue;
            }
            //加载类
            try {
                Class<?> clazz = classLoader.loadClass(className);
                if (classFilter.test(clazz)) {
                    classes.add(clazz);
                }
            } catch (ClassNotFoundException e) {
                logger.warn("load class {} from file {} caught exception", className, file.getAbsolutePath(), e);
            }
        }
    }

    /**
     * 是否是隐藏文件夹
     * 一般情况下，"."开头的文件被认为是隐藏文件夹，如：.svn, .git
     */
    private static boolean isHiddenFolder(String folderName) {
        return folderName.charAt(0) == '.';
    }

    /**
     * 从jar包中搜索class文件
     *
     * @param classLoader     类加载器
     * @param pkgName         java包名
     * @param jar             jar包对象
     * @param classes         结果输出
     * @param classNameFilter 过滤要加载的类，避免加载过多无用的类
     * @param classFilter     对加载后的类进行再次确认
     */
    private static void findClassesByJar(ClassLoader classLoader, String pkgName, JarFile jar, Set<Class<?>> classes,
                                         Predicate<String> classNameFilter, Predicate<Class<?>> classFilter) {
        // 这里需要 + "/"，避免startWith判断错误的情况
        final String pkgDir = pkgName.replace(".", "/") + "/";
        final Enumeration<JarEntry> entry = jar.entries();

        // 同样的进行循环迭代
        while (entry.hasMoreElements()) {
            // 获取jar里的一个实体，可以是目录和一些jar包里的其他文件，如META-INF等文
            JarEntry jarEntry = entry.nextElement();

            // jar包中的文件夹
            if (jarEntry.isDirectory()) {
                continue;
            }
            // jar包中的文件
            String name = jarEntry.getName();
            // 使用startWith一定要加"/"，否则可能出现错误
            if (!name.startsWith(pkgDir) || !name.endsWith(".class")) {
                continue;
            }
            // 如果是一个.class文件，去掉后面的".class" 获取真正的类名
            String className = name.substring(0, name.length() - 6).replace("/", ".");

            // 不需要加载
            if (!classNameFilter.test(className)) {
                continue;
            }

            //加载类
            try {
                Class<?> clazz = classLoader.loadClass(className);
                if (classFilter.test(clazz)) {
                    classes.add(clazz);
                }
            } catch (ClassNotFoundException | NoClassDefFoundError e) {
                logger.warn("load class {} from jar {} caught exception", className, jar.getName(), e);
            }
        }
    }
}