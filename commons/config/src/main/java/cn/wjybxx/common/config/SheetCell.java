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

import java.util.Objects;

/**
 * 一个表格单元由两部分构成：value + header，
 * header用于声明value的名字和类型等，不过为了节省空间，header是单独存储的，因为普通表格的header是共享的。
 *
 * @author wjybxx
 * date 2023/4/15
 */
public class SheetCell {

    /**
     * 单元格内容
     * 和properties一样，这里都是原始的字符串，使得用户可以进行自定义解析 -- 可以实现{@link ValueParser}
     */
    private final String value;
    /** 缓存在Cell上方便使用 */
    private final transient Header header;

    public SheetCell(String value, Header header) {
        this.value = value;
        this.header = Objects.requireNonNull(header);
    }

    public String getValue() {
        return value;
    }

    public Header getHeader() {
        return header;
    }
    //

    public String getName() {
        return header.getName();
    }

    public String getType() {
        return header.getType();
    }

    public String getArgs() {
        return header.getArgs();
    }

    public String getComment() {
        return header.getComment();
    }

    public int getRowIndex() {
        return header.getRowIndex();
    }

    public int getColIndex() {
        return header.getColIndex();
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        SheetCell sheetCell = (SheetCell) o;

        if (!value.equals(sheetCell.value)) return false;
        return header.equals(sheetCell.header);
    }

    @Override
    public int hashCode() {
        int result = value.hashCode();
        result = 31 * result + header.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return value;
    }

}