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

package cn.wjybxx.common.time;

import javax.annotation.concurrent.NotThreadSafe;
import javax.annotation.concurrent.ThreadSafe;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class TimeProviders {

    private TimeProviders() {

    }

    /**
     * 获取[毫秒]实时时间提供器
     *
     * @return timeProvider - threadSafe
     */
    public static TimeProvider systemTimeProvider() {
        return SystemTimeProvider.INSTANCE;
    }

    /***
     * 获取[纳秒]实时时间提供器
     *
     * @return timeProvider - threadSafe
     */
    public static TimeProvider systemNanoTimeProvider() {
        return SystemNanoTimeProvider.INSTANCE;
    }

    //

    /**
     * 创建一个支持缓存的时间提供器，但不是线程安全的。
     * 你需要调用{@link CachedTimeProvider#setTime(long)}更新时间值。
     *
     * @param curTime 初始时间
     * @return timeProvider -- {@link NotThreadSafe}
     */
    public static CachedTimeProvider newTimeProvider(long curTime) {
        return new UnsharableCachedTimeProvider(curTime);
    }

    /**
     * 创建一个基于deltaTime更新的时间提供器，用在一些特殊的场合。
     * 你需要调用{@link Timepiece#update(long)}更新时间值。
     *
     * @return timeProvider -- {@link NotThreadSafe}
     */
    public static Timepiece newTimepiece() {
        return new UnsharableTimepiece();
    }

    // region 多线程的

    @ThreadSafe
    private static class SystemTimeProvider implements TimeProvider {

        static final SystemTimeProvider INSTANCE = new SystemTimeProvider();

        private SystemTimeProvider() {

        }

        @Override
        public long getTime() {
            return System.currentTimeMillis();
        }

        @Override
        public String toString() {
            return "SystemTimeProvider{}";
        }
    }

    private static class SystemNanoTimeProvider implements TimeProvider {

        static final SystemNanoTimeProvider INSTANCE = new SystemNanoTimeProvider();

        @Override
        public long getTime() {
            return System.nanoTime();
        }

        @Override
        public String toString() {
            return "SystemNanoTimeProvider{}";
        }
    }

    // endregion

    // region 单线程的

    @NotThreadSafe
    private static class UnsharableCachedTimeProvider implements CachedTimeProvider {

        private long time;

        private UnsharableCachedTimeProvider(long time) {
            setTime(time);
        }

        public void setTime(long curTime) {
            this.time = curTime;
        }

        @Override
        public long getTime() {
            return time;
        }

        @Override
        public String toString() {
            return "UnsharableCachedTimeProvider{" +
                    "curTime=" + time +
                    '}';
        }
    }

    /** 其实可以指定最大 {@code deltaTime} 的，暂未支持 */
    @NotThreadSafe
    private static class UnsharableTimepiece implements Timepiece {

        private long time;
        private long deltaTime;

        @Override
        public long getTime() {
            return time;
        }

        @Override
        public long getDeltaTime() {
            return deltaTime;
        }

        @Override
        public void update(long deltaTime) {
            if (deltaTime <= 0) {
                this.deltaTime = 0;
            } else {
                this.deltaTime = deltaTime;
                this.time += deltaTime;
            }
        }

        @Override
        public void setTime(long curTime) {
            this.time = curTime;
        }

        @Override
        public void setDeltaTime(long deltaTime) {
            checkDeltaTime(deltaTime);
            this.deltaTime = deltaTime;
        }

        @Override
        public void restart(long curTime, long deltaTime) {
            checkDeltaTime(deltaTime);
            this.time = curTime;
            this.deltaTime = deltaTime;
        }

        private static void checkDeltaTime(long deltaTime) {
            if (deltaTime < 0) {
                throw new IllegalArgumentException("deltaTime must gte 0,  value " + deltaTime);
            }
        }

    }
    // endregion

}