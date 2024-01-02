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

package cn.wjybxx.bigcat.protobuf.gen;

import cn.wjybxx.bigcat.protobuf.*;
import cn.wjybxx.bigcat.tools.GenClassUtils;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.CodeBlock;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.Modifier;
import java.io.File;
import java.io.IOException;

/**
 * 生成辅助类，辅助类中注册所有的{@code  PBMethodInfo}到{@code PBMethodInfoRegistry}
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public class MethodInfoExporterGenerator extends AbstractGenerator {

    /** 生成的类名 */
    private static final String CLASS_NAME = "PBMethodInfoExporter";
    private static final ClassName CLSNAME_METHOD_REGISTRY = GenClassUtils.classNameOfCanonicalName("cn.wjybxx.bigcat.pb.PBMethodInfoRegistry");
    private static final ClassName CLSNAME_METHOD_INFO = GenClassUtils.classNameOfCanonicalName("cn.wjybxx.bigcat.pb.PBMethodInfo");

    private final boolean isProto2;

    public MethodInfoExporterGenerator(PBParserOptions options, PBRepository repository) {
        super(options, repository);
        this.isProto2 = options.isProto2();
    }

    public void build() throws IOException {
        MethodSpec.Builder exportBuilder = MethodSpec.methodBuilder("export")
                .addModifiers(GenClassUtils.PUBLIC_STATIC)
                .addParameter(CLSNAME_METHOD_REGISTRY, "registry");

        for (PBFile pbFile : repository.getSortedFiles()) {
            for (PBService service : pbFile.getServices()) {
                for (PBMethod method : service.getMethods()) {
                    // 增加一行注释，既用于分割代码，也方便检索
                    exportBuilder.addComment("$L.$L", service.getSimpleName(), method.getSimpleName());

                    CodeBlock.Builder codeBuilder = CodeBlock.builder().add("registry.register(new $T<>($L, $L,",
                            CLSNAME_METHOD_INFO,
                            service.getServiceId(),
                            method.getMethodId());
                    codeBuilder.add("\n");
                    addParserStatement(method.getArgType(), codeBuilder);
                    codeBuilder.add(",\n");
                    addParserStatement(method.getResultType(), codeBuilder);
                    codeBuilder.add("))");

                    exportBuilder.addStatement(codeBuilder.build());
                }
            }
        }

        TypeSpec typeSpec = TypeSpec.classBuilder(CLASS_NAME)
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(GenClassUtils.newGeneratorInfoAnnotation(getClass()))
                .addMethod(exportBuilder.build())
                .build();
        GenClassUtils.writeToFile(new File(options.getJavaOut()), typeSpec, options.getJavaPackage());
    }

    private CodeBlock.Builder addParserStatement(String type, CodeBlock.Builder codeBuilder) {
        if (type == null) {
            return codeBuilder.add("null, null");
        }
        ClassName className = classNameOfType(type);
        if (isProto2) {
            // 该语句其实兼容proto2和3，但太长
            return codeBuilder.add("$T.class, $T.getDefaultInstance().getParserForType()",
                    className,
                    className);
        } else {
            return codeBuilder.add("$T.class, $T.parser()",
                    className,
                    className);
        }
    }

}