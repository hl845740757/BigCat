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
package cn.wjybxx.common.rpc;


import cn.wjybxx.common.log.DebugLogLevel;

import javax.annotation.concurrent.Immutable;

/**
 * rpc日志配置（用于底层打印日志信息）
 * 注意：这里控制的是一些非必要日志(冗余日志)，方便debug等，关键日志底层一定会打印。
 * (实现为不可变，主要为方便共享)
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Immutable
public class RpcLogConfig {

    public static final RpcLogConfig NONE = RpcLogConfig.newBuilder().build();

    public static final RpcLogConfig ALL_DETAIL = RpcLogConfig.newBuilder()
            .setSndRequestLogLevel(DebugLogLevel.DETAIL)
            .setSndResponseLogLevel(DebugLogLevel.DETAIL)
            .setRcvRequestLogLevel(DebugLogLevel.DETAIL)
            .setRcvResponseLogLevel(DebugLogLevel.DETAIL)
            .build();

    public static final RpcLogConfig ALL_SIMPLE = RpcLogConfig.newBuilder()
            .setSndRequestLogLevel(DebugLogLevel.SIMPLE)
            .setSndResponseLogLevel(DebugLogLevel.SIMPLE)
            .setRcvRequestLogLevel(DebugLogLevel.SIMPLE)
            .setRcvResponseLogLevel(DebugLogLevel.SIMPLE)
            .build();

    private final int sndRequestLogLevel;
    private final int sndResponseLogLevel;
    private final int rcvRequestLogLevel;
    private final int rcvResponseLogLevel;

    private RpcLogConfig(Builder builder) {
        this.sndRequestLogLevel = builder.sndRequestLogLevel;
        this.sndResponseLogLevel = builder.sndResponseLogLevel;
        this.rcvRequestLogLevel = builder.rcvRequestLogLevel;
        this.rcvResponseLogLevel = builder.rcvResponseLogLevel;
    }

    public int getSndRequestLogLevel() {
        return sndRequestLogLevel;
    }

    public int getSndResponseLogLevel() {
        return sndResponseLogLevel;
    }

    public int getRcvRequestLogLevel() {
        return rcvRequestLogLevel;
    }

    public int getRcvResponseLogLevel() {
        return rcvResponseLogLevel;
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        /** 发送的请求日志级别 */
        private int sndRequestLogLevel = DebugLogLevel.NONE;
        /** 发送的响应日志级别 */
        private int sndResponseLogLevel = DebugLogLevel.NONE;
        /** 接收到的请求的日志级别 */
        private int rcvRequestLogLevel = DebugLogLevel.NONE;
        /** 接收到的响应的日志级别 */
        private int rcvResponseLogLevel = DebugLogLevel.NONE;

        public int getSndRequestLogLevel() {
            return sndRequestLogLevel;
        }

        public Builder setSndRequestLogLevel(int sndRequestLogLevel) {
            this.sndRequestLogLevel = DebugLogLevel.checkedLevel(sndRequestLogLevel);
            return this;
        }

        public int getSndResponseLogLevel() {
            return sndResponseLogLevel;
        }

        public Builder setSndResponseLogLevel(int sndResponseLogLevel) {
            this.sndResponseLogLevel = DebugLogLevel.checkedLevel(sndResponseLogLevel);
            return this;
        }

        public int getRcvRequestLogLevel() {
            return rcvRequestLogLevel;
        }

        public Builder setRcvRequestLogLevel(int rcvRequestLogLevel) {
            this.rcvRequestLogLevel = DebugLogLevel.checkedLevel(rcvRequestLogLevel);
            return this;
        }

        public int getRcvResponseLogLevel() {
            return rcvResponseLogLevel;
        }

        public Builder setRcvResponseLogLevel(int rcvResponseLogLevel) {
            this.rcvResponseLogLevel = DebugLogLevel.checkedLevel(rcvResponseLogLevel);
            return this;
        }

        public RpcLogConfig build() {
            return new RpcLogConfig(this);
        }
    }
}