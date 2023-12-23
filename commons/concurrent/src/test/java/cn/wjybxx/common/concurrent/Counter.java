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

package cn.wjybxx.common.concurrent;

import it.unimi.dsi.fastutil.ints.Int2LongArrayMap;
import it.unimi.dsi.fastutil.ints.Int2LongMap;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.List;

/**
 * 需要创建实例，避免使用静态数据，否则批量执行测试用例的时候由于共享数据导致失败
 *
 * @author wjybxx
 * date 2023/4/13
 */
@NotThreadSafe
public class Counter {

    private final Int2LongMap sequenceMap = new Int2LongArrayMap();
    private final List<String> errorMsgList = new ArrayList<>();

    public Int2LongMap getSequenceMap() {
        return sequenceMap;
    }

    public List<String> getErrorMsgList() {
        return errorMsgList;
    }

    public void count(int type, long sequence) {
        if (type < 1) {
            errorMsgList.add(String.format("code1, event.type: %d (expected: > 0)",
                    type));
            return;
        }

        long nextSequence = sequenceMap.get(type);
        if (sequence != nextSequence) {
            if (errorMsgList.size() < 100) { // 避免toString爆炸
                errorMsgList.add(String.format("code2, event.type: %d, nextSequence: %d (expected: = %d)",
                        type, sequence, nextSequence));
            }
        }
        sequenceMap.put(type, nextSequence + 1);
    }

    public Runnable newTask(int type, long sequence) {
        if (type <= 0) throw new IllegalArgumentException("invalid type " + type);
        return new CounterTask(type, sequence);
    }

    /** 可能被添加为fixedRate之类 */
    private class CounterTask implements Runnable {

        final int type;
        final long sequence;
        private boolean first = true;

        private CounterTask(int type, long sequence) {
            this.type = type;
            this.sequence = sequence;
        }

        @Override
        public void run() {
            if (first) {
                first = false;
                count(type, sequence);
            }
        }

    }
}