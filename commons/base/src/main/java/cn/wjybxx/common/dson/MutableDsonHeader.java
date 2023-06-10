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

import cn.wjybxx.common.CollectionUtils;

/**
 * 1.Header不可以再持有header，否则陷入死循环
 * 2.Header的结构应该是简单清晰的，可简单编解码
 *
 * @author wjybxx
 * date - 2023/5/27
 */
public class MutableDsonHeader<K> extends DsonHeader<K> {

    public MutableDsonHeader() {
        this(4);
    }

    public MutableDsonHeader(int expectedSize) {
        super(CollectionUtils.newLinkedHashMap(expectedSize));
    }

    //

    /** @return this */
    public MutableDsonHeader<K> append(K key, DsonValue value) {
        super.append(key, value);
        return this;
    }

}