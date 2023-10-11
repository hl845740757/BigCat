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

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.config.Header;
import cn.wjybxx.common.config.Sheet;
import cn.wjybxx.common.config.SheetCell;
import cn.wjybxx.common.config.SheetRow;
import it.unimi.dsi.fastutil.ints.Int2ObjectLinkedOpenHashMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import org.apache.commons.lang3.StringUtils;
import org.apache.poi.ss.usermodel.Cell;
import org.apache.poi.ss.usermodel.CellType;
import org.apache.poi.ss.usermodel.Row;

import java.io.IOException;
import java.util.*;
import java.util.function.Consumer;

/**
 * 1.poi的sheet行下标是0开始的，但我们在表格中所见的行号是1开始的，因此打印日志的时候还是需要打印成1开始的，否则不利于定位错误。
 *
 * @author wjybxx
 * date 2023/4/16
 */
class SheetReader {

    private static final String COL_ARGS = "args";
    private static final String COL_NAME = "name";
    private static final String COL_TYPE = "type";
    private static final String COL_VALUE = "value";
    private static final String COL_COMMENT = "comment";
    /** 参数表的所有列 */
    private static final List<String> PARAM_SHEET_COL_NAMES = List.of(COL_ARGS, COL_NAME, COL_TYPE, COL_VALUE, COL_COMMENT);
    /** 参数表至少具备的列 - 注释列可选 */
    private static final List<String> PARAM_SHEET_REQUIRED_COL_NAMES = List.of(COL_ARGS, COL_NAME, COL_TYPE, COL_VALUE);

    private final String fileName;
    private final String sheetName;
    private final int sheetIndex;
    private final ExcelReaderOptions options;

    private final int totalRowCount;
    private Iterator<Row> rowItr;

    SheetReader(String fileName, String sheetName, int sheetIndex, ExcelReaderOptions options,
                org.apache.poi.ss.usermodel.Sheet poiSheet) {
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
        this.options = options;

        totalRowCount = poiSheet.getLastRowNum() + 1;
        rowItr = poiSheet.rowIterator();
    }

    private static class CheckedIterator implements Iterator<Row> {

        final Iterator<Row> delegated;
        int expectedIndex;

        CheckedIterator(Iterator<Row> delegated, int expectedIndex) {
            this.delegated = delegated;
            this.expectedIndex = expectedIndex;
        }

        @Override
        public boolean hasNext() {
            return delegated.hasNext();
        }

        @Override
        public Row next() {
            // excel的数据存储似乎是不连贯的，中间的空行似乎没有存储
            // 我们正常的配置都应该是连贯的，因此需要进行校验 -- 不连贯容易引发各种问题
            final Row row = delegated.next();
            if (row.getRowNum() != expectedIndex) {
                throw new IllegalStateException(String.format("rowIndex: %d, expectedIndex: %d", row.getRowNum(), expectedIndex));
            }
            expectedIndex++;
            return row;
        }

        @Override
        public void remove() {
            delegated.remove();
        }

        @Override
        public void forEachRemaining(Consumer<? super Row> action) {
            delegated.forEachRemaining(action);
        }
    }

    /** @param expected 期望的行索引(0-based) */
    private static Row skipRowsUtil(Iterator<Row> rowItr, int expected) {
        while (rowItr.hasNext()) {
            Row row = rowItr.next();
            if (row.getRowNum() == expected) {
                return row;
            }
            if (row.getRowNum() > expected) {
                throw new IllegalStateException(String.format("The number of rows is not continuous, expected: %d, found: %d",
                        expected, row.getRowNum()));
            }
        }
        throw new IllegalStateException("Insufficient rows");
    }

    Sheet read() throws IOException {
        // 至少要有一行，才能判定表的有效性
        if (totalRowCount < options.skipRows + 1) {
            return null;
        }

        Row firstRow = skipRowsUtil(rowItr, options.skipRows);
        rowItr = new CheckedIterator(rowItr, options.skipRows + 1); // 后续使用行号必须连续

        Int2ObjectMap<String> firstRowValues = readHeaderRowValues(firstRow);
        final SheetContent sheetContent;
        if (isParamSheet(firstRowValues)) {
            sheetContent = readParamSheet(firstRow, rowItr);
        } else if (isNormalSheet(firstRowValues, totalRowCount, options.skipRows)) {
            sheetContent = readNormalSheet(firstRow, rowItr);
        } else {
            return null;
        }
        if (sheetContent.headerMap.isEmpty()) {
            return null;
        }
        return new Sheet(fileName, sheetName, sheetIndex, sheetContent.headerMap, sheetContent.valueRowList);
    }

    private static Int2ObjectMap<String> readHeaderRowValues(Row row) {
        final int totalColCount = getTotalColCount(row);
        final Int2ObjectMap<String> result = new Int2ObjectLinkedOpenHashMap<>(totalColCount);
        for (int colIndex = 0; colIndex < totalColCount; colIndex++) {
            final String cellValue = readCellValueNonNull(row, colIndex).trim();
            result.put(colIndex, cellValue);
        }
        return result;
    }

