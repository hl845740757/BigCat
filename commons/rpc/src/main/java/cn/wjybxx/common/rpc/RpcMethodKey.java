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

package cn.wjybxx.common.rpc;

/**
 * rpc方法键工具类
 *
 * @author wjybxx
 * date - 2023/10/13
 */
public class RpcMethodKey {

    private static final int FACTOR = 10000;

    public static int methodKey(int serviceId, int methodId) {
        if (methodId < 0 || methodId >= FACTOR) {
            throw new IllegalArgumentException("methodId must be between [0, 9999]");
        }
        // 使用乘法更直观，更有规律；负数需要转正数，计算后再转负数
        if (serviceId < 0) {
            return -1 * (Math.abs(serviceId) * FACTOR + methodId);
        } else {
            return serviceId * FACTOR + methodId;
        }
    }

    public static int serviceIdOfKey(int methodKey) {
        if (methodKey < 0) {
            return -1 * (Math.abs(methodKey) / FACTOR);
        } else {
            return methodKey / FACTOR;
        }
    }

    public static int methodIdOfKey(int methodKey) {
        if (methodKey < 0) {
            return Math.abs(methodKey) % 10000;
        }
        return methodKey % 10000;
    }

}