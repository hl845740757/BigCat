package cn.wjybxx.common;

import cn.wjybxx.common.dson.DsonEnum;
import cn.wjybxx.common.dson.DsonEnumMapper;

import javax.annotation.Nullable;

/**
 * 关系运算符枚举
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public enum RelationalOperator implements DsonEnum {

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
            return a >= b;
        }

        @Override
        public boolean test(double a, double b) {
            return a >= b;
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
            return a <= b;
        }

        @Override
        public boolean test(double a, double b) {
            return a <= b;
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

    private static final DsonEnumMapper<RelationalOperator> MAPPER = EnumUtils.mapping(values());

    @Nullable
    public static RelationalOperator forNumber(int number) {
        return MAPPER.forNumber(number);
    }

    public static RelationalOperator checkedForNumber(int number) {
        return MAPPER.checkedForNumber(number);
    }
}