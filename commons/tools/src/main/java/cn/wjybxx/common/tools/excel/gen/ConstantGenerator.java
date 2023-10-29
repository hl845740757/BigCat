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

package cn.wjybxx.common.tools.excel.gen;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.tools.util.GenClassUtils;
import com.squareup.javapoet.*;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;
import it.unimi.dsi.fastutil.ints.IntSets;
import org.apache.commons.lang3.StringUtils;

import javax.lang.model.element.Modifier;
import java.io.File;
import java.io.IOException;
import java.util.HashSet;
import java.util.List;
import java.util.Objects;
import java.util.Set;

/**
 * 生成常量类
 *
 * @author wjybxx
 * date - 2023/10/15
 */
public class ConstantGenerator {

    private final String javaOutDir;
    private final String javaPackage;
    private final String className;
    private final List<SheetEnumValue> enumValueList;

    private final boolean stringEnum;
    private final boolean allowAlias;

    /**
     * @param enumValueList 外部若需要保持生成代码的文档型，应在外部根据name或number排序
     * @param stringEnum    枚举的值是否是字符串类型
     * @param allowAlias    是否允许不同的变量指向同一常量值
     */
    public ConstantGenerator(String javaOutDir, String javaPackage,
                             String className, List<SheetEnumValue> enumValueList,
                             boolean stringEnum, boolean allowAlias) {
        this.javaOutDir = javaOutDir;
        this.javaPackage = javaPackage;
        this.className = className;
        this.enumValueList = enumValueList;
        this.stringEnum = stringEnum;
        this.allowAlias = allowAlias;
    }

    public void build() throws IOException {
        GenClassUtils.writeToFile(new File(javaOutDir), buildClass().build(), javaPackage);
    }

    /** public，以允许外部追加实现额外的逻辑 */
    public TypeSpec.Builder buildClass() {
        checkAlias();
        TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(className)
                .addModifiers(Modifier.PUBLIC);

        // 生成常量字段
        genConstantFields(typeBuilder);

        // VALUE_MAP name -> value 的映射
        // VALUE_SET
        // 静态初始块
        if (stringEnum) {
            TypeName valueMapType = ParameterizedTypeName.get(GenClassUtils.CLSNAME_MAP, GenClassUtils.CLSNAME_STRING, GenClassUtils.CLSNAME_STRING);
            typeBuilder.addField(valueMapType, "VALUE_MAP", GenClassUtils.PUBLIC_STATIC_FINAL);

            TypeName valueSetMap = ParameterizedTypeName.get(GenClassUtils.CLSNAME_SET, GenClassUtils.CLSNAME_STRING);
            typeBuilder.addField(valueSetMap, "VALUE_SET", GenClassUtils.PUBLIC_STATIC_FINAL);

            CodeBlock.Builder codeBuilder = CodeBlock.builder();
            // MAP初始化块 -- 代码块包起来
            codeBuilder.beginControlFlow("");
            codeBuilder.addStatement("$T tempMap = new $T<>()", valueMapType, GenClassUtils.CLSNAME_LINKED_MAP);
            for (SheetEnumValue enumValue : enumValueList) {
                codeBuilder.addStatement("tempMap.put($S, $S)", enumValue.name, enumValue.value);
            }
            codeBuilder.addStatement("VALUE_MAP = $T.toImmutableLinkedHashMap(tempMap)", ClassName.get(CollectionUtils.class));
            codeBuilder.endControlFlow();

            // Set初始化块
            codeBuilder.addStatement("VALUE_SET = Set.copyOf(VALUE_MAP.values())");
            typeBuilder.addStaticBlock(codeBuilder.build());
        } else {
            TypeName valueMapType = ParameterizedTypeName.get(GenClassUtils.CLSNAME_MAP, GenClassUtils.CLSNAME_STRING, TypeName.INT.box());
            typeBuilder.addField(valueMapType, "VALUE_MAP", GenClassUtils.PUBLIC_STATIC_FINAL);

            TypeName valueSetType = ClassName.get(IntSet.class);
            typeBuilder.addField(valueSetType, "VALUE_SET", GenClassUtils.PUBLIC_STATIC_FINAL);

            // 额外生成 MIN_VALUE, MAX_VALUE
            {
                typeBuilder.addField(FieldSpec.builder(TypeName.INT, "MIN_VALUE", GenClassUtils.PUBLIC_STATIC_FINAL)
                        .initializer("$L", minValue(enumValueList))
                        .build());
                typeBuilder.addField(FieldSpec.builder(TypeName.INT, "MAX_VALUE", GenClassUtils.PUBLIC_STATIC_FINAL)
                        .initializer("$L", maxValue(enumValueList))
                        .build());
            }

            CodeBlock.Builder codeBuilder = CodeBlock.builder();
            // MAP初始化块
            codeBuilder.beginControlFlow("");
            codeBuilder.addStatement("$T tempMap = new $T<>()", valueMapType, GenClassUtils.CLSNAME_LINKED_MAP);
            for (SheetEnumValue enumValue : enumValueList) {
                codeBuilder.addStatement("tempMap.put($S, $L)", enumValue.name, enumValue.number);
            }
            codeBuilder.addStatement("VALUE_MAP = $T.toImmutableLinkedHashMap(tempMap)", ClassName.get(CollectionUtils.class));
            codeBuilder.endControlFlow();

            // SET初始化块
            codeBuilder.addStatement("VALUE_SET = $T.unmodifiable(new $T(VALUE_MAP.values()))",
                    ClassName.get(IntSets.class),
                    ClassName.get(IntOpenHashSet.class));
            typeBuilder.addStaticBlock(codeBuilder.build());
        }

        // isEmpty size
        typeBuilder.addMethod(MethodSpec.methodBuilder("isEmpty")
                .addModifiers(GenClassUtils.PUBLIC_STATIC)
                .returns(TypeName.BOOLEAN)
                .addStatement("return VALUE_MAP.isEmpty()")
                .build());
        typeBuilder.addMethod(MethodSpec.methodBuilder("size")
                .addModifiers(GenClassUtils.PUBLIC_STATIC)
                .returns(TypeName.INT)
                .addStatement("return VALUE_MAP.size()")
                .build());
        // containsKey
        typeBuilder.addMethod(MethodSpec.methodBuilder("containsKey")
                .addModifiers(GenClassUtils.PUBLIC_STATIC)
                .returns(TypeName.BOOLEAN)
                .addParameter(GenClassUtils.CLSNAME_STRING, "key")
                .addStatement("return VALUE_MAP.containsKey(key)")
                .build());
        // containsValue
        if (stringEnum) {
            typeBuilder.addMethod(MethodSpec.methodBuilder("containsValue")
                    .addModifiers(GenClassUtils.PUBLIC_STATIC)
                    .returns(TypeName.BOOLEAN)
                    .addParameter(GenClassUtils.CLSNAME_STRING, "value")
                    .addStatement("return VALUE_SET.contains(value)")
                    .build());
        } else {
            typeBuilder.addMethod(MethodSpec.methodBuilder("containsValue")
                    .addModifiers(GenClassUtils.PUBLIC_STATIC)
                    .returns(TypeName.BOOLEAN)
                    .addParameter(TypeName.INT, "value")
                    .addStatement("return VALUE_SET.contains(value)")
                    .build());
            // 额外生成 isBetween
            typeBuilder.addMethod(MethodSpec.methodBuilder("isBetween")
                    .addModifiers(GenClassUtils.PUBLIC_STATIC)
                    .returns(TypeName.BOOLEAN)
                    .addParameter(TypeName.INT, "value")
                    .addStatement("if (VALUE_SET.isEmpty()) return false")
                    .addStatement("return value >= MIN_VALUE && value <= MAX_VALUE")
                    .build());
        }
        return typeBuilder;
    }

