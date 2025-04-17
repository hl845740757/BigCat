/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import com.google.inject.AbstractModule;
import com.google.inject.Guice;
import com.google.inject.Injector;
import com.google.inject.Singleton;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * 验证父子注入器共享实例的情况
 *
 * @author wjybxx
 * date - 2025/4/17
 */
public class InjectorTest {

    @Test
    public void test() {
        Module module = new Module();
        Injector injector1 = Guice.createInjector(module);
        Injector injector2 = Guice.createInjector(module);

        RpcClient rpcClient1 = injector1.getInstance(RpcClient.class);
        RpcClient rpcClient2 = injector2.getInstance(RpcClient.class);
        Assertions.assertNotSame(rpcClient1, rpcClient2);


        Injector childInjector = injector1.createChildInjector(new Module2());
        RpcClient rpcClient3 = childInjector.getInstance(RpcClient.class);
        Assertions.assertSame(rpcClient1, rpcClient3);
    }

    private static class Module extends AbstractModule {

        @Override
        protected void configure() {
            binder().requireExplicitBindings();
            bind(RpcClient.class).to(S2SRpcClient.class).in(Singleton.class);
        }
    }

    private static class Module2 extends AbstractModule {

        @Override
        protected void configure() {
            binder().requireExplicitBindings();
            bind(RpcRouter.class).to(TestRpcRouter.class).in(Singleton.class);
        }
    }
}