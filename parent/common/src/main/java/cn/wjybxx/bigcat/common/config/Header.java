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

/**
 * @author wjybxx
 * date 2023/4/15
 */
public class Header {

    /** 属性名 eg: {@code  itemId} */
    private final String name;
    /** 属性的类型 eg：{@code int32 float} */
    private final String type;
    /** 命令和参数，格式：{@code cs -x -y} */
    private final String args;
    /** 注释 */
    private final String comment;

    /** name定义的行索引 -- 记录行列，方便我们导出 */
    private final int rowIndex;
    /** name定义的列索引 */
    private final int colIndex;

    public Header(String name, String type, String args, String comment, int rowIndex, int colIndex) {
        this.name = name;
        this.type = type.intern(); // type类型很少，而且会尽量大量的equals测试，池化很有帮助
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
}