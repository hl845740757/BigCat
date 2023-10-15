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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.concurrent.*;
import cn.wjybxx.common.rpc.RpcAddr;
import cn.wjybxx.common.rpc.RpcRegistry;
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
    private final RpcAddr nodeAddr;

    private final Worker[] children;
    private final List<Worker> readonlyChildren;
    private final EventLoopChooser chooser;

    private volatile IntSet serviceIdSet = IntSets.emptySet();
    private volatile Int2ObjectMap<ServiceInfo> serviceInfoMap = Int2ObjectMaps.emptyMap();

    public NodeImpl(NodeBuilder.DefaultNodeBuilder builder) {
        super(decorate(builder));
        Agent agent = (Agent) getAgent();
        agent.node = this;

        this.workerId = Objects.requireNonNull(builder.getWorkerId(), "workerId");
        this.injector = Objects.requireNonNull(builder.getInjector(), "injector");
        this.nodeAddr = Objects.requireNonNull(builder.getNodeAddr(), "nodeAddr");

        // 初始化Module列表
        List<WorkerModule> moduleList = FxUtils.createModules(builder);
        this.mainModule = (MainModule) moduleList.get(0);
        this.moduleList = List.copyOf(moduleList);

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
            Worker eventLoop = Objects.requireNonNull(workerFactory.newChild(this, i));
            if (eventLoop.parent() != this) throw new IllegalStateException("the parent of worker is illegal");
            children[i] = eventLoop;
        }
        readonlyChildren = List.of(children);
        chooser = chooserFactory.newChooser(children);
    }

    private static EventLoopBuilder.DefaultBuilder decorate(NodeBuilder.DefaultNodeBuilder builder) {
        return builder.getDelegateBuilder()
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
    public RpcAddr nodeAddr() {
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
        return (Worker) chooser.next();
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

    @Nullable
    @Override
    public Node parent() {
        return null;
    }

    @Nonnull
    @Override
    public Node next() {
        return this;
    }

    @Nonnull
    @Override
    public Node select(int key) {
        return this;
    }
    //

    private static class Agent implements EventLoopAgent<AgentEvent> {

        NodeImpl node;
        MainModule mainModule; // 缓存
        List<WorkerModule> updatableModuleList = new ArrayList<>();
        List<WorkerModule> startedModuleList = new ArrayList<>();

        public Agent() {
        }

        @Override
        public void onStart(EventLoop eventLoop) throws Exception {
            mainModule = node.mainModule;
            updatableModuleList.addAll(FxUtils.filterUpdatableModules(node.moduleList));

            Worker.CURRENT_WORKER.set(node);
            Node.CURRENT_NODES.add(node);
            resolveDependence();

            // 需要先启动Node的模块和服务，Worker可能会在启动时使用
            mainModule.beforeWorkerStart();
            startModules();
            exportServices();
            startWorkers();
            exportServiceInfoMap();
            mainModule.afterWorkerStart();
        }

        private void resolveDependence() {
            for (WorkerModule workerModule : node.moduleList) {
                workerModule.inject(node);
            }
            mainModule.resolveDependence();
        }

        private void exportServices() {
            RpcRegistry registry = node.injector.getInstance(RpcRegistry.class);
            node.setServiceIdSet(registry.export());
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

        private void exportServiceInfoMap() {
            Int2ObjectMap<ServiceInfo> serviceInfoMap = new Int2ObjectOpenHashMap<>();
            // 需要包含Node自身的服务
            node.serviceIdSet.forEach((int serviceId) -> {
                serviceInfoMap.put(serviceId, new ServiceInfo(serviceId, List.of(node)));
            });
            // 添加Worker上的服务
            for (Worker child : node.children) {
                child.services().forEach((int serviceId) -> {
                    serviceInfoMap.computeIfAbsent(serviceId, k -> new ServiceInfo(k, new ArrayList<>(2))).workers.add(child);
                });
            }
            node.setServiceInfoMap(serviceInfoMap);
        }

        @Override
        public void onEvent(AgentEvent event) throws Exception {
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