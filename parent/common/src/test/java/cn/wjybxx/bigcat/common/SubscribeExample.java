/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common;

import cn.wjybxx.bigcat.common.eventbus.DefaultEventBus;
import cn.wjybxx.bigcat.common.eventbus.Subscribe;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 编译之后，将 parent/common/target/generated-test-sources/test-annotations 设置为 test-resource 目录，
 * 就可以看见生成的代码是什么样的，也可以查看到调用。
 *
 * @author wjybxx
 * date 2023/4/7
 */
public class SubscribeExample {

    static final AtomicInteger counter = new AtomicInteger();

    @Subscribe
    public void subscribeList(SimpleGenericEvent<ArrayList<?>> listEvent) {
        counter.incrementAndGet();
    }

    @Subscribe
    public void subscribeSet(SimpleGenericEvent<HashSet<?>> hashSetEvent) {
        counter.incrementAndGet();
    }

    @Subscribe
    public void subscribeString(SimpleGenericEvent<String> stringEvent) {
        counter.incrementAndGet();
    }

    @Test
    void subscribeTest() {
        DefaultEventBus eventBus = new DefaultEventBus();
        // 绑定监听
        SubscribeExampleBusRegister.register(eventBus, new SubscribeExample());

        final int eventCount = 300;
        for (int i = 0; i < eventCount; i++) {
            int mod = i % 3;
            switch (mod) {
                case 1 -> eventBus.post(new SimpleGenericEvent<>(new ArrayList<>()));
                case 2 -> eventBus.post(new SimpleGenericEvent<>(new HashSet<>()));
                default -> eventBus.post(new SimpleGenericEvent<>("test"));
            }
        }
        Assertions.assertEquals(eventCount, counter.get());
    }
}