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

package cn.wjybxx.bigcat.util;


import cn.wjybxx.common.EnumLite;
import cn.wjybxx.common.EnumLiteMap;
import cn.wjybxx.common.EnumUtils;
import cn.wjybxx.common.MathUtils;
import cn.wjybxx.common.codec.binary.BinarySerializable;

import javax.annotation.Nullable;

/**
 * 关系运算符枚举
 *
 * @author wjybxx
 * date - 2023/4/27
 */
@BinarySerializable
public enum RelationalOperator implements EnumLite {

    gt(1) {
        @Override
        public boolean test(int a, int b) {
            return a > b;
        }

        @Override
        public boolean test(long a, long b) {
            return a > b;
        }

        @Override
        public boolean test(float a, float b) {
            return a > b;
        }

        @Override
        public boolean test(double a, double b) {
            return a > b;
        }
    },
    gte(2) {
        @Override
        public boolean test(int a, int b) {
            return a >= b;
        }

        @Override
        public boolean test(long a, long b) {
            return a >= b;
        }

        @Override
        public boolean test(float a, float b) {
            return a > b || MathUtils.isEqual(a, b);
        }

        @Override
        public boolean test(double a, double b) {
            return a > b || MathUtils.isEqual(a, b);
        }
    },

    lt(3) {
        @Override
        public boolean test(int a, int b) {
            return a < b;
        }

        @Override
        public boolean test(long a, long b) {
            return a < b;
        }

        @Override
        public boolean test(float a, float b) {
            return a < b;
        }

        @Override
        public boolean test(double a, double b) {
            return a < b;
        }
    },
    lte(4) {
        @Override
        public boolean test(int a, int b) {
            return a <= b;
        }

        @Override
        public boolean test(long a, long b) {
            return a <= b;
        }

        @Override
        public boolean test(float a, float b) {
            return a < b || MathUtils.isEqual(a, b);
        }

        @Override
        public boolean test(double a, double b) {
            return a < b || MathUtils.isEqual(a, b);
        }
    },

    eq(5) {
        @Override
        public boolean test(int a, int b) {
            return a == b;
        }

        @Override
        public boolean test(long a, long b) {
            return a == b;
        }

        @Override
        public boolean test(float a, float b) {
            return MathUtils.isEqual(a, b);
        }

        @Override
        public boolean test(double a, double b) {
            return MathUtils.isEqual(a, b);
        }
    },

    neq(6) {
        @Override
        public boolean test(int a, int b) {
            return a != b;
        }

        @Override
        public boolean test(long a, long b) {
            return a != b;
        }

        @Override
        public boolean test(float a, float b) {
            return !MathUtils.isEqual(a, b);
        }

        @Override
        public boolean test(double a, double b) {
            return !MathUtils.isEqual(a, b);
        }
    },

    ;

    private final int number;

    RelationalOperator(int number) {
        this.number = number;
    }

    @Override
    public int getNumber() {
        return number;
    }

    public abstract boolean test(int a, int b);

    public abstract boolean test(long a, long b);

    public abstract boolean test(float a, float b);

    public abstract boolean test(double a, double b);

    public boolean test(boolean a, boolean b) {
        return switch (this) {
            case eq -> a == b;
            case neq -> a != b;
            default -> throw new IllegalStateException("invalid operator on bool " + this);
        };
    }

    private static final EnumLiteMap<RelationalOperator> MAPPER = EnumUtils.mapping(values());

    @Nullable
    public static RelationalOperator forNumber(int number) {
        return MAPPER.forNumber(number);
    }

    public static RelationalOperator checkedForNumber(int number) {
        return MAPPER.checkedForNumber(number);
    }
}