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

package cn.wjybxx.common.tools.protobuf.gen;

import cn.wjybxx.common.pb.PBMethodParser;
import cn.wjybxx.common.pb.PBMethodParserRegistry;
import cn.wjybxx.common.tools.protobuf.*;
import cn.wjybxx.common.tools.util.Utils;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.CodeBlock;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.Modifier;
import java.io.File;
import java.io.IOException;

/**
 * 生成辅助类，辅助类中注册所有的{@link PBMethodParser}到{@link PBMethodParserRegistry}
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public class ParserExporterGenerator extends AbstractGenerator {

    /** 生成的类名 */
    private static final String CLASS_NAME = "PBMethodParserExporter";

    private final boolean isProto2;

    public ParserExporterGenerator(PBParserOptions options, PBRepository repository) {
        super(options, repository);
        this.isProto2 = options.isProto2();
    }

    public void build() throws IOException {
        MethodSpec.Builder exportBuilder = MethodSpec.methodBuilder("export")
                .addModifiers(Utils.PUBLIC_STATIC)
                .addParameter(PBMethodParserRegistry.class, "registry");
        ClassName className_methodParser = ClassName.get(PBMethodParser.class);

        for (PBFile pbFile : repository.getSortedFiles()) {
            for (PBService service : pbFile.getServices()) {
                for (PBMethod method : service.getMethods()) {
                    CodeBlock.Builder codeBuilder = CodeBlock.builder().add("registry.register(new $T<>($L, $L,",
                            className_methodParser,
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
                .addAnnotation(Utils.newGeneratorInfoAnnotation(getClass()))
                .addMethod(exportBuilder.build())
                .build();
        Utils.writeToFile(new File(options.getJavaOut()), typeSpec, options.getJavaPackage());
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