    private void checkAlias() {
        if (allowAlias || enumValueList.isEmpty()) {
            return;
        }
        if (stringEnum) {
            Set<String> valueSet = new HashSet<>();
            for (SheetEnumValue enumValue : enumValueList) {
                if (enumValue.value == null) {
                    throw new IllegalArgumentException("enumValue cant be null, name: %s"
                            .formatted(enumValue.name));
                }
                if (!valueSet.add(enumValue.value)) {
                    throw new IllegalArgumentException("enumValue is duplicate, name: %s, value: %s"
                            .formatted(enumValue.name, enumValue.value));
                }
            }
        } else {
            IntSet valueSet = new IntOpenHashSet(enumValueList.size());
            for (SheetEnumValue enumValue : enumValueList) {
                if (!valueSet.add(enumValue.number)) {
                    throw new IllegalArgumentException("enumValue is duplicate, name: %s, number: %d"
                            .formatted(enumValue.name, enumValue.number));
                }
            }
        }
    }

    private void genConstantFields(TypeSpec.Builder typeBuilder) {
        if (stringEnum) {
            for (SheetEnumValue enumValue : enumValueList) {
                FieldSpec.Builder fb = FieldSpec.builder(GenClassUtils.CLSNAME_STRING, enumValue.name, GenClassUtils.PUBLIC_STATIC_FINAL)
                        .initializer("$S", Objects.requireNonNull(enumValue.value));
                if (!StringUtils.isBlank(enumValue.comment)) {
                    fb.addJavadoc(enumValue.comment);
                }
                typeBuilder.addField(fb.build());
            }
        } else {
            for (SheetEnumValue enumValue : enumValueList) {
                FieldSpec.Builder fb = FieldSpec.builder(TypeName.INT, enumValue.name, GenClassUtils.PUBLIC_STATIC_FINAL)
                        .initializer("$L", enumValue.number);
                if (!StringUtils.isBlank(enumValue.comment)) {
                    fb.addJavadoc(enumValue.comment);
                }
                typeBuilder.addField(fb.build());
            }
        }
    }

    static int minValue(List<SheetEnumValue> enumValueList) {
        return enumValueList.stream()
                .mapToInt(e -> e.number)
                .min()
                .orElse(-1);
    }

    static int maxValue(List<SheetEnumValue> enumValueList) {
        return enumValueList.stream()
                .mapToInt(e -> e.number)
                .max()
                .orElse(-1);
    }

}