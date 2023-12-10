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

import cn.wjybxx.common.codec.ConverterUtils;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;

/**
 * 生成的代码会继承该类
 *
 * @author wjybxx
 * date 2023/4/4
 */
public abstract class AbstractDocumentPojoCodecImpl<T> implements DocumentPojoCodecImpl<T> {

    // region

    @Nonnull
    @Override
    public abstract Class<T> getEncoderClass();

    @Override
    public boolean isWriteAsArray() {
        return ConverterUtils.isEncodeAsArray(getEncoderClass());
    }

    @Override
    public boolean autoStartEnd() {
        return true;
    }

    // endregion

    // region doc

    @Override
    public final T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        final T instance = newInstance(reader, typeArgInfo);
        readFields(reader, instance, typeArgInfo);
        afterDecode(reader, instance, typeArgInfo);
        return instance;
    }

    /**
     * 创建一个对象，如果是一个抽象类，应该抛出异常
     */
    protected abstract T newInstance(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo);

    /**
     * 从输入流中读取所有序列化的字段到指定实例上。
     *
     * @param instance 可以是子类实例
     */
    public abstract void readFields(DocumentObjectReader reader, T instance, TypeArgInfo<?> typeArgInfo);

    /**
     * 用于执行用户序列化完成的钩子方法
     */
    protected void afterDecode(DocumentObjectReader reader, T instance, TypeArgInfo<?> typeArgInfo) {

    }

    @Override
    public abstract void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style);

    // endregion

}