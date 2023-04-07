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

import cn.wjybxx.bigcat.common.eventbus.Subscribe;

import java.util.ArrayList;
import java.util.Collection;
import java.util.HashSet;

/**
 * 编译之后，将 parent/common/target/generated-test-sources/test-annotations 设置为 test-resource 目录，
 * 就可以看见生成的代码是什么样的，也可以查看到调用。
 *
 * @author wjybxx
 * date 2023/4/7
 */
public class SubscribeExample {

    @Subscribe
    public void subscribeList(CollectionEvent<ArrayList<?>> listEvent) {

    }

    @Subscribe
    public void subscribeSet(CollectionEvent<HashSet<?>> hashSetEvent) {

    }

    @Subscribe(childEvents = {ArrayList.class, HashSet.class})
    public void subscribeCollection(CollectionEvent<Collection<?>> hashSetEvent) {

    }

    @Subscribe(childEvents = {String.class, Integer.class})
    public String subscribeObject(Object objEvent) {
        return objEvent.toString();
    }

}