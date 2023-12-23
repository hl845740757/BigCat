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

package cn.wjybxx.common.concurrent.ext;

import com.lmax.disruptor.util.Util;
import sun.misc.Unsafe;

import java.lang.invoke.VarHandle;

/**
 * LMAX的{@link com.lmax.disruptor.Sequence}的int值版本，方便做一些逻辑。
 *
 * <p>Concurrent sequence class used for tracking the progress of
 * the ring buffer and event processors.  Support a number
 * of concurrent operations including CAS and order writes.
 *
 * <p>Also attempts to be more efficient with regards to false
 * sharing by adding padding around the volatile field.
 */
public class IntSequence {

    @SuppressWarnings("unused")
    protected long p1, p2, p3, p4, p5, p6, p7, p8;

    /** 由于value是int值，因此需要左右两端都8个long */
    protected volatile int value;

    @SuppressWarnings("unused")
    protected long p9, p10, p11, p12, p13, p14, p15, p16;

    private static final int INITIAL_VALUE = -1;
    private static final Unsafe UNSAFE;
    private static final long VALUE_OFFSET;

    static {
        UNSAFE = Util.getUnsafe();
        try {
            VALUE_OFFSET = UNSAFE.objectFieldOffset(IntSequence.class.getDeclaredField("value"));
        } catch (final Exception e) {
            throw new RuntimeException(e);
        }
    }

    /**
     * Create a sequence initialised to -1.
     */
    public IntSequence() {
        this(INITIAL_VALUE);
    }

    /**
     * Create a sequence with a specified initial value.
     *
     * @param initialValue The initial value for this sequence.
     */
    public IntSequence(final int initialValue) {
        UNSAFE.putOrderedInt(this, VALUE_OFFSET, initialValue);
    }

    //

    /**
     * Perform a volatile read of this sequence's value.
     *
     * @return The current value of the sequence.
     */
    public int get() {
        return value;
    }

    /**
     * Perform an ordered write of this sequence.  The intent is
     * a Store/Store barrier between this write and any previous
     * store.
     *
     * @param value The new value for the sequence.
     */
    public void set(final int value) {
        UNSAFE.putOrderedInt(this, VALUE_OFFSET, value);
    }

    /**
     * Performs a volatile write of this sequence.  The intent is
     * a Store/Store barrier between this write and any previous
     * write and a Store/Load barrier between this write and any
     * subsequent volatile read.
     *
     * @param value The new value for the sequence.
     */
    public void setVolatile(final int value) {
        UNSAFE.putIntVolatile(this, VALUE_OFFSET, value);
    }
    //

    // region 原子操作

    /**
     * Perform a compare and set operation on the sequence.
     *
     * @param expectedValue The expected current value.
     * @param newValue      The value to update to.
     * @return true if the operation succeeds, false otherwise.
     */
    public boolean compareAndSet(final int expectedValue, final int newValue) {
        return UNSAFE.compareAndSwapInt(this, VALUE_OFFSET, expectedValue, newValue);
    }

    /**
     * Atomically increment the sequence by one.
     *
     * @return The value after the increment
     */
    public int incrementAndGet() {
        return UNSAFE.getAndAddInt(this, VALUE_OFFSET, 1) + 1;
    }

    /**
     * Atomically decrements the current value,
     * with memory effects as specified by {@link VarHandle#getAndAdd}.
     *
     * <p>Equivalent to {@code addAndGet(-1)}.
     *
     * @return the updated value
     */
    public int decrementAndGet() {
        return UNSAFE.getAndAddInt(this, VALUE_OFFSET, -1) - 1;
    }

    /**
     * Atomically increments the current value,
     * with memory effects as specified by {@link VarHandle#getAndAdd}.
     *
     * <p>Equivalent to {@code getAndAdd(1)}.
     *
     * @return the previous value
     */
    public final int getAndIncrement() {
        return UNSAFE.getAndAddInt(this, VALUE_OFFSET, 1);
    }

    /**
     * Atomically decrements the current value,
     * with memory effects as specified by {@link VarHandle#getAndAdd}.
     *
     * <p>Equivalent to {@code getAndAdd(-1)}.
     *
     * @return the previous value
     */
    public final int getAndDecrement() {
        return UNSAFE.getAndAddInt(this, VALUE_OFFSET, -1);
    }

    /**
     * Atomically add the supplied value.
     *
     * @param delta The value to add to the sequence.
     * @return The value after the increment.
     */
    public int addAndGet(final int delta) {
        return UNSAFE.getAndAddInt(this, VALUE_OFFSET, delta) + delta;
    }

    /**
     * Atomically adds the given value to the current value,
     * with memory effects as specified by {@link VarHandle#getAndAdd}.
     *
     * @param delta the value to add
     * @return the previous value
     */
    public int getAndAdd(final int delta) {
        return UNSAFE.getAndAddInt(this, VALUE_OFFSET, delta);
    }
    // endregion

    @Override
    public String toString() {
        return Integer.toString(get());
    }
}
