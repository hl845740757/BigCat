/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.concurrent.IAgentEvent;

/**
 * Worker主循环事件
 * (是池化的，不必担心开销)
 *
 * @author wjybxx
 * date - 2025/3/14
 */
public final class WorkerEvent implements IAgentEvent {

    private int type = TYPE_INVALID;
    private int options;
    public Object obj1;
    public Object obj2;
    public Object obj3;

    // 扩展字段
    public int intVal1;
    public int intVal2;
    public long longVal1;
    public long longVal2;

    @Override
    public void clean() {
        type = TYPE_INVALID;
        options = 0;
        obj1 = null;
        obj2 = null;
        obj3 = null;
    }

    @Override
    public void cleanAll() {
        type = TYPE_INVALID;
        options = 0;
        obj1 = null;
        obj2 = null;
        obj3 = null;

        intVal1 = 0;
        intVal2 = 0;
        longVal1 = 0;
        longVal2 = 0;
    }

    @Override
    public int getType() {
        return type;
    }

    @Override
    public void setType(int type) {
        this.type = type;
    }

    @Override
    public int getOptions() {
        return options;
    }

    @Override
    public void setOptions(int options) {
        this.options = options;
    }

    @Override
    public Object getObj1() {
        return obj1;
    }

    @Override
    public void setObj1(Object obj1) {
        this.obj1 = obj1;
    }

    @Override
    public Object getObj2() {
        return obj2;
    }

    @Override
    public void setObj2(Object obj2) {
        this.obj2 = obj2;
    }

    public Object getObj3() {
        return obj3;
    }

    public void setObj3(Object obj3) {
        this.obj3 = obj3;
    }

    public int getIntVal1() {
        return intVal1;
    }

    public void setIntVal1(int intVal1) {
        this.intVal1 = intVal1;
    }

    public int getIntVal2() {
        return intVal2;
    }

    public void setIntVal2(int intVal2) {
        this.intVal2 = intVal2;
    }

    @Override
    public long getLongVal1() {
        return longVal1;
    }

    @Override
    public void setLongVal1(long longVal1) {
        this.longVal1 = longVal1;
    }

    @Override
    public long getLongVal2() {
        return longVal2;
    }

    @Override
    public void setLongVal2(long longVal2) {
        this.longVal2 = longVal2;
    }
}