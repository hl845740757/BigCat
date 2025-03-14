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

import cn.wjybxx.bigcat.TimeModule;
import com.google.inject.Inject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * @author wjybxx
 * date - 2025/3/14
 */
public class TestMainModule implements MainModule {

    private static final Logger logger = LoggerFactory.getLogger(TestMainModule.class);

    private Worker worker;
    @Inject
    private TimeModule timeModule;

    @Override
    public void inject(Worker worker) {
        this.worker = worker;
    }

    @Override
    public void start() {
        timeModule.start(System.currentTimeMillis());
    }

    @Override
    public boolean checkMainLoop(long eventLoopFrame) {
        return System.currentTimeMillis() - timeModule.getTime() >= 10;
    }

    @Override
    public void beforeMainLoop() {
        timeModule.update(System.currentTimeMillis());
    }

    @Override
    public void afterMainLoop() {

    }

    @Override
    public void onEvent(WorkerEvent rawEvent) throws Exception {
        logger.info("eventType: {}, index: {}", rawEvent.getType(), rawEvent.intVal1);
    }

    @Override
    public void beforeWorkerStart() {
        logger.info("beforeWorkerStart: " + worker.workerId());
    }

    @Override
    public void afterWorkerStart() {
        logger.info("afterWorkerStart: " + worker.workerId());
    }

    @Override
    public void beforeWorkerShutdown() {
        logger.info("beforeWorkerShutdown: " + worker.workerId());
    }

    @Override
    public void afterWorkerShutdown() {
        logger.info("afterWorkerShutdown: " + worker.workerId());
    }
}