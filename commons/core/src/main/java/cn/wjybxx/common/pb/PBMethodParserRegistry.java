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
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

/**
 * rpc方法解析器注册表
 * <p>
 * {@link PBMethodParser}通常由主线程进行注册，IO线程查询使用，
 * 为保证线程可见性和安全性，主线程在注册完成之后需调用{@link #setImmutable()}将registry变更为不可变状态（注册完成），
 * IO线程在启动时可调用{@link #ensureImmutable()}检查registry的状态。
 * <p>
 * 另一种方案是通过线程的启动顺序来保证可见性，同时后续禁止修改。
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public final class PBMethodParserRegistry {

    private volatile boolean mutable = true;
    private final Int2ObjectMap<PBMethodParser<?, ?>> parserMap = new Int2ObjectOpenHashMap<>(100);

    @StableName
    public void register(PBMethodParser<?, ?> parser) {
        if (!mutable) {
            throw new IllegalStateException("registry is immutable");
        }
        int methodKey = RpcMethodKey.methodKey(parser.serviceId, parser.methodId);
        parserMap.put(methodKey, parser);
    }

    public PBMethodParser<?, ?> getParser(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.methodKey(serviceId, methodId);
        return parserMap.get(methodKey);
    }

    public boolean isMutable() {
        return mutable;
    }

    /** 主线程注册完毕后调用 */
    public void setImmutable() {
        mutable = false;
    }

    /** IO线程启动时调用 */
    public void ensureImmutable() {
        if (mutable) {
            throw new IllegalStateException("registry is mutable");
        }
    }
}