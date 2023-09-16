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
package cn.wjybxx.common.pool;

import java.util.Collection;
import java.util.Map;

/**
 * 对象池对象的重置策略。
 * 使用组合的方式更加灵活，尤其是遇到一些非自己定义的类对象时。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@FunctionalInterface
public interface ResetPolicy<V> {

    void reset(V object);

    ResetPolicy<? super Collection<?>> CLEAR = Collection::clear;
    ResetPolicy<? super Map<?, ?>> CLEAR_MAP = Map::clear;
    ResetPolicy<Object> DO_NOTHING = V -> {};

}