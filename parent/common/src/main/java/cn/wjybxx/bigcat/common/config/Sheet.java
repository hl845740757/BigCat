/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.config;

import java.util.Iterator;
import java.util.List;
import java.util.Map;

/**
 * 一个表格页的抽象
 * 我们不使用POI那么复杂的数据结构，也不需要，简单的数据结构就可以很好工作。
 * <p>
 * 1.关于表格的格式，见顶层Doc目录下的<b>表格设计</b>文档
 * 2.程序可以直接使用{@link Sheet}作为配置对象，在运行时解析值，也可以提前解析并存储为json等格式。
 * 3.不论项目使用Excel、CSV或自定义格式，都可以转为该格式，该抽象的目的就在于解除业务对配置形式的依赖。
 *
 * @author wjybxx
 * date 2023/4/15
 */
public class Sheet {

    /** 文件名字 如: bag.xlsx */
    private final String fileName;
    /** 页签名  如：bag */
    private final String sheetName;
    /** 页索引，默认应该为0 */
    private final int sheetIndex;

    /** 所有的表头信息 */
    private final Map<String, Header> headerMap;
    /** 只包含内容部分 -- 因此{@link SheetRow#getRowIndex()}起始不是0 */
    private final List<SheetRow> valueRowList;

    public Sheet(String fileName, String sheetName, int sheetIndex,
                 Map<String, Header> headerMap, List<SheetRow> valueRowList) {
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
        this.headerMap = headerMap;
        this.valueRowList = valueRowList;
    }

    public String getFileName() {
        return fileName;
    }

    public String getSheetName() {
        return sheetName;
    }

    public int getSheetIndex() {
        return sheetIndex;
    }

    public Map<String, Header> getHeaderMap() {
        return headerMap;
    }

    public List<SheetRow> getValueRowList() {
        return valueRowList;
    }

    //

    /** 是否是参数表 */
    public boolean isParamSheet() {
        // 至少有name/type/value三个header，且不在同一行；header同一行的是普通表
        if (headerMap.size() < 3) {
            return false;
        }
        final Iterator<Header> itr = headerMap.values().iterator();
        return itr.next().getRowIndex() != itr.next().getRowIndex();
    }

    /**
     * 获取参数表某个参数的单元格
     * 由于参数表的特殊特性，我们在这里通过快捷方法
     */
    public SheetCell getParamCell(String name) {
        Header header = headerMap.get(name);
        if (header == null) {
            return null;
        }
        return getValueRow(header.getRowIndex())
                .getCell(name);
    }

    /** @param rowIndex 有效内容行的真实索引，非数组下标 */
    public SheetRow getValueRow(int rowIndex) {
        List<SheetRow> valueRowList = this.valueRowList;
        if (valueRowList.isEmpty()) {
            return null;
        }
        int firstRowIndex = valueRowList.get(0).getRowIndex();
        if (rowIndex < firstRowIndex) {
            throw new IllegalArgumentException(String.format("invalidRowIndex, firstRowIndex: %d, rowIndex: %d",
                    firstRowIndex, rowIndex));
        }
        return valueRowList.get(rowIndex - firstRowIndex);
    }

    /** 获取指定属性的表头 */
    public Header getHeader(String name) {
        return headerMap.get(name);
    }
    //

    public NormalSheetReader readNormalSheet(ValueParser valueParser) {
        if (isParamSheet()) {
            throw new IllegalArgumentException("this sheet is a param sheet");
        }
        return new NormalSheetReader(this, valueParser);
    }

    public ParamSheetReader readParamSheet(ValueParser valueParser) {
        if (!isParamSheet()) {
            throw new IllegalArgumentException("this sheet is a normal sheet");
        }
        return new ParamSheetReader(this, valueParser);
    }

    //
}