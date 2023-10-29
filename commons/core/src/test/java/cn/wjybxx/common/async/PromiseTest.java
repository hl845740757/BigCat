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

package cn.wjybxx.common.async;

import org.apache.commons.lang3.mutable.MutableInt;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.stream.IntStream;

/**
 * @author wjybxx
 * date - 2023/10/16
 */
public class PromiseTest {

    @Test
    void testOrder() {
        int count = 4;
        FluentPromise<String> promise = SameThreads.newPromise();
        MutableInt counter = new MutableInt(0);
        IntStream.range(0, count).forEach(idx -> {
            promise.thenAccept(s -> {
                Assertions.assertEquals(idx, counter.getValue());
                counter.increment();
            });
        });

        promise.complete("");
        Assertions.assertEquals(count, counter.getValue());
    }

    public static void main(String[] args) {
        System.setProperty(AbstractPromise.propKey, "true");

        final int count = 4;
        FluentPromise<String> promise = SameThreads.newPromise();
        MutableInt counter = new MutableInt(0);
        IntStream.range(0, count).forEach(idx -> {
            promise.thenAccept(s -> {
                int expected = count - idx - 1;
                Assertions.assertEquals(expected, counter.getValue());
                counter.increment();
            });
        });

        promise.complete("");
        Assertions.assertEquals(count, counter.getValue());
    }
}
