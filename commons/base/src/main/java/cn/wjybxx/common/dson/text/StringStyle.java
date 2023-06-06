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

package cn.wjybxx.common.dson.text;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
public enum StringStyle {

    /**
     * 自动判别
     * 1.当内容较短且无特殊字符，且不是特殊值（tre/false/数字）时不加引号
     * 2.当内容长度中等时，打印为双引号字符串
     * 3.当内容较长时，打印为文本模式
     */
    AUTO,

    /** 加引号 -- 内容可能包含特殊字符，且想保持流式输入 */
    QUOTE,

    /** 不加引号 -- 内容不包含特殊字符，且内容较短；要小心使用 */
    UNQUOTE,

    /** 不加引号 -- 内容可能包含特殊字符，或内容较长 */
    TEXT,

}