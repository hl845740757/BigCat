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
 * Dson是按照行解析的，缓存是也是基于行
 * 注意：
 * 1. 注释行不会被迭代(实现类最好是不加载到内存)
 * 2. 其它三种类型的行都会被迭代，这样位置信息才是准确的，与源文件尽可能匹配。
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public interface DsonBuffer extends AutoCloseable {

    /**
     * 1.只返回内容部分的char.忽略换行事件
     * 2.空行和注释行会被跳过
     * 3.position不一定是加 1
     *
     * @return 1.如果到达文件尾部，则返回 -1
     */
    default int read() {
        int c;
        while ((c = readSlowly()) == -2) {
        }
        return c;
    }

    /**
     * 1.首次读也会产生换行，换行时当前位置处于行首。
     * 2.空行和注释行会被跳过
     * 3.position不一定是加 1
     *
     * @return 1.如果到达文件尾部，则返回 -1；
     * 2.如果产生换行，则返回 -2
     */
    int readSlowly();

    /**
     * 回退一次读
     * 1.position不一定是减少1
     * 2.回退可能是有限制的 -- 节省开销
     */
    void unread();

    /**
     * 获取当前的行首标记
     */
    LheadType lhead();

    /**
     * 初始位置-1，表示尚未开始
     * 有效行的起始位置不一定是0
     */
    int getPosition();

    /**
     * 获取行号
     * 1.初始0，表示尚未开始
     * 2.正式行号1开始
     */
    int getLn();

    /**
     * 获取列号
     * 1.初始0，表示尚未开始
     * 2.正式列号1开始
     */
    int getCol();

    /**
     * 主要方便打印
     * 在不同的电脑上，由于换行符的实现不同，因此一个字符的pos可能和在文本编辑器中不同，
     * 这种情况下使用与换行符无关的行号和列号更加有用，可以快速定位错误
     */
    default LnCol getLnCol() {
        return new LnCol(getLn(), getCol());
    }

    @Override
    void close();

    interface Marker {

        void reset();

    }

    class LnCol {

        private final int ln;
        private final int col;

        public LnCol(int ln, int col) {
            this.ln = ln;
            this.col = col;
        }

        public int getLn() {
            return ln;
        }

        public int getCol() {
            return col;
        }

        @Override
        public String toString() {
            return "LnCol{%d, %d}".formatted(ln, col);
        }
    }

}