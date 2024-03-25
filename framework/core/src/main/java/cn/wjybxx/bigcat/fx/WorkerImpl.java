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
import cn.wjybxx.concurrent.*;
import com.google.inject.Injector;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;
import it.unimi.dsi.fastutil.ints.IntSets;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/10/4
 */
public class WorkerImpl extends DisruptorEventLoop<RingBufferEvent> implements Worker {

    private final String workerId;
    private final Injector injector;
    private final MainModule mainModule;
    private final List<WorkerModule> moduleList;
    private volatile IntSet serviceIdSet = IntSets.emptySet();
    private final WorkerCtx workerCtx;

    public WorkerImpl(WorkerBuilder.DisruptWorkerBuilder builder) {
        super(decorate(builder));
        Agent agent = (Agent) getAgent();
        agent.worker = this;

        this.workerId = Objects.requireNonNull(builder.getWorkerId(), "workerId");
        this.injector = Objects.requireNonNull(builder.getInjector(), "injector");
        this.workerCtx = builder.getWorkerCtx();

        // 初始化Module列表
        List<WorkerModule> moduleList = FxUtils.createModules(builder);
        this.mainModule = (MainModule) moduleList.get(0);
        this.moduleList = List.copyOf(moduleList);
        // 导出Rpc服务 -- 先注册到Registry但不对外发布
        FxUtils.exportService(builder);
    }

    private static EventLoopBuilder.DisruptorBuilder<RingBufferEvent> decorate(WorkerBuilder.DisruptWorkerBuilder builder) {
        return builder.getDelegated()
                .setAgent(new Agent());
    }

    private void setServiceIdSet(IntSet serviceIdSet) {
        this.serviceIdSet = IntSets.unmodifiable(new IntOpenHashSet(serviceIdSet));
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

    @Nullable
    @Override
    public Node parent() {
        return (Node) parent;
    }

    @Nonnull
    @Override
    public Node node() {
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

    @Override
    public WorkerCtx workerCtx() {
        return workerCtx;
    }

    private static class Agent implements EventLoopAgent<RingBufferEvent> {

        WorkerImpl worker;
        MainModule mainModule; // 缓存
        List<WorkerModule> updatableModuleList = new ArrayList<>();
        List<WorkerModule> startedModuleList = new ArrayList<>();
        long loopFrame;

        public Agent() {
        }

        @Override
        public void inject(EventLoop eventLoop) {

        }

        @Override
        public void onStart() throws Exception {
            Worker.CURRENT_WORKER.set(worker);
            mainModule = worker.mainModule;
            updatableModuleList.addAll(FxUtils.filterUpdatableModules(worker.moduleList));
            resolveDependence();

            mainModule.beforeWorkerStart();
            startModules();
            exportServices();
            mainModule.afterWorkerStart();
        }

        private void resolveDependence() {
            for (WorkerModule workerModule : worker.moduleList) {
                workerModule.inject(worker);
            }
            mainModule.resolveDependence();
        }

        private void exportServices() {
            RpcRegistry registry = worker.injector.getInstance(RpcRegistry.class);
            worker.setServiceIdSet(registry.export());
        }

        private void startModules() {
            // 顺序启动
            for (WorkerModule workerModule : worker.moduleList) {
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

        @Override
        public void onEvent(RingBufferEvent event) throws Exception {
            mainModule.onEvent(event);
        }

        @Override
        public void update() throws Exception {
            while (mainModule.checkMainLoop(loopFrame)) {
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
            loopFrame++;
        }

        @Override
        public void onShutdown() throws Exception {
            try {
                mainModule.beforeWorkerShutdown();
            } catch (Throwable e) {
                logCause(e);
            }
            try {
                stopModules();
                mainModule.afterWorkerShutdown();
            } finally {
                Worker.CURRENT_WORKER.remove();
                mainModule = null;
                updatableModuleList.clear();
                startedModuleList.clear();
            }
        }
    }

}