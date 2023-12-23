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

import cn.wjybxx.common.EnumLite;
import cn.wjybxx.common.EnumLiteMap;
import cn.wjybxx.common.EnumUtils;
import cn.wjybxx.common.annotation.StableName;
import cn.wjybxx.common.codec.binary.BinarySerializable;

import javax.annotation.Nullable;

/**
 * 一些特殊的地址描述
 *
 * @author wjybxx
 * date - 2023/10/4
 */
@BinarySerializable
public enum StaticRpcAddr implements RpcAddr, EnumLite {

    /** Node内本地单播地址 */
    LOCAL(1),

    /** Node内本地广播地址 */
    LOCAL_BROADCAST(2);

    private final int number;

    StaticRpcAddr(int number) {
        this.number = number;
    }

    @Override
    public int getNumber() {
        return number;
    }

    private static final EnumLiteMap<StaticRpcAddr> VALUE_MAP = EnumUtils.mapping(values());

    @Nullable
    @StableName
    public static StaticRpcAddr forNumber(int number) {
        return VALUE_MAP.forNumber(number);
    }

    @StableName
    public static StaticRpcAddr checkedForNumber(int number) {
        return VALUE_MAP.checkedForNumber(number);
    }

    @StableName
    public static StaticRpcAddr forNumber(int number, StaticRpcAddr def) {
        return VALUE_MAP.forNumber(number, def);
    }
}