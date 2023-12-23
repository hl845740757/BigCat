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

package cn.wjybxx.common.concurrent;

import cn.wjybxx.common.annotation.Internal;

/**
 * future的聚合选项
 *
 * @author wjybxx
 * date 2023/4/12
 */
@Internal
public final class AggregateOptions {

    private final boolean anyOf;
    public final int successRequire;
    public final boolean lazy;

    AggregateOptions(boolean anyOf, int successRequire, boolean lazy) {
        this.anyOf = anyOf;
        this.successRequire = successRequire;
        this.lazy = lazy;
    }

    public boolean isAnyOf() {
        return anyOf;
    }

    private static final AggregateOptions ANY = new AggregateOptions(true, 0, false);

    public static AggregateOptions anyOf() {
        return ANY;
    }

    public static AggregateOptions selectN(int successRequire, boolean lazy) {
        if (successRequire < 0) {
            throw new IllegalArgumentException("successRequire < 0");
        }
        return new AggregateOptions(false, successRequire, lazy);
    }
}