    private static boolean isParamSheet(Int2ObjectMap<String> firstRowValues) {
        for (String colName : PARAM_SHEET_REQUIRED_COL_NAMES) {
            if (!firstRowValues.containsValue(colName)) {
                return false;
            }
        }
        return true;
    }

    private static boolean isNormalSheet(Int2ObjectMap<String> firstRowValues, int totalRowCount, int skipRows) {
        // 表头信息不足
        if (totalRowCount - skipRows < 4) {
            return false;
        }
        // 只要有一列是c/s开头就认为是
        for (String value : firstRowValues.values()) {
            if (StringUtils.isBlank(value)) continue;
            if (isRequired(value, Mode.BOTH)) return true;
        }
        return false;
    }

    // 转义
    private static int getLineNumber(Row row) {
        return row.getRowNum() + 1;
    }

    private static int getTotalColCount(Row row) {
        return row.getLastCellNum();
    }

    private static String readCellValueNonNull(Row row, int colIndex) {
        final Cell cell = row.getCell(colIndex);
        if (cell == null) {
            return "";
        }
        if (cell.getCellType() == CellType.NUMERIC
                || cell.getCellType() == CellType.STRING
                || cell.getCellType() == CellType.BOOLEAN
                || cell.getCellType() == CellType.BLANK
                || cell.getCellType() == CellType.FORMULA) {
            // 原生POI最扯淡的是数值不能直接读取为字符串，导致丢失精度等，StreamerReader则可以，一切都是字符串，这很好
            return ObjectUtils.nullToDef(cell.getStringCellValue(), "");
        }
        throw new IllegalArgumentException(String.format("unsupported cellType, rowNumber %d cellType %s",
                getLineNumber(row), cell.getCellType()));
    }

    /** @param headerValue 应当去除了两端空白 */
    private static boolean isRequired(String headerValue, Mode mode) {
        if (headerValue.length() == 0) {
            return false;
        }
        if (mode == Mode.BOTH) {
            return containsCSChar(headerValue, 's') || containsCSChar(headerValue, 'c');
        }
        if (mode == Mode.CLIENT) {
            containsCSChar(headerValue, 'c');
        }
        return containsCSChar(headerValue, 's');
    }

    private static boolean containsCSChar(String cellValue, char required) {
        return cellValue.charAt(0) == required
                || (cellValue.length() > 1 && cellValue.charAt(1) == required);
    }

    private SheetContent readNormalSheet(Row firstRow, Iterator<Row> rowItr) {
        return new NormalSheetReader(firstRow, rowItr, sheetName, totalRowCount, options).read();
    }

    private SheetContent readParamSheet(Row firstRow, Iterator<Row> rowItr) {
        return new ParamSheetReader(firstRow, rowItr, sheetName, totalRowCount, options).read();
    }

    static class SheetContent {

        final Map<String, Header> headerMap;
        final List<SheetRow> valueRowList;

        public SheetContent(Map<String, Header> headerMap, List<SheetRow> valueRowList) {
            this.headerMap = headerMap;
            this.valueRowList = valueRowList;
        }
    }

    /** 定义类避免大规模传参 */
    static class NormalSheetReader {

        final Row argsRow;
        final Iterator<Row> rowItr;
        final String sheetName;
        final int totalRowCount;
        final ExcelReaderOptions options;

        final Row typeRow;
        final Row nameRow;
        final Row commentRow;

        NormalSheetReader(Row firstRow, Iterator<Row> rowItr,
                          String sheetName, int totalRowCount, ExcelReaderOptions options) {
            this.argsRow = firstRow;
            this.rowItr = rowItr;
            this.sheetName = sheetName;
            this.totalRowCount = totalRowCount;
            this.options = options;

            typeRow = rowItr.next();
            nameRow = rowItr.next();
            commentRow = rowItr.next();
        }

        private SheetContent read() {
            final Map<String, Header> headerMap = readHeaders();
            final List<SheetRow> valueRowList = new ArrayList<>(totalRowCount - options.skipRows - 4);
            while (rowItr.hasNext()) {
                valueRowList.add(readValueRow(headerMap, rowItr.next()));
            }
            return new SheetContent(headerMap, valueRowList);
        }

