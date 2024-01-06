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

package cn.wjybxx.bigcat.pb;

import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import javax.annotation.Nullable;

/**
 * 用于rpc方法解析参数和结果
 *
 * @param <T> 方法参数类型，{@link Void}表示无
 * @param <R> 方法结果类型，{@link Void}表示无
 * @author wjybxx
 * date - 2023/10/12
 */
public class PBMethodInfo<T, R> {

    public final int serviceId;
    public final int methodId;

    /** 方法参数类型 */
    public final Class<T> argType;
    public final Parser<T> argParser;

    /** 方法结果类型 */
    public final Class<R> resultType;
    public final Parser<R> resultParser;

    public PBMethodInfo(int serviceId, int methodId, Class<T> argType, Class<R> resultType) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.argType = argType;
        this.resultType = resultType;

        this.argParser = findParser(argType);
        this.resultParser = findParser(resultType);
    }

    // 该构造器生成的代码会调用
    public PBMethodInfo(int serviceId, int methodId,
                        Class<T> argType, @Nullable Parser<T> argParser,
                        Class<R> resultType, @Nullable Parser<R> resultParser) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.argType = argType;
        this.resultType = resultType;

        this.argParser = argParser;
        this.resultParser = resultParser;
    }

    /** 方法是否有参数 */
    public boolean hasArg() {
        return argParser != null;
    }

    /** 方法是否有结果 */
    public boolean hasResult() {
        return resultParser != null;
    }

    @SuppressWarnings("unchecked")
    private static <M> Parser<M> findParser(Class<M> argType) {
        if (argType == null || argType == Void.class || argType == void.class) {
            return null;
        }
        Class<? extends MessageLite> clazz = (Class<? extends MessageLite>) argType;
        return (Parser<M>) ProtobufUtils.findParser(clazz);
    }

}