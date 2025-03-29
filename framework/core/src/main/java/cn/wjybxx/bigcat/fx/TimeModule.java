/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.base.annotation.VisibleForTesting;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.concurrent.IEventLoopAgent;

import javax.annotation.concurrent.NotThreadSafe;

/**
 * 时间模块
 * 1.系统的启动帧我们定为第0帧。
 * 2.通常应该由{@link IEventLoopAgent}来启动和更新时间模块。
 * 3.线程一个，多线程共享是不必要也不安全的。
 *
 * @author wjybxx
 * date - 2023/10/4
 */
@NotThreadSafe
public class TimeModule implements TimeProvider {

    private int frame;
    private long time;
    private long deltaTime;

    public TimeModule() {
        time = System.currentTimeMillis();
    }

    public void start(long timeMillis) {
        this.frame = 0;
        this.deltaTime = 0;
        this.time = timeMillis;
    }

    /** 通常由主模块在主循环中更新 */
    public void update(long curTime) {
        frame += 1;
        this.deltaTime = Math.max(0, curTime - this.time);
        this.time = curTime;
    }

    public int getFrame() {
        return frame;
    }

    @Override
    public long getTime() {
        return time;
    }

    public long getDeltaTime() {
        return deltaTime;
    }

    // setter
    @VisibleForTesting
    public void setFrame(int frame) {
        this.frame = frame;
    }

    @VisibleForTesting
    public void setDeltaTime(long deltaTime) {
        this.deltaTime = deltaTime;
    }

    @VisibleForTesting
    public void setTime(long time) {
        this.time = time;
    }

}