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
public interface DsonLineBuffer {

    /**
     * read只返回内容部分的char，获取行首请调用{@link #lhead()}
     * 1.position不一定是加 1
     *
     * @return 如果到达文件尾部，则返回 -1
     */
    int read();

    /**
     * @return 如果到达文件尾部，则返回 -1；
     * 1.position不一定是加 1
     * 2.如果产生换行，则返回 -2
     * 3.首次读也会产生换行，换行时当前位置处于行首。
     */
    int readSlowly();

    /**
     * 回退一次读
     * 注意：position不一定是减少1
     */
    void unread();

    DsonLheadType lhead();

    /**
     * 初始位置-1，表示尚未开始
     */
    int getPosition();

    interface Marker {

        void reset();

    }

}