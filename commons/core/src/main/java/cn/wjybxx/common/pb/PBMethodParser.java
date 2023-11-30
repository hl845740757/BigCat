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

package cn.wjybxx.common.pb;

import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import java.util.Objects;

/**
 * 用于rpc方法解析参数和结果
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public class PBMethodParser<T extends MessageLite, U extends MessageLite> {

    public final int serviceId;
    public final int methodId;

    /** 方法参数类型 -- 可判断是否有参数 */
    public final Class<T> argType;
    public final Parser<T> argParser;

    /** 方法结果类型 -- 可判断是否有结果 */
    public final Class<U> resultType;
    public final Parser<U> resultParser;

    public PBMethodParser(int serviceId, int methodId, Class<T> argType, Class<U> resultType) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.argType = argType;
        this.resultType = resultType;

        this.argParser = findParser(argType);
        this.resultParser = findParser(resultType);
    }

    public PBMethodParser(int serviceId, int methodId,
                          Class<T> argType, Parser<T> argParser,
                          Class<U> resultType, Parser<U> resultParser) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.argType = argType;
        this.resultType = resultType;

        this.argParser = argType != null ? Objects.requireNonNull(argParser) : null;
        this.resultParser = resultType != null ? Objects.requireNonNull(resultParser) : null;
    }

    private static <T extends MessageLite> Parser<T> findParser(Class<T> argType) {
        return argType == null ? null : ProtobufUtils.findParser(argType);
    }

}