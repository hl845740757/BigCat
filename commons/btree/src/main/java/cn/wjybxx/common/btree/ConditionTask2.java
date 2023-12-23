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
package cn.wjybxx.common.btree;

import javax.annotation.Nonnull;

/**
 * 可返回详细错误码的条件节点
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class ConditionTask2<E> extends LeafTask<E> {

    @Override
    protected final void execute() {
        int status = test();
        if (status == Status.SUCCESS) {
            setSuccess();
        } else {
            setFailed(status);
        }
    }

    protected abstract int test();

    @Override
    public boolean canHandleEvent(@Nonnull Object event) {
        return false;
    }

    /** 条件节点正常情况下不会触发事件 */
    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

}