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

import cn.wjybxx.common.ex.NoLogRequiredException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * @author wjybxx
 * date - 2023/10/26
 */
public class FutureExceptionLoggerLoader {

    private static volatile FutureExceptionLogger exceptionLogger;

    /** Future会在初始化的时候读取一次 */
    public static FutureExceptionLogger getExceptionLogger() {
        return exceptionLogger;
    }

    public static void setExceptionLogger(FutureExceptionLogger exceptionLogger) {
        FutureExceptionLoggerLoader.exceptionLogger = exceptionLogger;
    }

    //
    public static final FutureExceptionLogger DEFAULT_LOGGER;

    static {
        final boolean enableLogError = Boolean.parseBoolean(System.getProperty("cn.wjybxx.common.concurrent.XCompletableFuture.logError", "true"));
        if (enableLogError) {
            DEFAULT_LOGGER = new DefaultExceptionLogger();
        } else {
            DEFAULT_LOGGER = new EmptyExceptionLogger();
        }
    }

    private static final class DefaultExceptionLogger implements FutureExceptionLogger {

        static final Logger logger = LoggerFactory.getLogger("cn.wjybxx.common.concurrent.XCompletableFuture"); // 默认和Future一个Logger

        @Override
        public boolean isEnable() {
            return true;
        }

        @Override
        public void onCaughtException(Throwable ex, String extraInfo) {
            if (!(ex instanceof NoLogRequiredException)) {
                logger.info("future completed with exception", ex);
            }
        }
    }

    private static final class EmptyExceptionLogger implements FutureExceptionLogger {

        private EmptyExceptionLogger() {
        }

        @Override
        public boolean isEnable() {
            return false;
        }

        @Override
        public void onCaughtException(Throwable ex, String extraInfo) {

        }
    }

}