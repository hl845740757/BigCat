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

package cn.wjybxx.common.excel;

import cn.wjybxx.common.tools.excel.ExcelExporter;
import cn.wjybxx.common.tools.excel.ExcelExporterOptions;
import cn.wjybxx.common.tools.util.ExecutorMgr;
import cn.wjybxx.common.tools.util.Utils;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.io.IOException;

/**
 * @author wjybxx
 * date - 2023/10/15
 */
public class ExportExcelTest {

    @Test
    void test() throws IOException {
        File projectDir = Utils.findProjectDir("BigCat");
        ExcelExporterOptions options = new ExcelExporterOptions()
                .setExcelDir(projectDir.getPath() + "/doc")
                .setOutDir(projectDir.getPath() + "/commons/testres/temp")
                .setWriteEmptyCell(false);

        ExecutorMgr executorMgr = new ExecutorMgr();
        try {
            new ExcelExporter(options, executorMgr.getExecutorService()).build();
        } finally {
            executorMgr.shutdownNow();
        }
    }
}