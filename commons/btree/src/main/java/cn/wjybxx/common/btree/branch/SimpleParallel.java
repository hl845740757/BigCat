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

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 简单并发节点。
 * 其中第一个任务为主要任务，其余任务为次要任务。
 * 一旦主要任务完成，则节点进入完成状态。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@BinarySerializable
@DocumentSerializable
public class SimpleParallel<E> extends Parallel<E> {

    @Override
    protected void execute() {
        final List<Task<E>> children = this.children;
        final Task<E> mainTask = children.get(0);

        final int reentryId = getReentryId();
        template_runChild(mainTask);
        if (checkCancel(reentryId)) { // 得出结果或取消
            return;
        }

        for (int idx = 1; idx < children.size(); idx++) {
            Task<E> child = children.get(idx);
            template_runHook(child);
            if (checkCancel(reentryId)) { // 得出结果或取消
                return;
            }
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        assert child == children.get(0);
        setCompleted(child.getStatus(), true);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        children.get(0).onEvent(event);
    }
}