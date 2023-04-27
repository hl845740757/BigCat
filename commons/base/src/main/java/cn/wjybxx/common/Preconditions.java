package cn.wjybxx.common;

import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date - 2023/4/27
 */
public class Preconditions {

    //
    public static void checkArgument(boolean expression) {
        if (!expression) {
            throw new IllegalArgumentException();
        }
    }

    public static void checkArgument(boolean expression, @Nullable Object message) {
        if (!expression) {
            throw new IllegalArgumentException(String.valueOf(message));
        }
    }

    public static <T> T checkNotNull(T v) {
        if (v == null) throw new NullPointerException();
        return v;
    }

    public static <T> T checkNotNull(T v, @Nullable Object msg) {
        if (v == null) throw new NullPointerException(String.valueOf(msg));
        return v;
    }

    public static void checkState(boolean expression) {
        if (!expression) {
            throw new IllegalStateException();
        }
    }

    public static void checkState(boolean expression, @Nullable Object message) {
        if (!expression) {
            throw new IllegalStateException(String.valueOf(message));
        }
    }

    //
    public static int checkPositive(int v, String name) {
        if (v <= 0) {
            throw new IllegalArgumentException(String.format("%s expected positive, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static long checkPositive(long v, String name) {
        if (v <= 0) {
            throw new IllegalArgumentException(String.format("%s expected positive, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static int checkNonNegative(int v, String name) {
        if (v < 0) {
            throw new IllegalArgumentException(String.format("%s expected nonnegative, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    public static long checkNonNegative(long v, String name) {
        if (v < 0) {
            throw new IllegalArgumentException(String.format("%s expected nonnegative, but found: %d", nullToDef(name, "value"), v));
        }
        return v;
    }

    //
    public static int checkBetweenRange(int v, int min, int max) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(String.format("value expected between range[%d, %d], but found: %d", min, max, v));
        }
        return v;
    }

    public static long checkBetweenRange(long v, long min, long max) {
        if (v < min || v > max) {
            throw new IllegalArgumentException(String.format("value expected between range[%d, %d], but found: %d", min, max, v));
        }
        return v;
    }

    public static int checkRelation(int v, RelationalOperator operator, int base) {
        if (!operator.test(v, base)) {
            throw new IllegalArgumentException(String.format("value expected %s base %d, but found: %d", operator, base, v));
        }
        return v;
    }

    public static long checkRelation(long v, RelationalOperator operator, long base) {
        if (!operator.test(v, base)) {
            throw new IllegalArgumentException(String.format("value expected %s base %d, but found: %d", operator, base, v));
        }
        return v;
    }

    //
    private static String nullToDef(String msg, String def) {
        return msg == null ? def : msg;
    }
}