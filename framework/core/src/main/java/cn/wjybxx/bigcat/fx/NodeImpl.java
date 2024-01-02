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

import cn.wjybxx.bigcat.rpc.RpcRegistry;
import cn.wjybxx.common.concurrent.*;
import com.google.inject.Injector;
import it.unimi.dsi.fastutil.ints.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2023/10/4
 */
public class NodeImpl extends DefaultEventLoop implements Node {

    private final String workerId;
    private final Injector injector;
    private final MainModule mainModule;
    private final List<WorkerModule> moduleList;
    private final WorkerAddr nodeAddr;

    private final Worker[] children;
    private final List<Worker> readonlyChildren;
    private final EventLoopChooser chooser;
    private final WorkerCtx workerCtx = new WorkerCtx();

    private volatile IntSet serviceIdSet = IntSets.emptySet();
    private volatile Int2ObjectMap<ServiceInfo> serviceInfoMap = Int2ObjectMaps.emptyMap();

    public NodeImpl(NodeBuilder.DefaultNodeBuilder builder) {
        super(decorate(builder));
        Agent agent = (Agent) getAgent();
        agent.node = this;

        this.workerId = Objects.requireNonNull(builder.getWorkerId(), "workerId");
        this.injector = Objects.requireNonNull(builder.getInjector(), "injector");
        this.nodeAddr = Objects.requireNonNull(builder.getNodeAddr(), "nodeAddr");
        if (nodeAddr.hasWorkerId()) {
            throw new IllegalArgumentException("nodeAddr.workerId must be null, addr: " + nodeAddr);
        }

        // 初始化Module列表
        List<WorkerModule> moduleList = FxUtils.createModules(builder);
        this.mainModule = (MainModule) moduleList.get(0);
        this.moduleList = List.copyOf(moduleList);
        // 导出Rpc服务 -- 先注册到Registry但不对外发布
        FxUtils.exportService(builder);

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
        for (int i = 0; i < numberChildren; i++) {
            WorkerCtx workerCtx = new WorkerCtx();
            Worker eventLoop = Objects.requireNonNull(workerFactory.newChild(this, workerCtx, i));
            if (eventLoop.parent() != this) throw new IllegalStateException("the parent of worker is illegal");
            if (eventLoop.workerCtx() != workerCtx) throw new IllegalStateException("the ctx of worker is illegal");
            children[i] = eventLoop;
        }
        readonlyChildren = List.of(children);
        chooser = chooserFactory.newChooser(children);
    }

