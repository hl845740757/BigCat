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
 * date 2023/3/31
 */
public class ObjectHolder<T> {

    private T value;

    public ObjectHolder() {
        this(null);
    }

    public ObjectHolder(T value) {
        this.value = value;
    }

    public T get() {
        return value;
    }

    public void set(T value) {
        this.value = value;
    }

    public T getAndSet(T value) {
        T result = this.value;
        this.value = value;
        return result;
    }

    @Override
    public String toString() {
        return "ObjectHolder{" +
                "value=" + value +
                '}';
    }

}