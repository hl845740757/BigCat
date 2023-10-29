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

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.EnumLite;
import cn.wjybxx.common.EnumLiteMap;
import cn.wjybxx.common.EnumUtils;
import cn.wjybxx.common.annotation.SourceFileRef;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import com.squareup.javapoet.*;

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
 * 用于生成Java文件的工具类
 *
 * @author wjybxx
 * date - 2023/10/15
 */
public class GenClassUtils {

    public static final Modifier[] PUBLIC_STATIC = {Modifier.PUBLIC, Modifier.STATIC};
    public static final Modifier[] PUBLIC_FINAL = {Modifier.PUBLIC, Modifier.FINAL};
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
    public static final AnnotationSpec ANNOTATION_SERIALIZABLE = AnnotationSpec.builder(BinarySerializable.class).build();

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

    public static final ClassName clsName_enumLite = ClassName.get(EnumLite.class);
    public static final ClassName clsName_enumLiteMap = ClassName.get(EnumLiteMap.class);
    public static final ClassName clsName_enumUtils = ClassName.get(EnumUtils.class);

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

    /**
     * 生成的类实现{@link EnumLite}接口
     * 1.生成的{@link EnumLiteMap}字段命名为{@code VALUE_MAP}
     *
     * @param numberFiledName number字段的名字；部分类可能不命名为number，以更贴近业务
     */
    public static void implEnumLite(ClassName className, TypeSpec.Builder typeBuilder, String numberFiledName) {
        if (!CollectionUtils.containsRef(typeBuilder.superinterfaces, clsName_enumLite)) {
            typeBuilder.addSuperinterface(clsName_enumLite);
        }

        typeBuilder.addMethod(MethodSpec.methodBuilder("getNumber")
                .addAnnotation(ANNOTATION_OVERRIDE)
                .returns(TypeName.INT)
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addStatement("return " + numberFiledName)
                .build());

        // mapper字段
        final TypeName mapperTypeName = ParameterizedTypeName.get(clsName_enumLiteMap, className);
        typeBuilder.addField(FieldSpec.builder(mapperTypeName, "VALUE_MAP", PUBLIC_STATIC_FINAL)
                .initializer("$T.mapping(values())", clsName_enumUtils)
                .build());

        // 三个代理方法
        typeBuilder.addMethod(MethodSpec.methodBuilder("forNumber")
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC)
                .returns(className)
                .addParameter(TypeName.INT, "number")
                .addStatement("return VALUE_MAP.forNumber(number)")
                .build());
        typeBuilder.addMethod(MethodSpec.methodBuilder("forNumber")
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC)
                .returns(className)
                .addParameter(TypeName.INT, "number")
                .addParameter(className, "def")
                .addStatement("return VALUE_MAP.forNumber(number, def)")
                .build());
        typeBuilder.addMethod(MethodSpec.methodBuilder("checkedForNumber")
                .returns(className)
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC)
                .addParameter(TypeName.INT, "number", Modifier.FINAL)
                .addStatement("return VALUE_MAP.checkedForNumber(number)")
                .build());
    }
}