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
package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 重复N次
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@BinarySerializable
@DocumentSerializable
public class Repeat<E> extends LoopDecorator<E> {

    public static final int MODE_ALWAYS = 0;
    public static final int MODE_ONLY_SUCCESS = 1;
    public static final int MODE_ONLY_FAILED = 2;
    public static final int MODE_NEVER = 3;

    /** 考虑到Java枚举与其它语言的兼容性问题，我们在编辑器中使用数字 */
    private int countMode = MODE_ALWAYS;
    private int required = 1;
    private transient int count;

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        count = 0;
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        count = 0;
    }

    @Override
    protected void enter(int reentryId) {
        super.enter(reentryId);
        if (required < 1) {
            setSuccess();
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        boolean match;
        switch (countMode) {
            case MODE_ALWAYS -> match = true;
            case MODE_ONLY_SUCCESS -> match = child.isSucceeded();
            case MODE_ONLY_FAILED -> match = child.isFailed();
            default -> match = false;
        }
        if (match && ++count >= required) {
            setSuccess();
        }
    }

    public int getCountMode() {
        return countMode;
    }

    public void setCountMode(int countMode) {
        this.countMode = countMode;
    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }
}