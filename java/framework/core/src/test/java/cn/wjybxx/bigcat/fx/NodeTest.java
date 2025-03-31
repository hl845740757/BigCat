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

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.concurrent.IEventLoopAgent;
import com.google.inject.AbstractModule;
import com.google.inject.Guice;
import com.google.inject.Injector;
import com.google.inject.Singleton;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.slf4j.LoggerFactory;

/**
 * @author wjybxx
 * date - 2023/10/5
 */
public class NodeTest {

    private static Node node;

    /** 准备工作有点多 */
    @BeforeAll
    static void setUp() {
        // 先初始化Logger，避免污染输出
        LoggerFactory.getLogger("Main");
        System.out.println();

        node = NodeBuilder.newDefaultNodeBuilder()
                .setNodeAddr(new WorkerAddr(1, 1))
                .setWorkerId("Node")
                // 初始化模块
                .setInjector(createNodeInjector())
                .addModule(RpcClient.class)
                .addModule(RpcSupport.class)
                .addModule(TestRpcRouter.class)
                // 初始化rpc接口包
                .addRpcPackage(TestRpcRouter.class.getPackageName())
                // 初始化Worker，1号worker是client，2号是server，否则无法支持同步调用
                .setNumberChildren(2)
                .setWorkerFactory((parent, index, workerCtx) -> {
                    WorkerBuilder.DisruptWorkerBuilder workerBuilder = WorkerBuilder.newDisruptorWorkerBuilder()
                            .setWorkerId("Worker-" + index)
                            .setParent(parent)
                            .setWorkerCtx(workerCtx)
                            // 初始化模块
                            .setInjector(createWorkerInjector())
                            .addModule(RpcClient.class);
                    // 初始化rpc服务
                    if (index == 0) {
                        workerBuilder.addModule(RpcClientExample.class)
                                .addService(RpcClientExample.class);
                    } else {
                        workerBuilder.addModule(RpcServiceExample.class)
                                .addService(RpcServiceExample.class);
                    }
                    return workerBuilder.build();
                })
                .build();

        node.start().join();
    }

    @AfterAll
    static void tearDown() {
        if (node != null) {
            node.shutdownNow();
            node.terminationFuture().join();
        }
    }

    @Test
    void test() {
        // 查看日志
        ThreadUtils.sleepQuietly(10 * 1000);
        node.shutdown();
    }

    private static Injector createNodeInjector() {
        return Guice.createInjector(new AbstractModule() {
            @Override
            protected void configure() {
                super.configure();
                // 获取未显式绑定的实例时抛出异常，避免获取到错误的实例；一定要声明，否则极易出bug
                binder().requireExplicitBindings();

                bind(IEventLoopAgent.class).to(DefaultMainModule.class).in(Singleton.class);
                bind(RpcClient.class).to(WorkerRpcClient.class).in(Singleton.class);
                bind(RpcRegistry.class).to(DefaultRpcRegistry.class).in(Singleton.class);
                bind(TimeProvider.class).to(TimeModule.class).in(Singleton.class);
                bind(TimeModule.class).in(Singleton.class); // 部分地方依赖的是TimeProvider

                // 要想直接注入子类，子类也需要显式绑定
                // 子类如果不单独绑定，则会创建一个新的实例，各种bug...
                bind(TestRpcRouter.class).in(Singleton.class);
                bind(RpcRouter.class).to(TestRpcRouter.class).in(Singleton.class);

                bind(RpcSupport.class).in(Singleton.class);
                bind(RpcSerializer.class).to(TestRpcSerializer.class).in(Singleton.class);
                bind(RpcMethodRegistry.class).in(Singleton.class);
            }
        });
    }

    private static Injector createWorkerInjector() {
        return Guice.createInjector(new AbstractModule() {
            @Override
            protected void configure() {
                super.configure();
                binder().requireExplicitBindings();

                bind(IEventLoopAgent.class).to(DefaultMainModule.class).in(Singleton.class);
                bind(RpcClient.class).to(WorkerRpcClient.class).in(Singleton.class);
                bind(RpcRegistry.class).to(DefaultRpcRegistry.class).in(Singleton.class);
                bind(TimeProvider.class).to(TimeModule.class).in(Singleton.class);
                bind(TimeModule.class).in(Singleton.class); // 部分地方依赖的是TimeProvider

                bind(RpcClientExample.class).in(Singleton.class);
                bind(RpcServiceExample.class).in(Singleton.class);
            }
        });
    }

}