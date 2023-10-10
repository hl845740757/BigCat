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

package cn.wjybxx.common.tools.excel;

import cn.wjybxx.common.config.Sheet;

import java.io.File;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/16
 */
public class ExcelUtils {

    /**
     * 读取Excel
     *
     * @return sheetName -> sheet
     */
    public static Map<String, Sheet> readExcel(File file, ExcelReaderOptions options) {
        Objects.requireNonNull(options);
        try (final ExcelReader reader = new ExcelReader(file, options)) {
            return reader.readSheets();
        }
    }

    public static Map<String, Sheet> readExcel(File file) {
        return readExcel(file, ExcelReaderOptions.DEFAULT);
    }

}