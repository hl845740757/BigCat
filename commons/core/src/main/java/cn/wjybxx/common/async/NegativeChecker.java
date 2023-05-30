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

/**
 * 负值处理策略
 *
 * @author wjybxx
 * date 2023/4/6
 */
public enum NegativeChecker {

    /** 允许负数值 -- 不建议使用 */
    SUCCESS,
    /** 失败 - 抛出异常 */
    FAILURE,
    /** 转为0 */
    ZERO;

    public long check(long value) {
        if (value >= 0) {
            return value;
        }
        switch (this) {
            case SUCCESS -> {
                return value;
            }
            case FAILURE -> {
                throw new IllegalArgumentException("initialDelay must be gte 0, value: " + value);
            }
            case ZERO -> {
                return 0;
            }
        }
        throw new AssertionError();
    }

}