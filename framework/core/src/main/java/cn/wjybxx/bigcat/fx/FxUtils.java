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

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.common.rpc.RpcRegistry;
import com.google.inject.Injector;
import org.apache.commons.lang3.ArrayUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.List;

/**
 * 框架辅助类
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public class FxUtils {

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
            method.invoke(null, registry, serviceInterface);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }
}