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
package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.SimpleParallel;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * Main策略，当第一个任务完成时就完成。
 * 类似{@link SimpleParallel}，但Join在得出结果前不重复运行已完成的子节点
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@ClassImpl(singleton = "getInstance")
@BinarySerializable
@DocumentSerializable
public class JoinMain<E> implements JoinPolicy<E> {

    private static final JoinMain<?> INSTANCE = new JoinMain<>();

    @SuppressWarnings("unchecked")
    public static <E> JoinMain<E> getInstance() {
        return (JoinMain<E>) INSTANCE;
    }

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<E> join) {

    }

    @Override
    public void enter(Join<E> join) {
        if (join.getChildCount() == 0) {
            join.setFailed(Status.CHILDLESS);
        }
    }

    @Override
    public void onChildCompleted(Join<E> join, Task<E> child) {
        if (join.isFirstChild(child)) {
            join.setCompleted(child.getStatus(), true);
        }
    }

    @Override
    public void onEvent(Join<E> join, Object event) {
        Task<E> firstChild = join.getFirstChild();
        assert firstChild != null;
        firstChild.onEvent(event);
    }
}