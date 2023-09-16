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

package cn.wjybxx.common;

import cn.wjybxx.common.annotation.AutoFields;
import cn.wjybxx.common.eventbus.DefaultEventBus;
import cn.wjybxx.common.eventbus.Subscribe;
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
@AutoFields(skipStatic = false)
public class SubscribeExample {

    public static final String MASTER_BEGIN = "begin";
    public static final String MASTER_END = "end";

    public static final String CHILD_P1 = "cp1";
    public static final String CHILD_P2 = "cp2";
    public static final String CHILD_P3 = "cp3";

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

    // region

    /** 引用类字段的方式声明主键和子键 */
    @Subscribe(masterDeclared = SubscribeExample.class, masterKey = "MASTER_BEGIN")
    public void subscribe1(Object event) {

    }

    /** 引用类字段的方式声明主键和子键 */
    @Subscribe(
            masterDeclared = SubscribeExample.class, masterKey = "MASTER_BEGIN",
            childDeclared = SubscribeExample.class, childKeys = {"CHILD_P1", "CHILD_P2"}
    )
    public void subscribe2(Object event) {

    }

    /** 使用字符串主键和子键 */
    @Subscribe(masterKey = "begin")
    public void subscribe3(Object event) {

    }

    /** 使用字符串主键和子键 */
    @Subscribe(
            masterKey = "begin",
            childKeys = {"cp1", "cp2"}
    )
    public void subscribe4(Object event) {

    }

    // endregion

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