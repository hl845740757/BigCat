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
import cn.wjybxx.common.tools.protobuf.PBParser;
import cn.wjybxx.common.tools.protobuf.PBParserOptions;
import cn.wjybxx.common.tools.util.Line;
import cn.wjybxx.common.tools.util.Utils;
import org.apache.commons.io.FileUtils;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/**
 * 生成临时pb文件
 *
 * @author wjybxx
 * date - 2023/10/13
 */
public class PBFileGenerator {

    private final PBParserOptions options;
    private final File tempDir;
    private final PBParser parser;

    public PBFileGenerator(PBParserOptions options, File tempDir, PBParser parser) {
        this.options = options;
        this.tempDir = tempDir;
        this.parser = parser;
    }

    public void build() throws IOException {
        PBFile pbFile = parser.getPbFile();
        if (pbFile.getFileName() == null) {
            throw new IllegalStateException("pbFile has not been parsed");
        }
        List<Line> processedLines = parser.getProcessedLines();
        List<String> lines = new ArrayList<>(processedLines.size() + 10);
        lines.add("%s = \"%s\";".formatted(PBKeywords.SYNTAX, pbFile.getSyntax()));
        // 写入import
        for (String fileImport : pbFile.getImports()) {
            lines.add("%s \"%s\";".formatted(PBKeywords.IMPORT, Utils.unquote(fileImport)));
        }
        // 写入公共option
        pbFile.getOptions().forEach((k, v) -> {
            if (PBKeywords.isStringOptionValue(k)) {
                lines.add("%s %s = \"%s\";".formatted(PBKeywords.OPTION, k, Utils.unquote(v)));
            } else {
                lines.add("%s %s = %s;".formatted(PBKeywords.OPTION, k, Utils.unquote(v)));
            }
        });
        // 写入java包和外部类类名
        if (pbFile.getOption(PBKeywords.JAVA_PACKAGE) == null) {
            lines.add("%s %s = \"%s\";".formatted(PBKeywords.OPTION, PBKeywords.JAVA_PACKAGE, options.getJavaPackage()));
        }
        if (options.isJavaMultipleFiles()) {
            lines.add("%s %s = %s;".formatted(PBKeywords.OPTION, PBKeywords.JAVA_MULTIPLE_FILES, "true"));
        } else if (pbFile.getOption(PBKeywords.JAVA_OUTER_CLASSNAME) == null) {
            String outerClassName = options.getOuterClassName(pbFile.getSimpleName());
            lines.add("%s %s = \"%s\";".formatted(PBKeywords.OPTION, PBKeywords.JAVA_OUTER_CLASSNAME, outerClassName));
        }

        // 填充空白行
        for (int i = options.getHeaderLineCount() - lines.size(); i > 0; i--) {
            lines.add("");
        }
        // 其它行直接写入
        for (Line line : processedLines) {
            lines.add(line.data);
        }

        File file = new File(tempDir, pbFile.getFileName());
        FileUtils.writeLines(file, "UTF-8", lines, options.getLineSeparator(), false);
    }

}