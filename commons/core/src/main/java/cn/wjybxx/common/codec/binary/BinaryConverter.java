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

package cn.wjybxx.common.codec.binary;

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.Converter;
import cn.wjybxx.common.codec.TypeMetaRegistry;

import javax.annotation.concurrent.ThreadSafe;

/**
 * 二进制转换器
 * 二进制是指将对象序列化为字节数组，以编解码效率和压缩比例为重。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface BinaryConverter extends Converter {

    BinaryCodecRegistry codecRegistry();

    TypeMetaRegistry typeMetaRegistry();

    ConvertOptions options();
}