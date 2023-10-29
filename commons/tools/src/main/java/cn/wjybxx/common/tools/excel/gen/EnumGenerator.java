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

import cn.wjybxx.common.tools.util.GenClassUtils;
import com.squareup.javapoet.*;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;
import org.apache.commons.lang3.StringUtils;

import javax.lang.model.element.Modifier;
import java.io.File;
import java.io.IOException;
import java.util.List;

/**
 * 枚举类生成器
 * 暂时禁止枚举关联的数值重复
 *
 * @author wjybxx
 * date - 2023/10/15
 */
public class EnumGenerator {

    private final String javaOutDir;
    private final String javaPackage;
    private final String className;
    private final List<SheetEnumValue> enumValueList;

    /**
     * @param enumValueList 外部若需要保持生成代码的文档型，应在外部根据name或number排序
     */
    public EnumGenerator(String javaOutDir, String javaPackage,
                         String className, List<SheetEnumValue> enumValueList) {
        this.javaOutDir = javaOutDir;
        this.javaPackage = javaPackage;
        this.className = className;
        this.enumValueList = enumValueList;
    }

    public void build() throws IOException {
        GenClassUtils.writeToFile(new File(javaOutDir), buildClass().build(), javaPackage);
    }

    /** public，以允许外部追加实现额外的逻辑 */
    public TypeSpec.Builder buildClass() {
        checkDuplicate();
        TypeSpec.Builder typeBuilder = TypeSpec.enumBuilder(className)
                .addAnnotation(GenClassUtils.ANNOTATION_SERIALIZABLE)
                .addModifiers(Modifier.PUBLIC);

        final ClassName selfClsName = ClassName.get(javaPackage, className);
        // 生成枚举对象常量
        for (SheetEnumValue enumValue : enumValueList) {
            TypeSpec.Builder enumBuilder = TypeSpec.anonymousClassBuilder("$L", enumValue.number);
            if (!StringUtils.isBlank(enumValue.comment)) {
                enumBuilder.addJavadoc(enumValue.comment);
            }
            typeBuilder.addEnumConstant(enumValue.name, enumBuilder.build());
        }
        // number属性
        typeBuilder.addField(TypeName.INT, "number", GenClassUtils.PUBLIC_FINAL);
        {
            typeBuilder.addMethod(MethodSpec.constructorBuilder()
                    .addParameter(TypeName.INT, "number")
                    .addStatement("this.number = number")
                    .build());
            GenClassUtils.implEnumLite(selfClsName, typeBuilder, "number");
        }

        // VALUES, MIN_VALUE, MAX_VALUE
        TypeName valuesType = ParameterizedTypeName.get(GenClassUtils.CLSNAME_LIST, selfClsName);
        typeBuilder.addField(FieldSpec.builder(valuesType, "VALUES", GenClassUtils.PUBLIC_STATIC_FINAL)
                .initializer("VALUE_MAP.values()") // 使用Mapper的缓存，勿调整定义顺序
                .build());
        typeBuilder.addField(FieldSpec.builder(TypeName.INT, "MIN_VALUE", GenClassUtils.PUBLIC_STATIC_FINAL)
                .initializer("$L", ConstantGenerator.minValue(enumValueList))
                .build());
        typeBuilder.addField(FieldSpec.builder(TypeName.INT, "MAX_VALUE", GenClassUtils.PUBLIC_STATIC_FINAL)
                .initializer("$L", ConstantGenerator.maxValue(enumValueList))
                .build());
        return typeBuilder;
    }

    private void checkDuplicate() {
        IntSet valueSet = new IntOpenHashSet(enumValueList.size());
        for (SheetEnumValue enumValue : enumValueList) {
            if (!valueSet.add(enumValue.number)) {
                throw new IllegalArgumentException("enumValue is duplicate, name: %s, number: %d"
                        .formatted(enumValue.name, enumValue.number));
            }
        }
    }

}