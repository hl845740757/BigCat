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

package cn.wjybxx.common.dson;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public enum DsonContextType {

    /** 当前在最顶层，尚未开始读写（topLevel相当于一个数组） */
    TOP_LEVEL(null, null, null),

    /** 当前是一个普通对象结构（文档结构） */
    OBJECT(DsonType.OBJECT, "{", "}"),

    /** 当前是一个数组结构 */
    ARRAY(DsonType.ARRAY, "[", "]"),

    /** 当前是一个Header结构 - 类Object */
    HEADER(DsonType.HEADER, "@{", "}");

    public final DsonType dsonType;
    public final String startSymbol;
    public final String endSymbol;

    DsonContextType(DsonType dsonType, String startSymbol, String endSymbol) {
        this.dsonType = dsonType;
        this.startSymbol = startSymbol;
        this.endSymbol = endSymbol;
    }

    public boolean isContainer() {
        return dsonType.isContainer();
    }

    public boolean isLikeArray() {
        return this == ARRAY || this == TOP_LEVEL;
    }

    public boolean isLikeObject() {
        return this == OBJECT || this == HEADER;
    }

}