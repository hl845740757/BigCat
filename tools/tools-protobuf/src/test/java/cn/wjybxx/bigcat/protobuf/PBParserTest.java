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

package cn.wjybxx.bigcat.protobuf;

import cn.wjybxx.bigcat.protobuf.gen.MethodInfoExporterGenerator;
import cn.wjybxx.bigcat.protobuf.gen.ServiceGenerator;
import cn.wjybxx.bigcat.tools.TestUtil;
import org.apache.commons.io.FileUtils;

import java.io.File;
import java.io.IOException;
import java.util.Collection;

/**
 * 可运行该测试，查看生成的代码
 *
 * @author wjybxx
 * date - 2023/10/9
 */
public class PBParserTest {

    /** 该测试部走自动化测试 */
    public static void main(String[] args) throws IOException {
        PBParserOptions options = new PBParserOptions();
        options.setProtoDir(TestUtil.testResPath)
                .setTempDir(TestUtil.testResPath + "/temp/")
                .setLineSeparator("\n")
                .setHeaderLineCount(5)
                .addCommon("common")
                .setJavaMultipleFiles(true);

        String modulePath = TestUtil.bigcatPath + "/tools/tools-protobuf";
        options.setJavaOut(modulePath + "/src/test/java")
                .setJavaPackage("cn.wjybxx.bigcat.temp.pb1");

        options.setMethodDefMode(PBMethod.MODE_CONTEXT)
                .setUseCompleteStage(false)
                .setServiceInterceptor(service -> {
                    service.addSuperinterface("cn.wjybxx.bigcat.rpc.ExtensibleService");
                });

        File protoDir = new File(options.getProtoDir());
        File tempDir = new File(options.getTempDir());
        File javaOutDir = new File(options.getJavaOut());

        // 清空旧文件
        if (tempDir.exists()) {
            FileUtils.cleanDirectory(tempDir);
        }
        if (options.isCleanJavaPackage()) {
            File javaPackageDir = new File(javaOutDir.getPath() + "/" + options.getJavaPackage().replace('.', '/'));
            if (javaPackageDir.exists()) {
                FileUtils.cleanDirectory(javaPackageDir);
            }
        }

        PBRepository repository = new PBRepository();
        Collection<File> protobufFiles = FileUtils.listFiles(protoDir, new String[]{"proto"}, false);
        for (File file : protobufFiles) {
            PBParser parser = new PBParser(file, options);
            repository.addFile(parser.parse());
            new PBFileGenerator(options, tempDir, parser).build();
        }
        new PBCompiler(options).build();
        new MethodInfoExporterGenerator(options, repository).build();
        new ServiceGenerator(options, repository).build();
    }

}