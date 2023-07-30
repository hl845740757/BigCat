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

package cn.wjybxx.common.box;

/**
 * 主要在lambda表达式中使用
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class BooleanHolder {

    private boolean value;

    public BooleanHolder() {
        this.value = false;
    }

    public BooleanHolder(boolean value) {
        this.value = value;
    }

    public boolean get() {
        return value;
    }

    public void set(boolean value) {
        this.value = value;
    }

    public boolean getAndSet(boolean value) {
        boolean result = this.value;
        this.value = value;
        return result;
    }

    public boolean isTrue() {
        return value;
    }

    public boolean isFalse() {
        return !value;
    }

    @Override
    public String toString() {
        return "BooleanHolder{" +
                "value=" + value +
                '}';
    }

}