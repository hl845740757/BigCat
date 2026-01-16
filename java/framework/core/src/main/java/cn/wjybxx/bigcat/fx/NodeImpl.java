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

import cn.wjybxx.concurrent.*;
import com.google.inject.Injector;
import it.unimi.dsi.fastutil.ints.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2023/10/4
 */
public final class NodeImpl extends DisruptorEventLoop<WorkerEvent> implements Node {

    private final Injector injector;
    private final WorkerAddr workerAddr;
    private final WorkerAddr nodeAddr;

    private final Worker[] children;
    private final List<Worker> readonlyChildren;
    private final EventLoopChooser chooser;
    private final WorkerControlData controlData = new WorkerControlData();

    /** Node自身的服务信息 */
    private volatile IntSet serviceIdSet = IntSets.emptySet();
    /** Node+Worker的服务信息 */
    private volatile Int2ObjectMap<ServiceInfo> serviceInfoMap = Int2ObjectMaps.emptyMap();

    public NodeImpl(DefaultNodeBuilder builder) {
        super(decorate(builder), false);

        String workerId = Objects.requireNonNull(builder.getWorkerId(), "workerId");
        int nodeId = builder.getNodeId();
        this.workerAddr = new WorkerAddr(nodeId, workerId);
        this.nodeAddr = new WorkerAddr(nodeId, null);
        this.injector = Objects.requireNonNull(builder.getInjector(), "injector");
        // 导出Rpc服务 -- 先注册到Registry但不对外发布
        FxUtils.exportService(builder);
        FxUtils.exportMethodInfo(builder);

        int numberChildren = builder.getNumberChildren();
        if (numberChildren < 1) {
            throw new IllegalArgumentException("numberChildren must greater than 0");
        }
        WorkerFactory workerFactory = builder.getWorkerFactory();
        if (workerFactory == null) {
            throw new NullPointerException("workerFactory");
        }
        EventLoopChooserFactory chooserFactory = builder.getChooserFactory();
        if (chooserFactory == null) {
            chooserFactory = new DefaultChooserFactory();
        }
        children = new Worker[numberChildren];
        for (int idx = 0; idx < numberChildren; idx++) {
            WorkerControlData controlData = new WorkerControlData();
            Worker eventLoop = workerFactory.newChild(this, idx, controlData);
            if (eventLoop.parent() != this) throw new IllegalStateException("the parent of worker is illegal");
            if (eventLoop.controlData() != controlData)
                throw new IllegalStateException("the controlData of worker is illegal");
            if (builder.getManualClose() != null) controlData.manualClose = builder.getManualClose();
            children[idx] = eventLoop;
        }
        readonlyChildren = List.of(children);
        chooser = chooserFactory.newChooser(children);

        // 构造完成后再初始化模块
        agent.inject(this, getConsumerId());
    }

    private static EventLoopBuilder.DisruptorBuilder<WorkerEvent> decorate(DefaultNodeBuilder builder) {
        FxUtils.createModules(builder);
        if (builder.getAgent() == null) {
            MainModule agent = builder.getInjector().getInstance(MainModule.class);
            builder.setAgent(agent);
        }
        return builder.getDelegated();
    }

    private void setServiceIdSet(IntSet serviceIdSet) {
        this.serviceIdSet = IntSets.unmodifiable(new IntOpenHashSet(serviceIdSet));
    }

    private void setServiceInfoMap(Int2ObjectMap<ServiceInfo> serviceInfoMap) {
        Int2ObjectMap<ServiceInfo> tempMap = new Int2ObjectOpenHashMap<>(serviceInfoMap.size());
        for (ServiceInfo serviceInfo : serviceInfoMap.values()) {
            tempMap.put(serviceInfo.serviceId, serviceInfo.toImmutable());
        }
        this.serviceInfoMap = Int2ObjectMaps.unmodifiable(tempMap);
    }

    // ---------------------
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
    public WorkerAddr nodeAddr() {
        return nodeAddr;
    }

    @Override
    public IntSet services() {
        return serviceIdSet;
    }

    @Override
    public Int2ObjectMap<ServiceInfo> serviceInfoMap() {
        return serviceInfoMap;
    }

    @Override
    public List<Worker> workers() {
        return readonlyChildren;
    }

    @Override
    public Worker mainWorker() {
        return children[0];
    }

