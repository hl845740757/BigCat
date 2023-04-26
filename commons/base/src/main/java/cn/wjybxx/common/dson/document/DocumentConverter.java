/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.Converter;

import javax.annotation.Nonnull;

/**
 * 文档转换器
 * 将对象转换为文档或类文档结构，比如：Json/Bson/Yaml/Lua，主要用于持久化存储
 * <p>
 * 1.文档是指人类可读的文本结构，以可读性为主，与文档型数据库并不直接关联。
 * 2.在设计上，文档编解码器对效率和压缩比例的追求较低，API会考虑易用性，因此{@link DocumentObjectReader}提供随机读的API。
 * 3.在设计上，是为了持久化的，为避免破坏用户数据，因此对于数组类型的数据结构不存储其类型信息，因此缺少静态类型信息的地方将无法准确解码。
 * 4.我们约定，持久化一个枚举对象时，写为文档结构，并使用{@code number}做为唯一字段属性，可通过{@link DocumentConverterUtils#NUMBER_KEY}获取。
 * 5.文档型结构不支持key为复杂Object，默认至少支持{@link Integer}{@link Long}{@link String}，即基础的整数（转字符串）和直接字符串，
 * 至于是否支持枚举key和字符串之间的转换，与具体的实现有关。
 *
 * @author wjybxx
 * date 2023/4/4
 */
public interface DocumentConverter<T> extends Converter<T> {

    /**
     * 写入pojo对象时的typeKey，最常见的情况是{@literal "_class"}
     */
    @Nonnull
    String getTypeKey();

    DocumentCodecRegistry codecRegistry();

    TypeNameRegistry typeNameRegistry();

}