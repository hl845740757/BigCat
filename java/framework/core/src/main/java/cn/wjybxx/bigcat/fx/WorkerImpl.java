/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.concurrent.DisruptorEventLoop;
import cn.wjybxx.concurrent.EventLoopBuilder;
import com.google.inject.Injector;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;
import it.unimi.dsi.fastutil.ints.IntSets;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/10/4
 */
public final class WorkerImpl extends DisruptorEventLoop<WorkerEvent> implements Worker {

    private final WorkerAddr workerAddr;
    private final Injector injector;
    private volatile IntSet serviceIdSet = IntSets.emptySet();
    private final WorkerControlData controlData;

    public WorkerImpl(DefaultWorkerBuilder builder) {
        super(decorate(builder), false);

        final int nodeId = parent().nodeAddr().nodeId;
        final String workerId = Objects.requireNonNull(builder.getWorkerId(), "workerId");
        this.workerAddr = new WorkerAddr(nodeId, workerId);
        this.injector = Objects.requireNonNull(builder.getInjector(), "injector");
        this.controlData = builder.getControlData();
        // 导出Rpc服务 -- 先注册到Registry但不对外发布
        FxUtils.exportService(builder);

        // 构造完成后再初始化模块
        agent.inject(this, getConsumerId());
    }

    private static EventLoopBuilder.DisruptorBuilder<WorkerEvent> decorate(DefaultWorkerBuilder builder) {
        FxUtils.createModules(builder);
        return builder.getDelegated();
    }

    private void setServiceIdSet(IntSet serviceIdSet) {
        this.serviceIdSet = IntSets.unmodifiable(new IntOpenHashSet(serviceIdSet));
    }

    @Override
    public Injector injector() {
        return injector;
    }

    @Nonnull
    @Override
    public WorkerAddr workerAddr() {
        return workerAddr;
    }

    @Override
    public IntSet services() {
        return serviceIdSet;
    }

    @Override
    public WorkerControlData controlData() {
        return controlData;
    }

    @Nonnull
    @Override
    public Node node() {
        return (Node) parent;
    }

    @Nullable
    @Override
    public Node parent() {
        return (Node) parent;
    }

    @Nonnull
    @Override
    public Worker select() {
        return this;
    }

    @Nonnull
    @Override
    public Worker select(int key) {
        return this;
    }

    // region 生命周期

    @Override
    protected void onStart() throws Throwable {
        FxUtils.CURRENT_WORKER.set(this);

        agent.beforeEventLoopStart();
        startModules();
        exportServices();
        agent.afterEventLoopStart();
    }

    @Override
    protected void onShutdown() throws Throwable {
        try {
            setServiceIdSet(IntSets.emptySet());
            super.onShutdown();
        } finally {
            FxUtils.CURRENT_WORKER.remove();
        }
    }

    private void exportServices() {
        RpcProxyRegistry registry = injector.getInstance(RpcProxyRegistry.class);
        setServiceIdSet(registry.export());
    }

    // endregion

}