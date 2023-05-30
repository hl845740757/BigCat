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

/**
 * 用于传递递归限制信息
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public final class RecursionLimiter {

    private final int limit;
    private int curDep;

    public RecursionLimiter(int limit) {
        this.limit = Preconditions.checkPositive(limit, "limit");
    }

    public int getLimit() {
        return limit;
    }

    public int getCurDep() {
        return curDep;
    }

    public void increment() {
        if (curDep == limit) {
            throw new RecursionExceedLimitException("recursion exceeds limit: " + limit);
        }
        curDep++;
    }

    public void decrement() {
        if (curDep == 0) {
            throw new IllegalStateException("call decrement when dep is 0");
        }
        curDep--;
    }

    public static class RecursionExceedLimitException extends IllegalStateException {

        public RecursionExceedLimitException() {
        }

        public RecursionExceedLimitException(String message) {
            super(message);
        }
    }

}