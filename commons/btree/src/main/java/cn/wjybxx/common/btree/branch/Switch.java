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
package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
@BinarySerializable
@DocumentSerializable
public class Switch<E> extends SingleRunningChildBranch<E> {

    @Override
    protected void execute() {
        if (runningChild == null && !selectChild()) {
            setFailed(Status.ERROR);
            return;
        }
        template_runChildDirectly(runningChild);
    }

    private boolean selectChild() {
        for (int idx = 0; idx < children.size(); idx++) {
            Task<E> child = children.get(idx);
            if (!template_checkGuard(child.getGuard())) {
                child.setGuardFailed(null); // 不接收通知
                continue;
            }
            this.runningChild = child;
            this.runningIndex = idx;
            return true;
        }
        return false;
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        setCompleted(child.getStatus(), true);
    }
}