/*
 *  Copyright 2023-2024 wjybxx
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to iBn writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.concurrent.RingBufferEvent;
import com.google.inject.Inject;

/**
 * @author wjybxx
 * date - 2023/12/23
 */
public class DefaultMainModule implements MainModule {

    @Inject
    protected TimeModule timeModule;
    /** 帧循环间隔 */
    private int frameInterval = 30;

    /** 主循环前时间戳 - 用于计算帧耗时等 */
    protected long timeBeforeMainLoop;
    /** 主循环后时间戳 */
    protected long timeAfterMainLoop;
    /** 上一次主循环耗时 */
    protected long mainLoopTimeSpan;
    //

    public int getFrameInterval() {
        return frameInterval;
    }

    public void setFrameInterval(int frameInterval) {
        if (frameInterval <= 0) throw new IllegalArgumentException("frameInterval: " + frameInterval);
        this.frameInterval = frameInterval;
    }

    /** 获取前一次主循环耗时 -- 或当前主循环结束后查看本次耗时 */
    public long getMainLoopTimeSpan() {
        return mainLoopTimeSpan;
    }

    /** 实时的主循环耗时 */
    public long mainLoopElapsed() {
        return System.currentTimeMillis() - timeBeforeMainLoop;
    }

    //

    @Override
    public void start() {
        timeModule.start(System.currentTimeMillis());
        timeBeforeMainLoop = timeAfterMainLoop = timeModule.getTime();
    }

    @Override
    public boolean checkMainLoop() {
        return System.currentTimeMillis() - timeModule.getTime() >= frameInterval;
    }

    @Override
    public void beforeMainLoop() {
        long timeMillis = System.currentTimeMillis();
        timeModule.update(timeMillis);
        timeBeforeMainLoop = timeMillis;
    }

    @Override
    public void afterMainLoop() {
        timeAfterMainLoop = System.currentTimeMillis();
        mainLoopTimeSpan = timeAfterMainLoop - timeBeforeMainLoop;
    }

    @Override
    public void onEvent(RingBufferEvent rawEvent) throws Exception {

    }

    @Override
    public void beforeWorkerStart() {

    }

    @Override
    public void afterWorkerStart() {

    }

    @Override
    public void beforeWorkerShutdown() {

    }

    @Override
    public void afterWorkerShutdown() {

    }
}