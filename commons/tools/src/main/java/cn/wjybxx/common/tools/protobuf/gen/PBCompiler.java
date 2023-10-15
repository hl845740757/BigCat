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

import cn.wjybxx.common.tools.protobuf.PBParserOptions;
import cn.wjybxx.common.tools.util.Utils;
import org.apache.commons.lang3.StringUtils;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/**
 * 执行protoc编译中间文件
 *
 * @author wjybxx
 * date - 2023/10/13
 */
public class PBCompiler {

    private final PBParserOptions options;

    public PBCompiler(PBParserOptions options) {
        this.options = options;
    }

    public void build() throws IOException {
        String tempDirPath = new File(options.getTempDir()).getCanonicalPath();
        String javaOutDirPath = new File(options.getJavaOut()).getCanonicalPath();

        // 编译某个目录的所有proto文件需要带上文件夹；并不是直接从proto_path搜索文件
        List<String> commands = new ArrayList<>();
        if (!StringUtils.isBlank(options.getProtocPath())) {
            commands.add(options.getProtocPath());
        } else {
            commands.add("protoc");
        }
        commands.add("--java_out=" + javaOutDirPath);
        commands.add("--proto_path=" + tempDirPath);
        commands.add(tempDirPath + File.separator + "*.proto");

        Process process = new ProcessBuilder(commands)
                .redirectErrorStream(true)
                .start();
        try {
            int code = process.waitFor();
            if (code != 0) {
                StringBuilder sb = Utils.readProcessOutput(process);
                throw new IOException(("""
                        protoc compile caught exception,
                        code: %d, tempDir: %s, info:
                        -----------------------------
                        %s
                        -----------------------------""")
                        .formatted(code, tempDirPath, sb.toString()));
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        } finally {
            process.destroy();
        }
    }
}
