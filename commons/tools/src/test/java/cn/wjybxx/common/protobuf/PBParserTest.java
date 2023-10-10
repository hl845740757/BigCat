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

package cn.wjybxx.common.protobuf;

import cn.wjybxx.common.tools.protobuf.PBFile;
import cn.wjybxx.common.tools.protobuf.PBMethod;
import cn.wjybxx.common.tools.protobuf.PBParser;
import cn.wjybxx.common.tools.protobuf.PBParserOptions;
import org.junit.jupiter.api.Test;

import java.io.File;

/**
 * @author wjybxx
 * date - 2023/10/9
 */
public class PBParserTest {

    private static final String resDir = new File(System.getProperty("user.dir")).getParent() + "/testres/";
    private static final File file = new File(resDir + "/test.proto");

    @Test
    void test() {
        PBParserOptions options = new PBParserOptions();
        options.getCommons().add("common_struct");
        options.setMethodDefMode(PBMethod.MODE_CONTEXT);

        PBParser reader = new PBParser(file, options);
        PBFile pbFile = reader.parse();
        System.out.println(pbFile);
    }
}
