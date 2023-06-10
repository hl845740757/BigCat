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

import cn.wjybxx.common.annotation.Internal;

/**
 * @author wjybxx
 * date - 2023/6/3
 */
@Internal
class LineInfo {

    /** 行全局起始位置，包含行首，与上一行只差包含换行符 */
    final int startPos;
    /** 行全局结束位置，包含换行符 -- start和end相等时表示空行 */
    final int endPos;
    /** 内容全局起始位置，可能 -1，表示该行没有内容 */
    final int contentStartPos;
    /** 行首类型 */
    final LheadType lheadType;

    /** 行号 */
    final int ln;
    /** 行信息在数组中索引 */
    final int index;

    public LineInfo(int startPos, int endPos, int contentStartPos, LheadType lheadType,
                    int ln, int index) {
        this.startPos = startPos;
        this.endPos = endPos;
        this.contentStartPos = contentStartPos;
        this.lheadType = lheadType;
        this.ln = ln;
        this.index = index;
    }

    @Override
    public String toString() {
        return "LineInfo{" +
                " startPos=" + startPos +
                ", endPos=" + endPos +
                ", contentStartPos=" + contentStartPos +
                ", lheadType=" + lheadType +
                ", ln=" + ln +
                '}';
    }
}