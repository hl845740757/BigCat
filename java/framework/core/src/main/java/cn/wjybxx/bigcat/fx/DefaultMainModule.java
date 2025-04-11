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

import cn.wjybxx.concurrent.IAgentEventHandler;
import cn.wjybxx.concurrent.IEventLoop;
import cn.wjybxx.concurrent.IEventLoopAgent;
import com.google.inject.Inject;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/12/23
 */
public class DefaultMainModule implements IEventLoopAgent<WorkerEvent> {

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
    /** 事件循环 */
    protected Worker worker;
    /** 事件循环的事件处理器 */
    protected final Int2ObjectMap<IAgentEventHandler<? super WorkerEvent>> handlerMap = new Int2ObjectOpenHashMap<>(20);

    // region 事件
    @Override
    public void inject(IEventLoop eventLoop, long consumerId) {
        this.worker = (Worker) eventLoop;
    }

    @Override
    public void subscribe(int type, IAgentEventHandler<? super WorkerEvent> handler) {
        Objects.requireNonNull(handler, "handler");
        if (handlerMap.containsKey(type)) {
            throw new IllegalArgumentException("type: " + type);
        }
        handlerMap.put(type, handler);
    }

    @Override
    public void onEvent(long sequence, WorkerEvent event) throws Exception {
        IAgentEventHandler<? super WorkerEvent> handler = handlerMap.get(event.getType());
        if (handler != null) {
            handler.onEvent(sequence, event);
        }
    }
    // endregion

    // region 主循环

    /** 获取帧间隔参数 */
    public int getFrameInterval() {
        return frameInterval;
    }

    /** 设置帧间隔参数 */
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

    @Override
    public void beforeEventLoopStart() {
        timeModule.start(System.currentTimeMillis());
        timeBeforeMainLoop = timeAfterMainLoop = timeModule.getTime();
    }

    @Override
    public boolean checkMainLoop(long threadTime) {
        return System.currentTimeMillis() - timeModule.getTime() >= frameInterval;
    }

    @Override
    public void beforeMainLoop(long threadTime) {
        long timeMillis = System.currentTimeMillis();
        timeModule.update(timeMillis);
        timeBeforeMainLoop = timeMillis;
    }

    @Override
    public void afterMainLoop(long threadTime) {
        timeAfterMainLoop = System.currentTimeMillis();
        mainLoopTimeSpan = timeAfterMainLoop - timeBeforeMainLoop;
    }

    // endregion

}