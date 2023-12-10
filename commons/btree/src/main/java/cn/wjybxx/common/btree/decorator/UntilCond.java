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
package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 循环子节点直到给定的条件达成
 *
 * @author wjybxx
 * date - 2023/12/1
 */
@BinarySerializable
@DocumentSerializable
public class UntilCond<E> extends LoopDecorator<E> {

    /** 循环条件 -- 不能直接使用child的guard，意义不同 */
    private Task<E> cond;

    @Override
    protected void onChildCompleted(Task<E> child) {
        if (template_checkGuard(cond)) {
            setSuccess();
        }
    }

    public Task<E> getCond() {
        return cond;
    }

    public UntilCond<E> setCond(Task<E> cond) {
        this.cond = cond;
        return this;
    }

}
