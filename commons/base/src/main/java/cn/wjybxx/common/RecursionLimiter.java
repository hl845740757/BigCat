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