    @Override
    public Worker findWorker(String workerId) {
        // worker通常不多，for循环足够快
        for (int i = 0; i < children.length; i++) {
            Worker child = children[i];
            if (workerId.equals(child.workerAddr().workerId)) {
                return child;
            }
        }
        // 可能是查找自己
        if (workerId.equals(workerAddr.workerId)) {
            return this;
        }
        return null;
    }

    //------------------------
    @Override
    public WorkerControlData controlData() {
        return controlData;
    }

    @Nonnull
    @Override
    public Node node() {
        return this;
    }

    @Nullable
    @Override
    public Node parent() {
        return null;
    }

    @Nonnull
    @Override
    public Worker select() {
        return (Worker) chooser.select();
    }

    @Nonnull
    @Override
    public Worker select(int key) {
        return (Worker) chooser.select(key);
    }

    // region 生命流程

    @Override
    protected void onStart() throws Throwable {
        FxUtils.CURRENT_WORKER.set(this);
        FxUtils.CURRENT_NODES.add(this);
        initControlData();

        agent.beforeEventLoopStart();
        startModules(); // 先启动自己的模块和服务，Worker可能需要使用
        exportServices(List.of());

        startWorkers();
        exportServices(readonlyChildren); // 再重新导出所有的服务
        agent.afterEventLoopStart();
    }

    @Override
    protected void onShutdown() throws Throwable {
        agent.beforeEventLoopShutdown();
        try {
            stopWorkers(); // 先停止worker，再停止自己的模块和服务
            setServiceIdSet(IntSets.emptySet());
            setServiceInfoMap(Int2ObjectMaps.emptyMap());

            stopModules();
            destroyModules();
            agent.afterEventLoopShutdown();
        } finally {
            FxUtils.CURRENT_WORKER.remove();
            FxUtils.CURRENT_NODES.remove(this);
        }
    }

    private void initControlData() {
        controlData.init(this, false);
        for (Worker worker : children) {
            worker.controlData().init(worker, worker.state() == EventLoopState.UNSTARTED);
        }
    }

    private void exportServices(List<Worker> workers) {
        // Node自身的服务
        IntSet nodeServiceIdSet = injector.getInstance(RpcMethodRegistry.class).export();
        setServiceIdSet(nodeServiceIdSet);

        Int2ObjectMap<ServiceInfo> serviceInfoMap = new Int2ObjectOpenHashMap<>();
        // Node自身的服务
        nodeServiceIdSet.forEach((int serviceId) -> {
            serviceInfoMap.put(serviceId, new ServiceInfo(serviceId, List.of(this)));
        });
        // 添加Worker上的服务 -- Worker不可包含Node同名服务
        for (Worker worker : workers) {
            worker.services().forEach((int serviceId) -> {
                if (nodeServiceIdSet.contains(serviceId)) {
                    throw new IllegalStateException("The service in the worker conflicts with the service in the node, id " + serviceId);
                }
                ServiceInfo serviceInfo = serviceInfoMap.get(serviceId);
                if (serviceInfo == null) {
                    serviceInfo = new ServiceInfo(serviceId, new ArrayList<>(2));
                    serviceInfoMap.put(serviceId, serviceInfo);
                }
                serviceInfo.addWorker(worker);
            });
        }
        setServiceInfoMap(serviceInfoMap);
    }

    private void startWorkers() {
        FutureCombiner combiner = ExecutorUtils.newCombiner();
        for (Worker child : children) {
            combiner.add(child.start());
        }
        combiner.selectAll().join();
    }

    private void stopWorkers() {
        FutureCombiner combiner = ExecutorUtils.newCombiner();
        // 逆序关闭 -- 可能存在时序依赖
        for (int i = children.length - 1; i >= 0; i--) {
            Worker child = children[i];
            if (child.controlData().isManualClose()) continue;
            child.shutdown();
            combiner.add(child.terminationFuture());
        }
        IPromise<Object> aggregateFuture = combiner.selectAll(true);
        if (aggregateFuture.awaitUninterruptibly(1, TimeUnit.MINUTES)) {
            return;
        }
        // 进入快速关闭阶段
        for (int i = children.length - 1; i >= 0; i--) {
            Worker child = children[i];
            if (child.controlData().isManualClose()) continue;
            child.shutdownNow();
        }
        aggregateFuture.join();
    }

    // endregion
}