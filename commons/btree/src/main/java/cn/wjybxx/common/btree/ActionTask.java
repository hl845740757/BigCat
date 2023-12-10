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

/**
 * 行为节点抽象
 * (并非所有行为节点都需要继承该类)
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class ActionTask<E> extends LeafTask<E> {

    @Override
    protected final void execute() {
        int reentryId = getReentryId();
        int status = executeImpl();
        if (isExited(reentryId)) {
            return;
        }
        switch (status) {
            case Status.NEW -> throw new IllegalStateException("Illegal action status: " + status);
            case Status.RUNNING -> {}
            case Status.SUCCESS -> setSuccess();
            case Status.CANCELLED -> setCancelled();
            default -> setFailed(status);
        }
    }

    /**
     * 我们的大多数行为节点逻辑都较为简单，不需要事件驱动特性，因而可以转换为同步返回的节点。
     */
    protected abstract int executeImpl();
}
