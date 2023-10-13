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

package cn.wjybxx.common.tools.util;

import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.annotation.SourceFileRef;
import com.squareup.javapoet.*;
import it.unimi.dsi.fastutil.chars.CharPredicate;

import javax.annotation.Nonnull;
import javax.annotation.processing.Generated;
import javax.lang.model.element.Modifier;
import java.io.File;
import java.io.IOException;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2023/10/9
 */
public class Utils extends ObjectUtils {

    // region 代码生成

    public static final Modifier[] PUBLIC_STATIC = {Modifier.PUBLIC, Modifier.STATIC};
    public static final Modifier[] PUBLIC_STATIC_FINAL = {Modifier.PUBLIC, Modifier.STATIC, Modifier.FINAL};
    public static final Modifier[] PRIVATE_STATIC_FINAL = {Modifier.PRIVATE, Modifier.STATIC, Modifier.FINAL};

    /** 由于生成的代码不能很好的处理泛型等信息，因此需要抑制警告 */
    public static final AnnotationSpec SUPPRESS_UNCHECKED_RAWTYPES = AnnotationSpec.builder(SuppressWarnings.class)
            .addMember("value", "{\"unchecked\", \"rawtypes\", \"unused\"}")
            .build();
    public static final AnnotationSpec SUPPRESS_UNCHECKED = AnnotationSpec.builder(SuppressWarnings.class)
            .addMember("value", "{\"unchecked\", \"unused\"}")
            .build();

    public static final AnnotationSpec ANNOTATION_OVERRIDE = AnnotationSpec.builder(Override.class).build();
    public static final AnnotationSpec ANNOTATION_NONNULL = AnnotationSpec.builder(Nonnull.class).build();

    public static final ClassName CLSNAME_SOURCE_REF = ClassName.get(SourceFileRef.class);
    public static final ClassName CLSNAME_STRING = ClassName.get(String.class);
    public static final TypeName CLSNAME_BYTES = ArrayTypeName.of(TypeName.BYTE);

    public static final ClassName CLSNAME_LIST = ClassName.get(List.class);
    public static final ClassName CLSNAME_ARRAY_LIST = ClassName.get(ArrayList.class);

    public static final ClassName CLSNAME_SET = ClassName.get(Set.class);
    public static final ClassName CLSNAME_HASH_SET = ClassName.get(HashSet.class);
    public static final ClassName CLSNAME_LINKED_SET = ClassName.get(LinkedHashSet.class);

    public static final ClassName CLSNAME_MAP = ClassName.get(Map.class);
    public static final ClassName CLSNAME_HASH_MAP = ClassName.get(HashMap.class);
    public static final ClassName CLSNAME_LINKED_MAP = ClassName.get(LinkedHashMap.class);

    public static final ClassName CLSNAME_SUPPLIER = ClassName.get(Supplier.class);
    public static final ClassName CLSNAME_ARRAYS = ClassName.get(Arrays.class);
    public static final ClassName CLSNAME_OBJECTS = ClassName.get(Objects.class);

    public static final ClassName CLSNAME_FUTURE = ClassName.get(CompletableFuture.class);
    public static final ClassName CLSNAME_STAGE = ClassName.get(CompletionStage.class);

    public static final Map<String, TypeName> primitiveTypeNameMap;
    public static final Map<String, TypeName> boxedTypeNameMap;

    static {
        {
            Map<String, TypeName> tempMap = new LinkedHashMap<>(16);
            tempMap.put("int", TypeName.INT);
            tempMap.put("long", TypeName.LONG);
            tempMap.put("float", TypeName.FLOAT);
            tempMap.put("double", TypeName.DOUBLE);
            tempMap.put("boolean", TypeName.BOOLEAN);
            tempMap.put("short", TypeName.SHORT);
            tempMap.put("byte", TypeName.BYTE);
            tempMap.put("char", TypeName.CHAR);
            primitiveTypeNameMap = Collections.unmodifiableMap(tempMap);
        }
        {
            Map<String, TypeName> tempMap = new LinkedHashMap<>(16);
            tempMap.put("Integer", TypeName.INT.box());
            tempMap.put("Long", TypeName.LONG.box());
            tempMap.put("Float", TypeName.FLOAT.box());
            tempMap.put("Double", TypeName.DOUBLE.box());
            tempMap.put("Boolean", TypeName.BOOLEAN.box());
            tempMap.put("Short", TypeName.SHORT.box());
            tempMap.put("Byte", TypeName.BYTE.box());
            tempMap.put("Character", TypeName.CHAR.box());
            boxedTypeNameMap = Collections.unmodifiableMap(tempMap);
        }
    }

