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


/**
 * @author wjybxx
 * date 2023/4/15
 */
public class Header {

    /** 命令和参数，格式：{@code cs -x -y} */
    private final String args;
    /** 属性名 eg: {@code  itemId} */
    private final String name;
    /** 属性的类型 eg：{@code int32 float} */
    private final String type;
    /** 注释 */
    private final String comment;

    /** name定义的行索引 -- 记录行列，方便我们查看和导出 */
    private final int rowIndex;
    /** name定义的列索引 */
    private final int colIndex;

    public Header(String args, String name, String type, String comment, int rowIndex, int colIndex) {
        this.name = name;
        this.type = type.intern(); // type类型很少，而且会进行大量的equals测试，池化很有帮助
        this.args = args;
        this.comment = comment;
        this.rowIndex = rowIndex;
        this.colIndex = colIndex;
    }

    public String getName() {
        return name;
    }

    public String getType() {
        return type;
    }

    public String getArgs() {
        return args;
    }

    public String getComment() {
        return comment;
    }

    public int getRowIndex() {
        return rowIndex;
    }

    public int getColIndex() {
        return colIndex;
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Header header = (Header) o;

        if (rowIndex != header.rowIndex) return false;
        if (colIndex != header.colIndex) return false;
        if (!args.equals(header.args)) return false;
        if (!name.equals(header.name)) return false;
        if (!type.equals(header.type)) return false;
        return comment.equals(header.comment);
    }

    @Override
    public int hashCode() {
        int result = args.hashCode();
        result = 31 * result + name.hashCode();
        result = 31 * result + type.hashCode();
        result = 31 * result + comment.hashCode();
        result = 31 * result + rowIndex;
        result = 31 * result + colIndex;
        return result;
    }

    @Override
    public String toString() {
        return "Header{" +
                "args='" + args + '\'' +
                ", name='" + name + '\'' +
                ", type='" + type + '\'' +
                ", comment='" + comment + '\'' +
                ", rowIndex=" + rowIndex +
                ", colIndex=" + colIndex +
                '}';
    }
}