        /** header和定于顺序一致 */
        private Map<String, Header> readHeaders() {
            final int totalColCount = getTotalColCount(nameRow);
            // 使用LinkedHashMap以保持读入顺序
            final Map<String, Header> result = CollectionUtils.newLinkedHashMap(totalColCount);
            for (int colIndex = 0; colIndex < totalColCount; colIndex++) {
                final String args = readCellValueNonNull(argsRow, colIndex).trim();
                final String type = readCellValueNonNull(typeRow, colIndex).trim();
                final String name = readCellValueNonNull(nameRow, colIndex).trim();
                final String comment = readCellValueNonNull(commentRow, colIndex).trim();

                if (!isRequired(args, options.mode)) {
                    continue;
                }
                if (StringUtils.isBlank(name)) {
                    final String msg = String.format("colName cannot be blank, colIndex: %d", colIndex);
                    throw new IllegalArgumentException(msg);
                }
                if (result.containsKey(name)) {
                    final String msg = String.format("colName is duplicate, colIndex: %d, colName: %s", colIndex, name);
                    throw new IllegalArgumentException(msg);
                }
                if (!options.supportedTypes.contains(type)) {
                    final String msg = String.format("unsupported type, valueType: %s", type);
                    throw new IllegalArgumentException(msg);
                }

                result.put(name, new Header(args, name, type, comment, nameRow.getRowNum(), colIndex));
            }
            return result;
        }

        /** value和header顺序一致 */
        private SheetRow readValueRow(Map<String, Header> headerMap, Row valueRow) {
            // 使用LinkedHashMap以保持读入顺序
            final LinkedHashMap<String, SheetCell> name2CellMap = new LinkedHashMap<>();
            for (Header header : headerMap.values()) {
                String value = readCellValueNonNull(valueRow, header.getColIndex());
                name2CellMap.put(header.getName(), new SheetCell(value, header));
            }
            // 内容行不可以空白
            if (isBlackLine(name2CellMap)) {
                final String msg = String.format("black line, rowNumber: %d", getLineNumber(valueRow));
                throw new IllegalArgumentException(msg);
            }
            return new SheetRow(valueRow.getRowNum(), name2CellMap);
        }

        private static boolean isBlackLine(LinkedHashMap<String, SheetCell> name2CellMap) {
            for (SheetCell e : name2CellMap.values()) {
                if (!StringUtils.isBlank(e.getValue())) {
                    return false;
                }
            }
            return true;
        }

    }

    static class ParamSheetReader {

        final Row firstRow;
        final Iterator<Row> rowItr;
        final String sheetName;
        final int totalRowCount;
        final ExcelReaderOptions options;

        ParamSheetReader(Row firstRow, Iterator<Row> rowItr, String sheetName, int totalRowCount,
                         ExcelReaderOptions options) {
            this.firstRow = firstRow;
            this.rowItr = rowItr;
            this.sheetName = sheetName;
            this.totalRowCount = totalRowCount;
            this.options = options;
        }

        private static int findIndex(Int2ObjectMap<String> map, String value) {
            for (Int2ObjectMap.Entry<String> entry : map.int2ObjectEntrySet()) {
                if (Objects.equals(entry.getValue(), value)) {
                    return entry.getIntKey();
                }
            }
            return -1;
        }

        private SheetContent read() {
            final Int2ObjectMap<String> colIndex2FixedNameMap = readHeaderRowValues(firstRow);
            final int argsColIndex = findIndex(colIndex2FixedNameMap, COL_ARGS);
            final int nameColIndex = findIndex(colIndex2FixedNameMap, COL_NAME);
            final int typeColIndex = findIndex(colIndex2FixedNameMap, COL_TYPE);
            final int valueColIndex = findIndex(colIndex2FixedNameMap, COL_VALUE);
            final int commentColIndex = findIndex(colIndex2FixedNameMap, COL_COMMENT);

            final Map<String, Header> headerMap = CollectionUtils.newLinkedHashMap(totalRowCount - options.skipRows);
            final List<SheetRow> valueRowList = new ArrayList<>(totalRowCount - options.skipRows);
            while (rowItr.hasNext()) {
                final Row valueRow = rowItr.next();
                final String args = readCellValueNonNull(valueRow, argsColIndex).trim();
                final String name = readCellValueNonNull(valueRow, nameColIndex).trim();
                final String type = readCellValueNonNull(valueRow, typeColIndex).trim();
                final String value = readCellValueNonNull(valueRow, valueColIndex); // value不可以trim
                final String comment = commentColIndex >= 0 ? readCellValueNonNull(valueRow, commentColIndex).trim() : "";

                if (!isRequired(args, options.mode)) {
                    continue;
                }
                if (StringUtils.isBlank(name)) {
                    final String msg = String.format("the name cannot be blank, rowNumber: %d", getLineNumber(valueRow));
                    throw new IllegalStateException(msg);
                }
                if (headerMap.containsKey(name)) { // 参数名不可以重复
                    final String msg = String.format("the name is duplicate, name: %s", name);
                    throw new IllegalStateException(msg);
                }
                if (!options.supportedTypes.contains(type)) {
                    final String msg = String.format("unsupported type, valueType: %s", type);
                    throw new IllegalStateException(msg);
                }

                Header header = new Header(args, name, type, comment, valueRow.getRowNum(), nameColIndex);
                headerMap.put(name, header);

                SheetCell sheetCell = new SheetCell(value, header);
                valueRowList.add(new SheetRow(valueRow.getRowNum(), CollectionUtils.newLinkedHashMap(name, sheetCell)));
            }
            return new SheetContent(headerMap, valueRowList);
        }
    }

}