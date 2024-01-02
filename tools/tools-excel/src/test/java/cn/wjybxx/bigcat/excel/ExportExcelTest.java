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

package cn.wjybxx.bigcat.excel;

import cn.wjybxx.bigcat.tools.ExecutorMgr;
import cn.wjybxx.bigcat.tools.TestUtil;
import org.junit.jupiter.api.Test;

import java.io.IOException;

/**
 * @author wjybxx
 * date - 2023/10/15
 */
public class ExportExcelTest {

    @Test
    void test() throws IOException {
        ExcelExporterOptions options = new ExcelExporterOptions()
                .setExcelDir(TestUtil.docPath)
                .setOutDir(TestUtil.testResPath + "/temp")
                .setWriteEmptyCell(false);

        ExecutorMgr executorMgr = new ExecutorMgr();
        try {
            new ExcelExporter(options, executorMgr.getExecutorService()).build();
        } finally {
            executorMgr.shutdownNow();
        }
    }
}