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

import cn.wjybxx.common.annotation.StableName;
import cn.wjybxx.common.rpc.RpcMethodKey;
import cn.wjybxx.common.rpc.RpcRequest;
import cn.wjybxx.common.rpc.RpcResponse;
import com.google.protobuf.InvalidProtocolBufferException;
import com.google.protobuf.Message;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;
import org.apache.commons.lang3.ArrayUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.util.List;

/**
 * rpc方法信息注册表
 * <p>
 * {@link PBMethodInfo}通常由主线程进行注册，IO线程查询使用，
 * 为保证线程可见性和安全性，主线程在注册完成之后需调用{@link #makeImmutable()}将registry变更为不可变状态（注册完成），
 * IO线程在启动时可调用{@link #ensureImmutable()}检查registry的状态。
 * <p>
 * 另一种方案是通过线程的启动顺序来保证可见性，同时后续禁止修改。
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public final class PBMethodInfoRegistry {

    private static final Logger logger = LoggerFactory.getLogger(PBMethodInfoRegistry.class);

    private volatile boolean mutable = true;
    private final Int2ObjectMap<PBMethodInfo<?, ?>> methodInfoMap = new Int2ObjectOpenHashMap<>(100);

    @StableName
    public void register(PBMethodInfo<?, ?> parser) {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
        int methodKey = RpcMethodKey.methodKey(parser.serviceId, parser.methodId);
        methodInfoMap.put(methodKey, parser);
    }

    /** @return 如果方法不存在，则返回null */
    public PBMethodInfo<?, ?> getMethodInfo(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return methodInfoMap.get(methodKey);
    }

    public boolean isMutable() {
        return mutable;
    }

    /** 主线程注册完毕后调用 */
    public void makeImmutable() {
        mutable = false;
    }

    /** IO线程启动时调用 */
    public void ensureImmutable() {
        if (mutable) {
            throw new IllegalStateException("registry is mutable");
        }
    }

    // region 编解码
    // 这里的序列化只是示例，在真实的项目中，应当直接写入对应的输出流

    public void encodeParameters(RpcRequest request) {
        Message message = (Message) request.getArgument();
        if (message == null) {
            request.setParameters(ArrayUtils.EMPTY_BYTE_ARRAY);
        } else {
            request.setParameters(message.toByteArray());
        }
    }

    public boolean decodeParameters(RpcRequest request) {
        int methodKey = RpcMethodKey.methodKey(request.getServiceId(), request.getMethodId());
        PBMethodInfo<?, ?> methodInfo = methodInfoMap.get(methodKey);
        if (methodInfo == null) {
            return false;
        }
        if (methodInfo.argType == null) { // 无参数
            request.setParameters(List.of());
            return true;
        }
        try {
            // 空字节数组将被解析为空消息
            Object message = methodInfo.argParser.parseFrom(request.bytesParameters());
            request.setParameters(List.of(message));
            return true;
        } catch (InvalidProtocolBufferException e) {
            logger.info("decode parameter caught exception, serviceId: {}, methodId {}",
                    request.getServiceId(), request.getMethodId(), e);
            return false;
        }
    }

    public void encodeResult(RpcResponse response) {
        if (!response.isSuccess()) { // 失败
            byte[] msgBytes = response.getErrorMsg().getBytes(StandardCharsets.UTF_8);
            response.setResults(msgBytes);
            return;
        }
        // null替换为空字节数组(空消息)
        Message message = (Message) response.getResult();
        if (message == null) {
            response.setResults(ArrayUtils.EMPTY_BYTE_ARRAY);
        } else {
            response.setResults(message.toByteArray());
        }
    }

    public boolean decodeResult(RpcResponse response) {
        if (!response.isSuccess()) {
            String errorMsg = new String(response.bytesResults(), StandardCharsets.UTF_8);
            response.setResults(List.of(errorMsg));
            return true;
        }

        int methodKey = RpcMethodKey.methodKey(response.getServiceId(), response.getMethodId());
        PBMethodInfo<?, ?> methodInfo = methodInfoMap.get(methodKey);
        if (methodInfo == null) {
            return false;
        }
        if (methodInfo.resultType == null) { // Void
            response.setResults(List.of()); // Future无法区分Void和Null
            return true;
        }
        try {
            // 空字节数组将被解析为空消息
            Object message = methodInfo.resultParser.parseFrom(response.bytesResults());
            response.setResults(List.of(message));
            return true;
        } catch (InvalidProtocolBufferException e) {
            logger.info("decode result caught exception, serviceId: {}, methodId {}",
                    response.getServiceId(), response.getMethodId(), e);
            return false;
        }
    }

    // endregion
}