    public static TypeName getTypeName(String name) {
        TypeName typeName = primitiveTypeNameMap.get(name);
        if (typeName != null) {
            return typeName;
        }
        typeName = boxedTypeNameMap.get(name);
        if (typeName != null) {
            return typeName;
        }
        return switch (name) {
            case "string", "String" -> CLSNAME_STRING;
            case "byte[]", "bytes" -> CLSNAME_BYTES;
            case "list", "List" -> CLSNAME_LIST;
            case "map", "Map" -> CLSNAME_MAP;
            case "object", "Object" -> TypeName.OBJECT;
            default -> null;
        };
    }

    /** 查询给定TypeName是否是包装类型 -- 比使用equals快 */
    public static boolean isBoxedType(TypeName typeName) {
        for (TypeName value : boxedTypeNameMap.values()) {
            if (value == typeName) return true;
        }
        return false;
    }

    /** @param cname 类的标准名，import语句格式 */
    public static ClassName classNameOfCanonicalName(String cname) {
        int index = cname.lastIndexOf('.');
        return ClassName.get(cname.substring(0, index), cname.substring(index + 1));
    }

    /** 创建生成器信息注解 */
    public static AnnotationSpec newGeneratorInfoAnnotation(Class<?> generator) {
        return AnnotationSpec.builder(Generated.class)
                .addMember("value", "$S", generator.getName())
                .build();
    }

    /**
     * 将类型写入文件夹
     *
     * @param javaOutDir  java根目录，不包含package
     * @param typeSpec    要写入的类型
     * @param javaPackage java包名
     */
    public static void writeToFile(File javaOutDir, TypeSpec typeSpec, String javaPackage) throws IOException {
        final JavaFile javaFile = JavaFile
                .builder(javaPackage, typeSpec)
                .skipJavaLangImports(true)
                .indent("    ")
                .build();
        javaFile.writeTo(javaOutDir);
    }

    // endregion

    // region 字符查找

    /** 查找第一个非给定char的元素的位置 */
    public static int indexOfNot(CharSequence cs, char c) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = 0; i < length; i++) {
            if (cs.charAt(i) != c) {
                return i;
            }
        }
        return -1;
    }

    /** 查找最后一个非给定char的元素位置 */
    public static int lastIndexOfNot(CharSequence cs, char c) {
        int length = length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = length - 1; i >= 0; i--) {
            if (cs.charAt(i) != c) {
                return i;
            }
        }
        return -1;
    }

    public static int indexOf(CharSequence cs, CharPredicate predicate) {
        return indexOf(cs, predicate, 0);
    }

    public static int indexOf(CharSequence cs, CharPredicate predicate, final int startIndex) {
        if (startIndex < 0) {
            throw new IllegalArgumentException("startIndex " + startIndex);
        }

        int length = length(cs);
        if (length == 0) {
            return -1;
        }

        for (int i = startIndex; i < length; i++) {
            if (predicate.test(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    public static int lastIndexOf(CharSequence cs, CharPredicate predicate) {
        return lastIndexOf(cs, predicate, -1);
    }

    /**
     * @param startIndex 开始下标，-1表示从最后一个字符开始
     * @return -1表示查找失败
     */
    public static int lastIndexOf(CharSequence cs, CharPredicate predicate, int startIndex) {
        if (startIndex < -1) {
            throw new IllegalArgumentException("startIndex " + startIndex);
        }

        int length = length(cs);
        if (length == 0) {
            return -1;
        }

        if (startIndex == -1) {
            startIndex = length - 1;
        } else if (startIndex >= length) {
            startIndex = length - 1;
        }

        for (int i = startIndex; i >= 0; i--) {
            if (predicate.test(cs.charAt(i))) {
                return i;
            }
        }
        return -1;
    }

    // endregion

    /** 去除字符串的双引号 */
    public static String unquote(String str) {
        int length = ObjectUtils.length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        char lastChar = str.charAt(str.length() - 1);
        if (firstChar == '"' && lastChar == '"') {
            return str.substring(1, str.length() - 1);
        }
        return str;
    }

    /** 给字符串添加双引号，若之前无双引号 */
    public static String quote(String str) {
        if (str == null) {
            return null;
        }
        if (str.isEmpty()) {
            return "\"\"";
        }
        char firstChar = str.charAt(0);
        char lastChar = str.charAt(str.length() - 1);
        if (firstChar == '"' && lastChar == '"') {
            return str;
        }
        return '"' + str + '"';
    }

    public static File getUserWorkerDir() {
        return new File(System.getProperty("user.dir"));
    }

    /** 查找项目的根目录 */
    public static File findProjectDir(String projectName) {
        final File workdir = getUserWorkerDir();
        if (workdir.getName().equalsIgnoreCase(projectName)) {
            return workdir;
        }
        File currentDir = workdir;
        File parentFile;
        while ((parentFile = currentDir.getParentFile()) != null) {
            if (parentFile.getName().equalsIgnoreCase(projectName)) {
                return parentFile;
            }
            currentDir = parentFile;
        }
        throw new AssertionError();
    }
}