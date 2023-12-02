package cn.wjybxx.common.ex;

/**
 * 死循环预防
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public class InfiniteLoopException extends RuntimeException {

    public InfiniteLoopException() {
    }

    public InfiniteLoopException(String message) {
        super(message);
    }

    public InfiniteLoopException(String message, Throwable cause) {
        super(message, cause);
    }

    public InfiniteLoopException(Throwable cause) {
        super(cause);
    }

    public InfiniteLoopException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}