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
 * @author wjybxx
 * date 2023/4/7
 */
public class DoubleHolder {

    private double value;

    public DoubleHolder() {
        this(0);
    }

    public DoubleHolder(double value) {
        this.value = value;
    }

    public double get() {
        return value;
    }

    public void set(double value) {
        this.value = value;
    }

    /**
     * 加上指定增量并返回
     *
     * @param delta the value to add
     * @return the updated value
     */
    public double addAndGet(double delta) {
        this.value += delta;
        return this.value;
    }

    /**
     * 返回之后加上指定增量
     *
     * @param delta the value to add
     * @return he previous value
     */
    public double getAndAdd(double delta) {
        double result = this.value;
        this.value += delta;
        return result;
    }

    public double getAndSet(double value) {
        double r = this.value;
        this.value = value;
        return r;
    }

    @Override
    public String toString() {
        return "DoubleHolder{" +
                "value=" + value +
                '}';
    }

}