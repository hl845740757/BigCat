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

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 在子节点完成之后仍返回运行。
 * 注意：在运行期间只运行一次子节点
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@BinarySerializable
@DocumentSerializable
public class AlwaysRunning<E> extends Decorator<E> {

    /** 记录子节点上次的重入id，这样不论enter和execute是否分开执行都不影响 */
    private transient int childPrevReentryId;

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (child == null) {
            childPrevReentryId = 0;
        } else {
            childPrevReentryId = child.getReentryId();
        }
    }

    @Override
    protected void execute() {
        if (child == null) {
            return;
        }
        final boolean started = child.isExited(childPrevReentryId);
        if (started && child.isCompleted()) {  // 勿轻易调整
            return;
        }
        template_runChild(child);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        if (child.isCancelled()) { // 不响应其它状态，但还是需要响应取消...
            setCancelled();
        }
    }

}