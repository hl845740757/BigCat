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
import cn.wjybxx.common.btree.Status;

/**
 * 循环节点抽象
 * 如果{@link #execute()}方法是有循环体的，那么一定要注意：
 * 只有循环的尾部运行child才是安全的，如果在运行child后还读写其它数据，可能导致bug(小心递归)。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class LoopDecorator<E> extends Decorator<E> {

    /** 每帧最大循环次数 - 避免死循环和占用较多CPU；默认1 */
    protected int maxLoopPerFrame = 1;

    public LoopDecorator() {
    }

    public LoopDecorator(int maxLoopPerFrame) {
        this.maxLoopPerFrame = Math.max(0, maxLoopPerFrame);
    }

    @Override
    protected void execute() {
        if (maxLoopPerFrame < 1) {
            setFailed(Status.ERROR);
            return;
        }
        if (maxLoopPerFrame == 1) {
            template_runChild(child);
            return;
        }
        int reentryId = getReentryId();
        for (int _i = maxLoopPerFrame - 1; _i >= 0; _i--) {
            template_runChild(child);
            if (checkCancel(reentryId)) {
                return;
            }
            if (child.isRunning()) { // 子节点未完成
                return;
            }
        }
    }

    public int getMaxLoopPerFrame() {
        return maxLoopPerFrame;
    }

    public void setMaxLoopPerFrame(int maxLoopPerFrame) {
        this.maxLoopPerFrame = maxLoopPerFrame;
    }
}