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

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class ArrayCopyTest {

    /**
     * 测试Object[]数组是否可以快速拷贝到int[]
     */
    @Test
    void objArrayCopyToIntArray() {
        final Object[] src = {1, 2, 3, 4, 5};
        //noinspection MismatchedReadAndWriteOfArray
        final int[] tar = new int[src.length];
        Assertions.assertThrowsExactly(ArrayStoreException.class,
                () -> System.arraycopy(src, 0, tar, 0, src.length)
        );
    }

    @Test
    void intArrayCopyToObjArray() {
        final int[] src = {1, 2, 3, 4, 5};
        //noinspection MismatchedReadAndWriteOfArray
        final Object[] tar = new Object[src.length];
        Assertions.assertThrowsExactly(ArrayStoreException.class,
                () -> System.arraycopy(src, 0, tar, 0, src.length)
        );
    }
}