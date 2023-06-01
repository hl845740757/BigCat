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

package cn.wjybxx.common.collect;

import java.util.ArrayList;
import java.util.Collection;

/**
 * 用于数据量较少的情况下，避免较大的初始容量
 *
 * @author wjybxx
 * date - 2023/6/1
 */
public class SmallArrayList<E> extends ArrayList<E> {

    private static final int INIT_CAPACITY = 4;

    public SmallArrayList() {
        super(0); // 必须显式指定0，才能在add的时候ensureCapacity成功
    }

    public SmallArrayList(Collection<? extends E> c) {
        super(c);
    }

    @Override
    public boolean add(E e) {
        if (isEmpty()) {
            ensureCapacity(INIT_CAPACITY);
        }
        return super.add(e);
    }

    @Override
    public void add(int index, E element) {
        if (isEmpty()) {
            ensureCapacity(INIT_CAPACITY);
        }
        super.add(index, element);
    }

}