    private static EventLoopBuilder.DefaultBuilder decorate(NodeBuilder.DefaultNodeBuilder builder) {
        return builder.getDelegated()
                .setAgent(new Agent());
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

    @Override
    public String workerId() {
        return workerId;
    }

    @Override
    public Injector injector() {
        return injector;
    }

    @Override
    public MainModule mainModule() {
        return mainModule;
    }

    @Override
    public List<WorkerModule> modules() {
        return moduleList; // 是不可变List
    }

    @Override
    public IntSet services() {
        return serviceIdSet; // 不可变Set
    }

    @Override
    public WorkerAddr nodeAddr() {
        return nodeAddr;
    }

    @Override
    public Int2ObjectMap<ServiceInfo> serviceInfoMap() {
        return serviceInfoMap;
    }

    @Override
    public Iterator<Worker> workers() {
        return readonlyChildren.iterator();
    }

    @Override
    public Worker mainWorker() {
        return children[0];
    }

    @Override
    public Worker nextWorker() {
        return (Worker) chooser.select();
    }

    @Override
    public Worker selectWorker(int key) {
        return (Worker) chooser.select(key);
    }

    @Override
    public Worker findWorker(String workerId) {
        // 该接口不常用，运行时Worker数量不多，暂不优化
        for (Worker child : children) {
            if (child.workerId().equals(workerId)) {
                return child;
            }
        }
        return null;
    }

    //
    @Override
    public WorkerCtx workerCtx() {
        return workerCtx;
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
    public Node select() {
        return this;
    }

    @Nonnull
    @Override
    public Node select(int key) {
        return this;
    }
    //

    private static class Agent implements EventLoopAgent {

        NodeImpl node;
        MainModule mainModule; // 缓存
        List<WorkerModule> updatableModuleList = new ArrayList<>();
        List<WorkerModule> startedModuleList = new ArrayList<>();

        public Agent() {
        }

        @Override
        public void inject(EventLoop eventLoop) {

        }

        @Override
        public void onStart() throws Exception {
            Worker.CURRENT_WORKER.set(node);
            Node.CURRENT_NODES.add(node);
            mainModule = node.mainModule;
            updatableModuleList.addAll(FxUtils.filterUpdatableModules(node.moduleList));

            initWorkerCtx();
            resolveDependence();

            // 需要先启动Node的模块和服务，Worker可能会在启动时使用
            mainModule.beforeWorkerStart();
            startModules();
            exportServices(List.of()); // 此时先导出自己的服务，Worker可能需要使用
            startWorkers();
            exportServices(node.readonlyChildren);
            mainModule.afterWorkerStart();
        }

        private void initWorkerCtx() {
            node.workerCtx.init(node);
            for (Worker worker : node.children) {
                worker.workerCtx().init(worker);
            }
        }

        private void resolveDependence() {
            for (WorkerModule workerModule : node.moduleList) {
                workerModule.inject(node);
            }
            mainModule.resolveDependence();
        }

        private void exportServices(List<Worker> workers) {
            IntSet nodeServiceIdSet = node.injector.getInstance(RpcRegistry.class).export();
            node.setServiceIdSet(nodeServiceIdSet);

            Int2ObjectMap<ServiceInfo> serviceInfoMap = new Int2ObjectOpenHashMap<>();
            // Node自身的服务
            nodeServiceIdSet.forEach((int serviceId) -> {
                serviceInfoMap.put(serviceId, new ServiceInfo(serviceId, List.of(node)));
            });
            // 添加Worker上的服务 -- Worker不可包含Node同名服务
            for (Worker worker : workers) {
                worker.services().forEach((int serviceId) -> {
                    if (nodeServiceIdSet.contains(serviceId)) {
                        throw new IllegalArgumentException("The service in the worker conflicts with the service in the node, id " + serviceId);
                    }
                    serviceInfoMap.computeIfAbsent(serviceId, k -> new ServiceInfo(k, new ArrayList<>(2)))
                            .addWorker(worker);
                });
            }
            node.setServiceInfoMap(serviceInfoMap);
        }

        private void startModules() {
            // 顺序启动
            for (WorkerModule workerModule : node.moduleList) {
                workerModule.start();
                startedModuleList.add(workerModule);
            }
        }

        private void stopModules() {
            // 逆序停止
            List<WorkerModule> startedModuleList = this.startedModuleList;
            for (int i = startedModuleList.size() - 1; i >= 0; i--) {
                WorkerModule workerModule = startedModuleList.get(i);
                try {
                    workerModule.stop();
                } catch (Throwable e) {
                    logCause(e);
                }
            }
        }

        private void startWorkers() {
            FutureCombiner combiner = FutureUtils.newCombiner();
            for (Worker child : node.children) {
                combiner.add(child.start());
            }
            combiner.selectAll().join();
        }

        private void stopWorkers() {
            FutureCombiner combiner = FutureUtils.newCombiner();
            Worker[] children = node.children;
            for (Worker child : children) {
                combiner.add(child.terminationFuture());
            }
            XCompletableFuture<Object> aggregateFuture = combiner.selectAll(true);

            // 逆序关闭 -- 可能存在时序依赖
            for (int i = children.length - 1; i >= 0; i--) {
                Worker child = children[i];
                child.shutdown();
            }
            if (aggregateFuture.awaitUninterruptedly(1, TimeUnit.MINUTES)) {
                return;
            }
            // 进入快速关闭阶段
            for (int i = children.length - 1; i >= 0; i--) {
                Worker child = children[i];
                child.shutdownNow();
            }
            aggregateFuture.join();
        }

        @Override
        public void onEvent(RingBufferEvent event) throws Exception {
            mainModule.onEvent(event);
        }

        @Override
        public void update() throws Exception {
            if (!mainModule.checkMainLoop()) {
                return;
            }
            mainModule.beforeMainLoop();
            List<WorkerModule> updatableModuleList = this.updatableModuleList;
            for (int i = 0; i < updatableModuleList.size(); i++) {
                WorkerModule workerModule = updatableModuleList.get(i);
                try {
                    workerModule.update();
                } catch (Throwable e) {
                    logCause(e);
                }
            }
            mainModule.afterMainLoop();
        }

        @Override
        public void onShutdown() throws Exception {
            try {
                mainModule.beforeWorkerShutdown();
            } catch (Throwable e) {
                logCause(e);
            }
            try {
                stopWorkers();
                stopModules();
                mainModule.afterWorkerShutdown();
            } finally {
                Worker.CURRENT_WORKER.remove();
                Node.CURRENT_NODES.remove(node);
                mainModule = null;
                updatableModuleList.clear();
                startedModuleList.clear();
            }
        }

    }

}