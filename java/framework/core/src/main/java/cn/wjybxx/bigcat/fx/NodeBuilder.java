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

import cn.wjybxx.base.Preconditions;
import cn.wjybxx.concurrent.EventLoopBuilder;
import cn.wjybxx.concurrent.EventLoopChooserFactory;
import cn.wjybxx.concurrent.IEventLoopAgent;
import cn.wjybxx.concurrent.RejectedExecutionHandler;
import com.google.inject.Injector;

import java.util.HashSet;
import java.util.List;
import java.util.Objects;
import java.util.Set;
import java.util.concurrent.ThreadFactory;

/**
 * 建议先设置子类属性再设置父类属性，避免过多的重写
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public abstract class NodeBuilder extends WorkerBuilder {

    private int numberChildren = 1;
    private WorkerFactory workerFactory;
    private EventLoopChooserFactory chooserFactory;

    /** 服务器节点id */
    private int nodeId;
    /** rpc接口所在的包，用于生成{@link RpcMethodRegistry} */
    private final Set<String> rpcPackages = new HashSet<>();

    protected NodeBuilder(EventLoopBuilder<WorkerEvent> delegateBuilder) {
        super(delegateBuilder);
        setWorkerId("Node");
    }

    @Override
    public abstract Node build();

    // region node
    public int getNumberChildren() {
        return numberChildren;
    }

    public NodeBuilder setNumberChildren(int numberChildren) {
        this.numberChildren = numberChildren;
        return this;
    }

    public WorkerFactory getWorkerFactory() {
        return workerFactory;
    }

    public NodeBuilder setWorkerFactory(WorkerFactory workerFactory) {
        this.workerFactory = workerFactory;
        return this;
    }

    public EventLoopChooserFactory getChooserFactory() {
        return chooserFactory;
    }

    public NodeBuilder setChooserFactory(EventLoopChooserFactory chooserFactory) {
        this.chooserFactory = chooserFactory;
        return this;
    }

    public int getNodeId() {
        return nodeId;
    }

    public NodeBuilder setNodeId(int nodeId) {
        this.nodeId = nodeId;
        return this;
    }

    public Set<String> getRpcPackages() {
        return rpcPackages;
    }

    public NodeBuilder addRpcPackage(String pkg) {
        Objects.requireNonNull(pkg);
        rpcPackages.add(pkg);
        return this;
    }

    public NodeBuilder addRpcPackages(List<String> packages) {
        Preconditions.checkNullElements(packages);
        rpcPackages.addAll(packages);
        return this;
    }
    // endregion

    // region worker

    @Override
    public NodeBuilder setParent(Node parent) {
        super.setParent(parent);
        return this;
    }

    @Override
    public NodeBuilder setThreadFactory(ThreadFactory threadFactory) {
        super.setThreadFactory(threadFactory);
        return this;
    }

    @Override
    public NodeBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        super.setRejectedExecutionHandler(rejectedExecutionHandler);
        return this;
    }

    @Override
    public WorkerBuilder setAgent(IEventLoopAgent<WorkerEvent> agent) {
        super.setAgent(agent);
        return this;
    }

    @Override
    public NodeBuilder setControlData(WorkerControlData controlData) {
        super.setControlData(controlData);
        return this;
    }

    @Override
    public NodeBuilder setManualClose(Boolean manualClose) {
        super.setManualClose(manualClose);
        return this;
    }

    @Override
    public NodeBuilder setWorkerId(String workerId) {
        super.setWorkerId(workerId);
        return this;
    }

    @Override
    public NodeBuilder setInjector(Injector injector) {
        super.setInjector(injector);
        return this;
    }

    @Override
    public NodeBuilder addModule(Class<?> moduleClazz) {
        super.addModule(moduleClazz);
        return this;
    }

    @Override
    public NodeBuilder addService(Class<?> serviceClass) {
        super.addService(serviceClass);
        return this;
    }

    @Override
    public NodeBuilder addModules(List<Class<?>> moduleClazz) {
        super.addModules(moduleClazz);
        return this;
    }

    @Override
    public NodeBuilder addServices(List<Class<?>> serviceClass) {
        super.addServices(serviceClass);
        return this;
    }

    // endregion

    public static DefaultNodeBuilder newDefaultNodeBuilder() {
        return new DefaultNodeBuilder();
    }

}