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

package cn.wjybxx.common.config;

import java.util.Map;
import java.util.Objects;

/**
 * 表格中的一行
 *
 * @author wjybxx
 * date 2023/4/15
 */
public class SheetRow implements CellProvider {

    /** 行索引 -- 内容行不是从0开始，取决于表格设计 */
    private final int rowIndex;
    /** 该行的所有值 */
    private final Map<String, SheetCell> name2CellMap;

    public SheetRow(int rowIndex, Map<String, SheetCell> name2CellMap) {
        this.rowIndex = rowIndex;
        this.name2CellMap = Objects.requireNonNull(name2CellMap);
    }

    /** 获取0开始的行索引 */
    public int getRowIndex() {
        return rowIndex;
    }

    /** 获取从1开始的行号 */
    public int getLineNumber() {
        return rowIndex + 1;
    }

    public Map<String, SheetCell> getName2CellMap() {
        return name2CellMap;
    }

    @Override
    public SheetCell getCell(String name) {
        return name2CellMap.get(name);
    }

    public SheetCell checkedGetCell(String name) {
        SheetCell sheetCell = name2CellMap.get(name);
        if (sheetCell == null) throw new IllegalArgumentException("cell is absent, name " + name);
        return sheetCell;
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        SheetRow sheetRow = (SheetRow) o;

        if (rowIndex != sheetRow.rowIndex) return false;
        return name2CellMap.equals(sheetRow.name2CellMap);
    }

    @Override
    public int hashCode() {
        int result = rowIndex;
        result = 31 * result + name2CellMap.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "SheetRow{" +
                "rowIndex=" + rowIndex +
                ", name2CellMap=" + name2CellMap +
                '}';
    }
}