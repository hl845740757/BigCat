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

import cn.wjybxx.common.time.TimeUtils;

import java.util.Set;
import java.util.concurrent.locks.LockSupport;
import java.util.function.Predicate;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class ThreadUtils {

    /**
     * J9中新的的{@link StackWalker}，拥有更好的性能，因为它可以只创建需要的栈帧，而不是像异常一样总是获得所有栈帧的信息。
     */
    private static final StackWalker STACK_WALKER = StackWalker.getInstance(Set.of(
            StackWalker.Option.SHOW_HIDDEN_FRAMES,
            StackWalker.Option.SHOW_REFLECT_FRAMES,
            StackWalker.Option.RETAIN_CLASS_REFERENCE));

    /**
     * 恢复中断
     */
    public static void recoveryInterrupted() {
        try {
            Thread.currentThread().interrupt();
        } catch (SecurityException ignore) {
        }
    }

    /**
     * 如果是中断异常，则恢复线程中断状态。
     */
    public static void recoveryInterrupted(Throwable t) {
        if (t instanceof InterruptedException) {
            try {
                Thread.currentThread().interrupt();
            } catch (SecurityException ignore) {
            }
        }
    }

    /**
     * 检查线程中断状态。
     *
     * @throws InterruptedException 如果线程被中断，则抛出中断异常
     */
    public static void checkInterrupted() throws InterruptedException {
        if (Thread.interrupted()) {
            throw new InterruptedException();
        }
    }

    /**
     * 安静地睡眠一会儿
     *
     * @param sleepMillis 要睡眠的时间(毫秒)
     */
    public static void sleepQuietly(long sleepMillis) {
        LockSupport.parkNanos(sleepMillis * TimeUtils.NANOS_PER_MILLI);
    }

    public static void joinUninterruptedly(Thread thread) {
        boolean interrupted = false;
        while (true) {
            try {
                thread.join();
                break;
            } catch (InterruptedException ignore) {
                interrupted = true;
            }
        }
        if (interrupted) {
            recoveryInterrupted();
        }
    }

    /**
     * 获取调用者信息。
     * ps：该方法的性能更好，但可读性差，且不易维护。
     *
     * @param deep 当前方法的深度
     * @return 调用者信息
     */
    public static String getCallerInfo(int deep) {
        // +1 对应当前方法的栈帧
        // Q: 为什么使用limit而不是skip?
        // A: 如果方法被内联，则skip跳过的栈帧可能不正确，因此使用limit，并选择最后一个栈帧
        return STACK_WALKER.walk(stackFrameStream -> stackFrameStream.limit(deep + 1)
                        .reduce((stackFrame, stackFrame2) -> stackFrame2)
                        .map(Object::toString))
                .orElse("Unknown source");
    }

    /**
     * 获取调用者信息。
     * ps: 该方法使用不当可能性能较差，但有更好的可读性和更好的可扩展性。
     * <p>
     * 注意：
     * 1. {@link StackWalker.StackFrame#getFileName()}可能为null，如lambda表达式。
     * 2. {@link StackWalker.StackFrame#getLineNumber()}可能为负数，如lambda表达式，jni方法。
     * 3. {@link StackWalker.StackFrame#getMethodName()} name是延迟初始化的，存在额外开销，可以考虑使用{@link StackWalker.StackFrame#getDeclaringClass()}代替。
     * 4. 可能需要考虑方法被内联产生的堆栈变化。
     *
     * @param filter 过滤器
     * @return 调用者信息
     */
    public static String getCallerInfo(Predicate<StackWalker.StackFrame> filter) {
        // 暂时使用getDeclaringClass过滤自己，这样可以降低开销，但对该类中的其它方法可能产生影响，如果以后有影响则需要改为名字判断
        return STACK_WALKER.walk(stackFrameStream -> stackFrameStream
                        .filter(stackFrame -> stackFrame.getDeclaringClass() != ThreadUtils.class)
                        .filter(filter)
                        .findFirst()
                        .map(Object::toString))
                .orElse("Unknown source");
    }

}