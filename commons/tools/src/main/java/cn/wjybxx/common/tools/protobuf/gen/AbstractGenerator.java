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

import cn.wjybxx.common.tools.protobuf.PBFile;
import cn.wjybxx.common.tools.protobuf.PBKeywords;
import cn.wjybxx.common.tools.protobuf.PBParserOptions;
import cn.wjybxx.common.tools.protobuf.PBRepository;
import cn.wjybxx.common.tools.util.Utils;
import com.squareup.javapoet.ClassName;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/10/13
 */
abstract class AbstractGenerator {

    protected final PBParserOptions options;
    protected final PBRepository repository;

    public AbstractGenerator(PBParserOptions options, PBRepository repository) {
        this.options = Objects.requireNonNull(options);
        this.repository = Objects.requireNonNull(repository);
    }

    protected ClassName classNameOfType(String type) {
        PBFile pbFile = repository.getFileOfTopElement(type);
        if (pbFile == null) {
            throw new IllegalArgumentException("class not found, type " + type);
        }
        // 处理文件中定义了JavaPackage的情况
        String javaPackage = pbFile.getOption(PBKeywords.JAVA_PACKAGE);
        if (javaPackage != null) {
            javaPackage = Utils.unquote(javaPackage);
        } else {
            javaPackage = options.getJavaPackage();
        }
        if (options.isJavaMultipleFiles()) {
            return ClassName.get(javaPackage, type);
        }
        // 处理文件中定义了OuterClassName的情况
        String outerClassName = pbFile.getOption(PBKeywords.JAVA_OUTER_CLASSNAME);
        if (outerClassName != null) {
            outerClassName = Utils.unquote(outerClassName);
        } else {
            outerClassName = options.getOuterClassName(pbFile.getSimpleName());
        }
        return ClassName.get(javaPackage, outerClassName, type);
    }

}