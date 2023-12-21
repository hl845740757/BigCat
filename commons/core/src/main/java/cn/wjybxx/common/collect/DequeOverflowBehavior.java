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

package cn.wjybxx.common.collect;

/**
 * 有界双端队列的溢出策略
 *
 * @author wjybxx
 * date - 2023/12/20
 */
public enum DequeOverflowBehavior {

    /** 抛出异常 */
    THROW_EXCEPTION(0),

    /**
     * 丢弃首部 -- 当尾部插入元素时，允许覆盖首部；首部插入时抛出异常。
     * eg：undo队列
     */
    DISCARD_HEAD(1),

    /**
     * 丢弃尾部 -- 当首部插入元素时，允许覆盖尾部；尾部插入时抛出异常。
     * eg: redo队列
     */
    DISCARD_TAIL(2),

    /**
     * 环形缓冲 -- 首部插入时覆盖尾部；尾部插入时覆盖首部。
     */
    CIRCLE_BUFFER(3);

    private final int number;

    DequeOverflowBehavior(int number) {
        this.number = number;
    }

    public int getNumber() {
        return number;
    }

    public boolean allowDiscardHead() {
        return this == CIRCLE_BUFFER || this == DISCARD_HEAD;
    }

    public boolean allowDiscardTail() {
        return this == CIRCLE_BUFFER || this == DISCARD_TAIL;
    }

    /** 序列化 */
    public static DequeOverflowBehavior forNumber(int number) {
        return switch (number) {
            case 0 -> THROW_EXCEPTION;
            case 1 -> DISCARD_HEAD;
            case 2 -> DISCARD_TAIL;
            case 3 -> CIRCLE_BUFFER;
            default -> throw new IllegalArgumentException("number: " + number);
        };
    }

}
