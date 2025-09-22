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
import cn.wjybxx.concurrent.EventLoopModule;
import cn.wjybxx.concurrent.IEventLoopAgent;
import cn.wjybxx.concurrent.RejectedExecutionHandler;
import com.google.inject.Injector;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ThreadFactory;

/**
 * 建议先设置子类属性再设置父类属性，避免过多的重写
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public abstract class WorkerBuilder {

    private String workerId;
    /**
     * Worker上绑定的容器，需要包含：
     * {@link IEventLoopAgent}、{@link TimeModule}
     * {@link RpcClient}、{@link RpcMethodRegistry}、
     * {@link S2SSessionMgr}
     * <p>
     * 如果是Node，则还需要包含：
     * {@link RpcSupport}、{@link RpcRouter}、{@link RpcSerializer}、
     */
    private Injector injector;

    /**
     * Worker上挂载的模块类
     * 1.需要能通过{@link #injector}获取实例
     * 2.添加顺序很重要，Worker将按照添加顺序启动所有的Module
     * 3.实现类必须是{@link EventLoopModule}的子类（注入的接口则不一定）
     */
    private final List<Class<?>> moduleClasses = new ArrayList<>();
    /**
     * Worker上挂载的服务类
     * 1.服务接口的实例必须在容器中存在
     * 2.服务会自动导出
     */
    private final List<Class<?>> serviceClasses = new ArrayList<>();

    /** 在真正构建时由{@link Node}赋值，用户需要设置到parent */
    private WorkerControlData controlData;
    /** 是否手动关闭Worker -- 如果未赋值，则取决于添加到Node时是否已启动 */
    private Boolean manualClose;
    /** Builder之间不方便继承 */
    protected final EventLoopBuilder<WorkerEvent> delegated;

    protected WorkerBuilder(EventLoopBuilder<WorkerEvent> delegated) {
        this.delegated = Objects.requireNonNull(delegated);
    }

    public EventLoopBuilder<WorkerEvent> getDelegated() {
        return delegated;
    }

    public abstract Worker build();

    // region event-loop

    public Node getParent() {
        return (Node) delegated.getParent();
    }

    public WorkerBuilder setParent(Node parent) {
        delegated.setParent(parent);
        return this;
    }

    public ThreadFactory getThreadFactory() {
        return delegated.getThreadFactory();
    }

    public WorkerBuilder setThreadFactory(ThreadFactory threadFactory) {
        delegated.setThreadFactory(threadFactory);
        return this;
    }

    public int getBatchSize() {
        return delegated.getBatchSize();
    }

    public WorkerBuilder setBatchSize(int batchSize) {
        delegated.setBatchSize(batchSize);
        return this;
    }

    public RejectedExecutionHandler getRejectedExecutionHandler() {
        return delegated.getRejectedExecutionHandler();
    }

    public WorkerBuilder setRejectedExecutionHandler(RejectedExecutionHandler rejectedExecutionHandler) {
        delegated.setRejectedExecutionHandler(rejectedExecutionHandler);
        return this;
    }

    public IEventLoopAgent<WorkerEvent> getAgent() {
        return delegated.getAgent();
    }

    public WorkerBuilder setAgent(IEventLoopAgent<WorkerEvent> agent) {
        delegated.setAgent(agent);
        return this;
    }

    // endregion


    //region worker

    public WorkerControlData getControlData() {
        return controlData;
    }

    public WorkerBuilder setControlData(WorkerControlData controlData) {
        this.controlData = controlData;
        return this;
    }

    public Boolean getManualClose() {
        return manualClose;
    }

    public WorkerBuilder setManualClose(Boolean manualClose) {
        this.manualClose = manualClose;
        return this;
    }

    public String getWorkerId() {
        return workerId;
    }

    public WorkerBuilder setWorkerId(String workerId) {
        this.workerId = workerId;
        return this;
    }

    public Injector getInjector() {
        return injector;
    }

    public WorkerBuilder setInjector(Injector injector) {
        this.injector = injector;
        return this;
    }

    public List<Class<?>> getModuleClasses() {
        return moduleClasses;
    }

    public WorkerBuilder addModule(Class<?> moduleClazz) {
        Objects.requireNonNull(moduleClazz);
        moduleClasses.add(moduleClazz);
        return this;
    }

    public WorkerBuilder addModules(List<Class<?>> moduleClazz) {
        Preconditions.checkNullElements(moduleClazz);
        moduleClasses.addAll(moduleClazz);
        return this;
    }

    public List<Class<?>> getServiceClasses() {
        return serviceClasses;
    }

    public WorkerBuilder addService(Class<?> serviceClass) {
        Objects.requireNonNull(serviceClass);
        serviceClasses.add(serviceClass);
        return this;
    }

    public WorkerBuilder addServices(List<Class<?>> serviceClass) {
        Preconditions.checkNullElements(serviceClass);
        serviceClasses.addAll(serviceClass);
        return this;
    }

    // endregion

    public static DefaultWorkerBuilder newDefaultWorkerBuilder() {
        return new DefaultWorkerBuilder();
    }

}