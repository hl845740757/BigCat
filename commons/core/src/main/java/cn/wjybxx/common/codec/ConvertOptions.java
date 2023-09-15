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

package cn.wjybxx.common.codec;

import cn.wjybxx.common.OptionalBool;
import cn.wjybxx.common.codec.document.codecs.MapCodec;
import cn.wjybxx.dson.text.DsonTextWriterSettings;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
@Immutable
public class ConvertOptions {

    /** 递归深度限制 */
    public final int recursionLimit;
    /** classId的写入策略 */
    public final ClassIdPolicy classIdPolicy;

    /**
     * 是否写入对象基础类型字段的默认值
     * 1.数值类型默认值为0
     * 2.bool类型默认值为false
     * <p>
     * 基础值类型需要单独控制，因为有时候我们仅想不输出null，但要输出基础类型字段的默认值 -- 通常是在文本模式下。
     */
    public final boolean appendDef;
    /**
     * 是否写入对象内的null值
     * 1.只在文档编解码中生效
     * 2.对于一般的对象可不写入，因为ObjectReader是支持随机读的
     */
    public final boolean appendNull;
    /**
     * 是否把Map编码为普通对象
     * 1.只在文档编解码中生效
     * 2.如果要将一个Map结构编码为普通对象，<b>Key的运行时必须和声明类型相同</b>，且只支持String、Integer、Long、EnumLite。
     * 3.即使不开启该选项，用户也可以通过定义字段的writeProxy实现将Map写为普通Object - 可参考{@link MapCodec}
     *
     * <h3>Map不是Object</h3>
     * 本质上讲，Map是数组，而不是普通的Object，因为标准的Map是允许复杂key的，因此Map默认应该序列化为数组。但存在两个特殊的场景：
     * 1.与脚本语言通信
     * 脚本语言通常没有静态语言中的字典结构，由object充当，但object不支持复杂的key作为键，通常仅支持数字和字符串作为key。
     * 因此在与脚本语言通信时，要求将Map序列化为简单的object。
     * 2.配置文件读写
     * 配置文件通常是无类型的，因此读取到内存中通常是一个字典结构；程序在输出配置文件时，同样需要将字典结构输出为object结构。
     */
    public final boolean encodeMapAsObject;

    /** protoBuf对应的二进制子类型 */
    public final int pbBinaryType;
    /** 数字classId的转换器 */
    public final ClassIdConverter classIdConverter;
    /** 缓存池 */
    public final BufferPool bufferPool;
    /** 字符串缓存池 */
    public final StringBuilderPool stringBuilderPool;
    /** 文本编码设置 */
    public final DsonTextWriterSettings textWriterSettings;
    /** 类json文本编码 */
    public final DsonTextWriterSettings jsonWriterSettings;

    public ConvertOptions(Builder builder) {
        this.recursionLimit = builder.recursionLimit;
        this.classIdPolicy = builder.classIdPolicy;

        this.appendDef = builder.appendDef.orElse(false);
        this.appendNull = builder.appendNull.orElse(false);
        this.encodeMapAsObject = builder.encodeMapAsObject.orElse(false);

        this.pbBinaryType = builder.pbBinaryType;
        this.classIdConverter = builder.classIdConverter;
        this.bufferPool = builder.bufferPool;
        this.stringBuilderPool = builder.stringBuilderPool;
        this.textWriterSettings = Objects.requireNonNull(builder.textWriterSettings);
        this.jsonWriterSettings = Objects.requireNonNull(builder.jsonWriterSettings);

        if (textWriterSettings.jsonLike) {
            throw new IllegalArgumentException("textWriterSettings.jsonLike must be false");
        }
        if (!jsonWriterSettings.jsonLike) {
            throw new IllegalArgumentException("jsonWriterSetting.jsonLike must be true");
        }
    }

    public static ConvertOptions DEFAULT = newBuilder().build();

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private ClassIdPolicy classIdPolicy = ClassIdPolicy.OPTIMIZED;
        private int recursionLimit = 32;

        private OptionalBool appendDef = OptionalBool.TRUE;
        private OptionalBool appendNull = OptionalBool.FALSE;
        private OptionalBool encodeMapAsObject = OptionalBool.FALSE;

        private int pbBinaryType = 127;
        private ClassIdConverter classIdConverter = new DefaultClassIdConverter();
        private BufferPool bufferPool = LocalPools.BUFFER_POOL;
        private StringBuilderPool stringBuilderPool = LocalPools.STRING_BUILDER_POOL;
        private DsonTextWriterSettings textWriterSettings = DsonTextWriterSettings.DEFAULT;
        private DsonTextWriterSettings jsonWriterSettings = DsonTextWriterSettings.JSON_DEFAULT;

        public int getRecursionLimit() {
            return recursionLimit;
        }

        public Builder setRecursionLimit(int recursionLimit) {
            if (recursionLimit < 1) throw new IllegalArgumentException("invalid limit " + recursionLimit);
            this.recursionLimit = recursionLimit;
            return this;
        }

        public ClassIdPolicy getClassIdPolicy() {
            return classIdPolicy;
        }

        public Builder setClassIdPolicy(ClassIdPolicy classIdPolicy) {
            this.classIdPolicy = Objects.requireNonNull(classIdPolicy);
            return this;
        }

        public OptionalBool getAppendDef() {
            return appendDef;
        }

        public Builder setAppendDef(OptionalBool appendDef) {
            this.appendDef = appendDef;
            return this;
        }

        public OptionalBool getAppendNull() {
            return appendNull;
        }

        public Builder setAppendNull(OptionalBool appendNull) {
            this.appendNull = Objects.requireNonNull(appendNull);
            return this;
        }

        public OptionalBool getEncodeMapAsObject() {
            return encodeMapAsObject;
        }

        public Builder setEncodeMapAsObject(OptionalBool encodeMapAsObject) {
            this.encodeMapAsObject = Objects.requireNonNull(encodeMapAsObject);
            return this;
        }

        public int getPbBinaryType() {
            return pbBinaryType;
        }

        public Builder setPbBinaryType(int pbBinaryType) {
            this.pbBinaryType = pbBinaryType;
            return this;
        }

        public ClassIdConverter getClassIdResolver() {
            return classIdConverter;
        }

        public Builder setClassIdResolver(ClassIdConverter classIdConverter) {
            this.classIdConverter = Objects.requireNonNull(classIdConverter);
            return this;
        }

        public BufferPool getBufferPool() {
            return bufferPool;
        }

        public Builder setBufferPool(BufferPool bufferPool) {
            this.bufferPool = Objects.requireNonNull(bufferPool);
            return this;
        }

        public StringBuilderPool getStringBuilderPool() {
            return stringBuilderPool;
        }

        public Builder setStringBuilderPool(StringBuilderPool stringBuilderPool) {
            this.stringBuilderPool = stringBuilderPool;
            return this;
        }

        public DsonTextWriterSettings getTextWriterSettings() {
            return textWriterSettings;
        }

        public Builder setTextWriterSettings(DsonTextWriterSettings textWriterSettings) {
            this.textWriterSettings = textWriterSettings;
            return this;
        }

        public DsonTextWriterSettings getJsonWriterSettings() {
            return jsonWriterSettings;
        }

        public Builder setJsonWriterSettings(DsonTextWriterSettings jsonWriterSettings) {
            this.jsonWriterSettings = jsonWriterSettings;
            return this;
        }

        public ConvertOptions build() {
            return new ConvertOptions(this);
        }
    }

}