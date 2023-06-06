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

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
public class DsonTextWriterSettings {

    public final String lineSeparator;
    public final int softLineLength;
    public final int maxLineLength;
    public final boolean unicodeChar;

    private DsonTextWriterSettings(Builder builder) {
        this.lineSeparator = Objects.requireNonNull(builder.lineSeparator);
        this.softLineLength = builder.softLineLength;
        this.maxLineLength = builder.maxLineLength;
        this.unicodeChar = builder.unicodeChar;
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        /** 行分隔符 */
        private String lineSeparator = System.lineSeparator();
        /**
         * 软行长度
         * 在写一个值的过程中不检查该值，在写入新值前检查行长度，如果行长度达到该值，则触发换行。
         */
        private int softLineLength = 120;
        /**
         * 最大行长度
         * 当行长度达到该值时立即换行
         */
        private int maxLineLength = 150;
        /**
         * 非单字节字符是否转unicode字符
         * (ascii码32~127以外的字符)
         */
        private boolean unicodeChar = false;

        private Builder() {
        }

        public DsonTextWriterSettings build() {
            return new DsonTextWriterSettings(this);
        }

        public String getLineSeparator() {
            return lineSeparator;
        }

        public Builder setLineSeparator(String lineSeparator) {
            this.lineSeparator = lineSeparator;
            return this;
        }

        public int getSoftLineLength() {
            return softLineLength;
        }

        public Builder setSoftLineLength(int softLineLength) {
            this.softLineLength = softLineLength;
            return this;
        }

        public int getMaxLineLength() {
            return maxLineLength;
        }

        public Builder setMaxLineLength(int maxLineLength) {
            this.maxLineLength = maxLineLength;
            return this;
        }

        public boolean isUnicodeChar() {
            return unicodeChar;
        }

        public Builder setUnicodeChar(boolean unicodeChar) {
            this.unicodeChar = unicodeChar;
            return this;
        }
    }

}