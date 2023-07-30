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
import cn.wjybxx.common.codec.document.codecs.MapAsObjectCodec;

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
     * 开启的特征值
     */
    public final int features = 0;
    /**
     * 是否写入对象内的null值
     * 1.只在文档编解码中生效
     * 2.对于一般的对象可不写入，因为ObjectReader是支持随机读的
     */
    public final boolean appendNull;
    /**
     * 是否把Map编码为普通对象
     * 1.只在文档编解码中生效
     * 2.在Dson中，Map被看做一个特殊的数组结构，而不是普通的Object，Map和Object本就是性质不同的对象。
     * 如果要将一个Map结构编码为普通对象，<b>Key的运行时必须和声明类型相同</b>，且只支持String、Integer、Long、EnumLite。
     * 3.即使不开启该选项，用户也可以通过定义字段的writeProxy实现将Map写为普通Object - 可参考{@link MapAsObjectCodec}
     */
    public final boolean encodeMapAsObject;

    /** protoBuf对应的二进制子类型 */
    public final int pbBinaryType;
    /** 数字classId的转换器 */
    public final ClassIdConverter classIdConverter;
    /** 缓存池 */
    public final BufferPool bufferPool;

    public ConvertOptions(Builder builder) {
        this.recursionLimit = builder.recursionLimit;
        this.classIdPolicy = builder.classIdPolicy;
        this.appendNull = builder.appendNull.orElse(false);
        this.encodeMapAsObject = builder.encodeMapAsObject.orElse(false);
        this.pbBinaryType = builder.pbBinaryType;
        this.classIdConverter = builder.classIdConverter;
        this.bufferPool = builder.bufferPool;
    }

    public static ConvertOptions DEFAULT = newBuilder().build();

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private ClassIdPolicy classIdPolicy = ClassIdPolicy.OPTIMIZED;
        private int recursionLimit = 32;

        private OptionalBool appendNull = OptionalBool.FALSE;
        private OptionalBool encodeMapAsObject = OptionalBool.FALSE;

        private int pbBinaryType = 127;
        private ClassIdConverter classIdConverter = new DefaultClassIdConverter();
        private BufferPool bufferPool = LocalBufferPool.INSTANCE;

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

        public ConvertOptions build() {
            return new ConvertOptions(this);
        }
    }

}