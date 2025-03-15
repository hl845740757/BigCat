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
import com.google.inject.Injector;
import org.apache.commons.lang3.ArrayUtils;

import java.lang.reflect.Method;
import java.lang.reflect.Parameter;
import java.lang.reflect.ParameterizedType;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.concurrent.Future;

/**
 * 框架辅助类
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public class FxUtils {

    /** worker发到node的rpc请求 - 发送，包含worker,request，promise */
    public static final int TYPE_WORKER_NODE_REQUEST = 1;
    /** worker发到node的rpc响应 - 发送，包含Response */
    public static final int TYPE_WORKER_NODE_RESPONSE = 2;

    /** 收到网络层的Request - 接收 */
    public static final int TYPE_NET_NODE_REQUEST = 3;
    /** 收到网络层的Response - 接收 */
    public static final int TYPE_NET_NODE_RESPONSE = 4;

    /** node发到worker的rpc请求 - 派发请求，包含worker,request */
    public static final int TYPE_NODE_WORKER_REQUEST = 5;
    /** node发到worker的rpc结果 - 设置Promise，包含Response, Promise */
    public static final int TYPE_NODE_WORKER_RESPONSE = 6;

    /** 筛选需要每帧Update的Module */
    public static List<WorkerModule> filterUpdatableModules(List<WorkerModule> workerModules) {
        final List<WorkerModule> result = new ArrayList<>(workerModules.size());
        for (WorkerModule workerModule : workerModules) {
            if (workerModule instanceof MainModule || isOverrideUpdate(workerModule)) {
                result.add(workerModule);
            }
        }
        return result;
    }

    public static boolean isOverrideUpdate(WorkerModule workerModule) {
        try {
            Method method = workerModule.getClass().getMethod("update", ArrayUtils.EMPTY_CLASS_ARRAY);
            return !method.getDeclaringClass().isInterface();
        } catch (NoSuchMethodException ignore) {
            return false;
        }
    }

    /** 获取所有的模块 */
    public static List<WorkerModule> createModules(WorkerBuilder builder) {
        Injector injector = builder.getInjector();
        MainModule mainModule = injector.getInstance(MainModule.class);

        List<WorkerModule> moduleList = new ArrayList<>(builder.getModuleClasses().size() + 1);
        moduleList.add(mainModule);
        for (Class<?> moduleClass : builder.getModuleClasses()) {
            WorkerModule workerModule = (WorkerModule) injector.getInstance(moduleClass);
            if (CollectionUtils.containsRef(moduleList, workerModule)) {
                throw new IllegalArgumentException("Duplicate Module: " + moduleClass);
            }
            moduleList.add(workerModule);
        }
        return moduleList;
    }

    /** 导出Rpc服务 */
    public static void exportService(WorkerBuilder builder) {
        Injector injector = builder.getInjector();
        RpcRegistry registry = injector.getInstance(RpcRegistry.class);
        for (Class<?> clazz : builder.getServiceClasses()) {
            Object instance = injector.getInstance(clazz);
            exportService(registry, clazz, instance);
        }
    }

    /** 导出Rpc服务 */
    public static void exportService(RpcRegistry registry, Class<?> serviceInterface, Object serviceImpl) {
        if (!serviceInterface.isInstance(serviceImpl)) {
            throw new IllegalArgumentException();
        }
        // public static void export(RpcRegistry registry, RpcServiceExample instance) {}
        try {
            Class<?> exporter = Class.forName(serviceInterface.getName() + "Exporter");
            Method method = exporter.getDeclaredMethod("export", RpcRegistry.class, serviceInterface); // 生成的静态export方法
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
                // 注册方法
                RpcMethodInfo<?, ?> methodInfo = new RpcMethodInfo<>(
                        serviceInterface.getSimpleName(), method.getName(),
                        serviceAnno.serviceId(), methodAnno.methodId(),
                        pType, rType);
                registry.register(methodInfo);
            }
        } catch (Exception e) {
            throw new RuntimeException("service:" + serviceInterface.getSimpleName(), e);
        }
    }
}