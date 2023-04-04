/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common.box;

/**
 * 主要在lambda表达式中使用
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LongHolder {

    private long value;

    public LongHolder() {
        this(0L);
    }

    public LongHolder(long value) {
        this.value = value;
    }

    /**
     * 获取当前值
     */
    public long get() {
        return value;
    }

    /**
     * 设置为指定值
     */
    public void set(long value) {
        this.value = value;
    }

    /** 返回之后+1 */
    public long getAndInc() {
        return value++;
    }

    /** +1之后返回 */
    public long incAndGet() {
        return ++value;
    }

    /** 返回之后-1 */
    public long getAndDec() {
        return value--;
    }

    /** -1之后返回 */
    public long decAndGet() {
        return --value;
    }

    /**
     * 加上指定增量并返回
     *
     * @param delta the value to add
     * @return the updated value
     */
    public long addAndGet(long delta) {
        this.value += delta;
        return this.value;
    }

    /**
     * 返回之后加上指定增量
     *
     * @param delta the value to add
     * @return he previous value
     */
    public long getAndAdd(long delta) {
        long result = this.value;
        this.value += delta;
        return result;
    }

    /**
     * 修改当前值，并返回之前的值
     *
     * @return old value
     */
    public long getAndSet(long value) {
        long result = this.value;
        this.value = value;
        return result;
    }

    @Override
    public String toString() {
        return "LongHolder{" +
                "value=" + value +
                '}';
    }

}