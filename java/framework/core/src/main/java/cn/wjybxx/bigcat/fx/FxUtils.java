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

import cn.wjybxx.base.ClassScanner;
import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.SystemPropsUtils;
import cn.wjybxx.concurrent.EventLoopModule;
import com.google.inject.Injector;

import java.lang.reflect.Method;
import java.lang.reflect.Parameter;
import java.lang.reflect.ParameterizedType;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.concurrent.CopyOnWriteArraySet;
import java.util.concurrent.Future;

/**
 * 框架辅助类
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public class FxUtils {

    /** 当前线程运行的Worker，Node也会发布到这里。 */
    public final static ThreadLocal<Worker> CURRENT_WORKER = new ThreadLocal<>();
    /**
     * 当前运行中的所有Node -- 用于未来支持单进程下启动多个服务器。
     * <p>
     * 经过反复地思考权衡，允许一个进程内启动多个Node是简单可靠的方式，代价是增加一部分开销 -- 不会太多。
     * 如果在一个Node内启动多个服务器，虽然资源利用率更高，但编程复杂，尤其对Rpc客户端不友好。
     * 如果需要查询当前线程的Node，可通过Worker查询。
     */
    public final static CopyOnWriteArraySet<Node> CURRENT_NODES = new CopyOnWriteArraySet<>();
    /** rpc对象池的大小 */
    public static final int RPC_POOL_SIZE = SystemPropsUtils.getInt("Wjybxx.BigCat.Fx.RpcPoolSize", 1024);

    /** worker发到node的rpc请求 - 发送，包含request，promise */
    public static final int TYPE_WORKER_NODE_REQUEST = 1;
    /** worker发到node的rpc响应 - 发送，包含Response */
    public static final int TYPE_WORKER_NODE_RESPONSE = 2;

    /** 收到网络层的Request - 接收 */
    public static final int TYPE_NET_NODE_REQUEST = 3;
    /** 收到网络层的Response - 接收 */
    public static final int TYPE_NET_NODE_RESPONSE = 4;

    /** node发到worker的rpc请求 - 派发请求，包含request */
    public static final int TYPE_NODE_WORKER_REQUEST = 5;
    /** node发到worker的rpc结果 - 设置Promise，包含Response, Promise */
    public static final int TYPE_NODE_WORKER_RESPONSE = 6;

    /** 创建所有的模块 */
    public static void createModules(WorkerBuilder builder) {
        List<EventLoopModule> moduleList = new ArrayList<>(builder.getModuleClasses().size());
        if (builder.getDelegated().getModules().size() > 0) {
            moduleList.addAll(builder.getDelegated().getModules());
        }
        Injector injector = builder.getInjector();
        for (Class<?> moduleClass : builder.getModuleClasses()) {
            EventLoopModule workerModule = (EventLoopModule) injector.getInstance(moduleClass);
            if (CollectionUtils.containsRef(moduleList, workerModule)) {
                throw new IllegalArgumentException("Duplicate Module: " + moduleClass);
            }
            moduleList.add(workerModule);
        }
        for (EventLoopModule module : moduleList) {
            builder.getDelegated().addModule(module);
        }
    }

    /** 导出Rpc服务 */
    public static void exportService(WorkerBuilder builder) {
        Injector injector = builder.getInjector();
        RpcMethodRegistry registry = injector.getInstance(RpcMethodRegistry.class);
        for (Class<?> clazz : builder.getServiceClasses()) {
            Object instance = injector.getInstance(clazz);
            exportService(registry, clazz, instance);
        }
    }

    /** 导出Rpc服务 */
    public static void exportService(RpcMethodRegistry registry, Class<?> serviceInterface, Object serviceImpl) {
        if (!serviceInterface.isInstance(serviceImpl)) {
            throw new IllegalArgumentException("interface: %s, impl: %s".formatted(serviceInterface, serviceImpl.getClass()));
        }
        // public static void export(RpcMethodRegistry registry, RpcServiceExample instance) {}
        try {
            Class<?> exporter = Class.forName(serviceInterface.getName() + "Proxy");
            Method method = exporter.getDeclaredMethod("export", RpcMethodRegistry.class, serviceInterface); // 生成的静态export方法
            method.invoke(null, registry, serviceImpl);
        } catch (Exception e) {
            throw new RuntimeException("service:" + serviceInterface.getSimpleName(), e);
        }
    }

    /** 导出rpc方法信息 */
    public static void exportMethodInfo(NodeBuilder builder) {
        RpcMethodRegistry registry = builder.getInjector().getInstance(RpcMethodRegistry.class);
        for (String pkg : builder.getRpcPackages()) {
            Set<Class<?>> classSet = ClassScanner.findClasses(pkg, c -> true, c -> c.isAnnotationPresent(RpcService.class));
            for (Class<?> serviceInterface : classSet) {
                exportMethodInfo(registry, serviceInterface);
            }
        }
    }

    /** 导出rpc方法信息 */
    public static void exportMethodInfo(RpcMethodRegistry registry, Class<?> serviceInterface) {
        RpcService serviceAnno = serviceInterface.getAnnotation(RpcService.class);
        if (serviceAnno == null) {
            throw new IllegalArgumentException("target is not RpcService: " + serviceInterface);
        }
        try {
            Method[] methods = serviceInterface.getMethods();
            for (Method method : methods) {
                RpcMethod methodAnno = method.getAnnotation(RpcMethod.class);
                if (methodAnno == null) {
                    continue;
                }
                // 获取RpcContext的类型和方法参数类型
                ParameterizedType ctxType;
                Class<?> pType;
                Parameter[] parameters = method.getParameters();
                if (parameters.length > 0 && RpcContext.class.isAssignableFrom(parameters[0].getType())) {
                    ctxType = (ParameterizedType) parameters[0].getParameterizedType();
                    pType = parameters.length > 1 ? parameters[1].getType() : null;
                } else {
                    ctxType = null;
                    pType = parameters.length > 0 ? parameters[0].getType() : null;
                }
                // 返回值类型可能在Future和RpcContext的泛型参数中
                Class<?> rType;
                if (ctxType != null) {
                    rType = (Class<?>) ctxType.getActualTypeArguments()[0];
                } else {
                    rType = method.getReturnType();
                    if (Future.class.isAssignableFrom(rType)) {
                        ParameterizedType genericReturnType = (ParameterizedType) method.getGenericReturnType();
                        rType = (Class<?>) genericReturnType.getActualTypeArguments()[0];
                    }
                }
                rType = voidToObject(rType);
                // 注册方法
                RpcMethodInfo methodInfo = new RpcMethodInfo(
                        serviceInterface.getSimpleName(), method.getName(),
                        serviceAnno.serviceId(), methodAnno.methodId(),
                        pType, rType);
                registry.register(methodInfo);
            }
        } catch (Exception e) {
            throw new RuntimeException("service:" + serviceInterface.getName(), e);
        }
    }

    private static Class<?> voidToObject(Class<?> clazz) {
        if (clazz == null || clazz == Void.class || clazz == void.class) {
            return Object.class;
        }
        return clazz;
    }
}