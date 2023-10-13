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

import cn.wjybxx.common.rpc.RpcMethodKey;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;

/**
 * @author wjybxx
 * date - 2023/10/13
 */
public class DefaultPBMethodParserRegistry implements PBMethodParserRegistry {

    private final Int2ObjectMap<PBMethodParser<?, ?>> parserMap = new Int2ObjectOpenHashMap<>(100);

    @Override
    public void register(PBMethodParser<?, ?> parser) {
        int methodKey = RpcMethodKey.calMethodKey(parser.serviceId, parser.methodId);
        parserMap.put(methodKey, parser);
    }

    @Override
    public PBMethodParser<?, ?> getParser(int serviceId, int methodId) {
        int methodKey = RpcMethodKey.calMethodKey(serviceId, methodId);
        return parserMap.get(methodKey);
    }

}
