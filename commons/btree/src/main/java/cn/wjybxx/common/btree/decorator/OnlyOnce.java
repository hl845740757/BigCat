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

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 只执行一次。
 * 1.适用那些不论成功与否只执行一次的行为。
 * 2.在调用{@link #resetForRestart()}后可再次运行。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@BinarySerializable
@DocumentSerializable
public class OnlyOnce<E> extends Decorator<E> {

    public OnlyOnce() {
    }

    public OnlyOnce(Task<E> child) {
        super(child);
    }

    @Override
    protected void execute() {
        if (child.isCompleted()) {
            setCompleted(child.getStatus(), true);
        } else {
            template_runChild(child);
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        setCompleted(child.getStatus(), true);
    }

}