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

package cn.wjybxx.common.tools.util;

import java.util.Objects;

/**
 * 若重命名，可改名{@code FileLine}
 *
 * @author wjybxx
 * date - 2023/10/8
 */
public final class Line {

    /** 行号 */
    public final int ln;
    /** 内容 */
    public final String data;

    public Line(int ln, String data) {
        this.ln = ln;
        this.data = Objects.requireNonNull(data);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Line line = (Line) o;

        if (ln != line.ln) return false;
        return data.equals(line.data);
    }

    @Override
    public int hashCode() {
        int result = ln;
        result = 31 * result + data.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "Line{" +
                "ln=" + ln +
                ", data='" + data + '\'' +
                '}';
    }
}