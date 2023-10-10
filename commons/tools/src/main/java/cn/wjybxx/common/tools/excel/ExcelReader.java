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

import cn.wjybxx.common.CloseableUtils;
import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.config.Sheet;
import com.monitorjbl.xlsx.StreamingReader;
import org.apache.commons.lang3.StringUtils;
import org.apache.poi.ss.usermodel.Workbook;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.util.Map;
import java.util.Objects;
import java.util.function.Predicate;
import java.util.regex.Pattern;

/**
 * @author wjybxx
 * date 2023/4/16
 */
class ExcelReader implements AutoCloseable {

    private static final Logger logger = LoggerFactory.getLogger(ExcelReader.class);

    private static final String SHEET_NAME_REGEX = "^[a-zA-Z][a-zA-Z0-9_]*$";
    private static final Pattern PATTERN = Pattern.compile(SHEET_NAME_REGEX);

    private final File file;
    private final ExcelReaderOptions options;
    private final Workbook workbook;

    ExcelReader(File file, ExcelReaderOptions options) {
        this.file = file;
        this.options = Objects.requireNonNull(options, "options");
        workbook = StreamingReader.builder()
                .rowCacheSize(200)
                .bufferSize(options.bufferSize)
                .open(file);
    }

    Map<String, Sheet> readSheets() {
        final int numberOfSheets = workbook.getNumberOfSheets();
        final Map<String, Sheet> result = CollectionUtils.newLinkedHashMap(numberOfSheets);
        for (int sheetIndex = 0; sheetIndex < numberOfSheets; sheetIndex++) {
            final org.apache.poi.ss.usermodel.Sheet poiSheet = workbook.getSheetAt(sheetIndex);
            final String rawSheetName = poiSheet.getSheetName();

            final String sheetName = options.sheetNameParser.apply(file.getName(), rawSheetName);
            if (isSheetNameSkippable(sheetName, options.sheetNameFilter)) {
                logger.info("skip sheet, sheetName is invalid, fileName {}, sheetName {}", file.getName(), rawSheetName);
                continue;
            }

            try {
                if (result.containsKey(sheetName)) {
                    final String msg = String.format("sheetName resolved is duplicate, fileName%s, sheetName: %s", file.getName(), rawSheetName);
                    throw new IllegalStateException(msg);
                }

                final Sheet appSheet = new SheetReader(file.getName(), sheetName, sheetIndex, options, poiSheet).read();
                if (appSheet == null) { // 可能没需要读取的字段
                    logger.info("skip sheet, appSheet is null, fileName {}, sheetName {}", file.getName(), rawSheetName);
                    continue;
                }

                result.put(sheetName, appSheet);
            } catch (Exception e) {
                throw new ReadSheetException(file.getName(), rawSheetName, sheetIndex, e);
            }
        }
        return result;
    }

    private static boolean isSheetNameSkippable(String sheetName, Predicate<String> sheetNameFilter) {
        return StringUtils.isBlank(sheetName)
                || sheetName.startsWith("Sheet")
                || sheetName.startsWith("sheet")
                || sheetName.equals("ExamleLang") // Excel自带的隐藏表
                || !sheetNameFilter.test(sheetName)
                || !PATTERN.matcher(sheetName).matches();
    }

    @Override
    public void close() {
        CloseableUtils.closeSafely(workbook);
    }

}