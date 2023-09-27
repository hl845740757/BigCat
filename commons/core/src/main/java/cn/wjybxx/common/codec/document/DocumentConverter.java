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

package cn.wjybxx.common.codec.document;

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.Converter;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.TypeMetaRegistry;
import cn.wjybxx.dson.DsonArray;
import cn.wjybxx.dson.DsonObject;
import cn.wjybxx.dson.DsonValue;
import cn.wjybxx.dson.text.DsonMode;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.io.Reader;
import java.io.Writer;

/**
 * 文档转换器
 * 将对象转换为文档或类文档结构，比如：Json/Bson/Yaml/Lua，主要用于持久化存储
 * <p>
 * 1.文档是指人类可读的文本结构，以可读性为主，与文档型数据库并不直接关联。
 * 2.在设计上，文档编解码器对效率和压缩比例的追求较低，API会考虑易用性，因此{@link DocumentObjectReader}提供随机读的API。
 * 3.在设计上，是为了持久化的，为避免破坏用户数据，因此对于数组类型的数据结构不存储其类型信息，因此缺少静态类型信息的地方将无法准确解码。
 * 4.我们约定，持久化一个枚举对象时，写为文档结构，并使用{@code number}做为唯一字段属性，可通过{@link DocumentConverterUtils#NUMBER_KEY}获取。
 * 5.文档型结构不支持key为复杂Object，默认只支持{@link Integer}{@link Long}{@link String}，即基础的整数（转字符串）和直接字符串，
 * 至于是否支持枚举key和字符串之间的转换，与具体的实现有关。
 *
 * @author wjybxx
 * date 2023/4/4
 */
@ThreadSafe
public interface DocumentConverter extends Converter {

    // region 文本编解码

    /**
     * 将一个对象写入源
     * 如果对象的运行时类型和{@link TypeArgInfo#declaredType}一致，则会省去编码结果中的类型信息
     *
     * @param dsonMode    文本模式
     * @param typeArgInfo 对象的类型信息
     */
    @Nonnull
    String writeAsDson(Object value, DsonMode dsonMode, TypeArgInfo<?> typeArgInfo);

    /**
     * 从数据源中读取一个对象
     *
     * @param source      数据源
     * @param dsonMode    文本模式
     * @param typeArgInfo 要读取的目标类型信息，部分实现支持投影
     */
    <U> U readFromDson(CharSequence source, DsonMode dsonMode, TypeArgInfo<U> typeArgInfo);

    /**
     * 将一个对象写入指定writer
     *
     * @param dsonMode    文本模式
     * @param typeArgInfo 对象的类型信息
     * @param writer      用于接收输出
     */
    void writeAsDson(Object value, DsonMode dsonMode, TypeArgInfo<?> typeArgInfo, Writer writer);

    /**
     * 从数据源中读取一个对象
     *
     * @param source      用于支持大数据源
     * @param dsonMode    文本模式
     * @param typeArgInfo 要读取的目标类型信息，部分实现支持投影
     */
    <U> U readFromDson(Reader source, DsonMode dsonMode, TypeArgInfo<U> typeArgInfo);

    /**
     * @param value       顶层对象必须的容器对象，Object和数组
     * @param typeArgInfo 对象的类型信息
     * @return {@link DsonObject}或{@link DsonArray}
     */
    DsonValue writeAsDsonValue(Object value, TypeArgInfo<?> typeArgInfo);

    /**
     * @param source      {@link DsonObject}或{@link DsonArray}
     * @param typeArgInfo 要读取的目标类型信息
     */
    <U> U readFromDsonValue(DsonValue source, TypeArgInfo<U> typeArgInfo);

    // endregion

    // region 快捷方法

    default String writeAsDson(Object value, TypeArgInfo<?> typeArgInfo) {
        return writeAsDson(value, DsonMode.STANDARD, typeArgInfo);
    }

    default <U> U readFromDson(CharSequence source, TypeArgInfo<U> typeArgInfo) {
        return readFromDson(source, DsonMode.STANDARD, typeArgInfo);
    }

    default String writeAsDson(Object value) {
        return writeAsDson(value, DsonMode.STANDARD, TypeArgInfo.OBJECT);
    }

    default Object readFromDson(CharSequence source) {
        return readFromDson(source, DsonMode.STANDARD, TypeArgInfo.OBJECT);
    }

    // endregion

    DocumentCodecRegistry codecRegistry();

    TypeMetaRegistry typeMetaRegistry();

    ConvertOptions options();

}