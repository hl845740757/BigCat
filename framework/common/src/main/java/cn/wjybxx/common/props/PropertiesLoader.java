/*
 * Copyright 2023 wjybxx(845740757@qq.com)
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

package cn.wjybxx.common.props;

import cn.wjybxx.base.CollectionUtils;

import javax.annotation.Nonnull;
import java.io.*;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.Objects;
import java.util.Properties;
import java.util.Set;

/**
 * Q：命名空间？
 * A：在实际的开发过程中，我们可能将许多配置配置在一个文件中；在我的工作中，我一般通过类似java包的方式来进行区分，即：{@code a.b.c.d } 的格式。
 * 按照这种规则，我们可以为特定路径下的配置创建一个视图，更方便的读取属性。
 *
 * @author wjybxx
 * date 2023/4/14
 */
public class PropertiesLoader {

    // region

    /** 通过Map创建Properties，该方法会拷贝Map的内容 */
    public static IProperties ofMap(Map<String, String> properties) {
        return PropertiesImpl.ofMap(properties);
    }

    /** 通过Map创建Properties，该方法会拷贝Properties的内容 */
    public static IProperties ofProperties(Properties properties) {
        return PropertiesImpl.ofProperties(properties);
    }

    /**
     * 直接代理给定的{@link Map}，而不是进行拷贝
     * 仅仅是为Map提供一个{@link IProperties}视图
     */
    public static IProperties wrapMap(Map<String, String> properties) {
        return PropertiesImpl.wrapMap(properties);
    }

    /**
     * 直接代理给定的{@link Properties}，而不是进行拷贝
     * 注意：{@link Properties}是同步容器，查询开销较大，尤其是查询所有的key。
     */
    public static IProperties wrapProperties(Properties properties) {
        return new PropertiesWrapper(properties);
    }

    /**
     * 过滤配置文件，只获取指定命名空间的内容
     * <pre>
     * partialOf("a.b.c.d=value", "a.b.c")   "d=value"
     * </pre>
     *
     * @param origin    原始的配置文件
     * @param namespace 期望的命名空间
     * @return 只包含指定命名空间参数的配置对象
     */
    public static IProperties partialOf(IProperties origin, String namespace) {
        final String filterKey = namespace + ".";
        final Set<String> nameSet = origin.keySet();
        final Map<String, String> copied = CollectionUtils.newLinkedHashMap(nameSet.size());
        nameSet.stream()
                .filter(k -> k.startsWith(filterKey))
                .forEach(k -> copied.put(k.substring(filterKey.length()), origin.getAsString(k)));
        return PropertiesImpl.wrapMap(copied);
    }

    public static IProperties partialOf(Properties origin, String namespace) {
        return partialOf(wrapProperties(origin), namespace);
    }

    //

    /**
     * 加载配置文件中的所有内容
     *
     * @param filePath 文件路径
     */
    public static IProperties load(String filePath) throws IOException {
        final Properties properties = loadRawPropertiesFromFile(filePath);
        return PropertiesImpl.ofProperties(properties);
    }

    /**
     * 加载指定命名空间的配置
     * <pre>
     * loadPartial("a.b.c.d=value", "a.b.c")   "d=value"
     * </pre>
     *
     * @param filePath  文件路径
     * @param namespace 要读取的命名空间
     * @return 只包含指定命名空间参数的配置对象
     */
    public static IProperties loadPartial(String filePath, String namespace) throws IOException {
        final Properties properties = loadRawPropertiesFromFile(filePath);
        return partialOf(wrapProperties(properties), namespace);
    }

    //

    /** 从普通文件中读取原始的配置 */
    public static Properties loadRawPropertiesFromFile(@Nonnull String path) throws IOException {
        Objects.requireNonNull(path);
        File file = new File(path);
        return loadRawPropertiesFromFile(file);
    }

    public static Properties loadRawPropertiesFromFile(@Nonnull File file) throws IOException {
        Objects.requireNonNull(file);
        if (file.exists() && file.isFile()) {
            try (FileInputStream fileInputStream = new FileInputStream(file);
                 InputStreamReader inputStreamReader = new InputStreamReader(fileInputStream, StandardCharsets.UTF_8)) {
                Properties properties = new Properties();
                properties.load(inputStreamReader);
                return properties;
            }
        }
        throw new FileNotFoundException(file.getPath());
    }

    /** 从jar包读取配置原始的配置 */
    public static Properties loadRawPropertiesFromJar(String path) throws IOException {
        return loadRawPropertiesFromJar(path, PropertiesLoader.class.getClassLoader());
    }

    public static Properties loadRawPropertiesFromJar(String path, ClassLoader classLoader) throws IOException {
        final URL resource = classLoader.getResource(path);
        if (resource == null) {
            throw new FileNotFoundException(path);
        }
        try (InputStream inputStream = resource.openStream()) {
            Properties properties = new Properties();
            properties.load(inputStream);
            return properties;
        }
    }

}