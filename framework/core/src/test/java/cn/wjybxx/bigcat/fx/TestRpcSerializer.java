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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.common.ClassScanner;
import cn.wjybxx.dson.codec.*;
import cn.wjybxx.dson.codec.binary.BinaryConverter;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.dson.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.dson.codec.binary.DefaultBinaryConverter;
import cn.wjybxx.common.rpc.RpcSerializer;
import org.apache.commons.lang3.ArrayUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

import javax.annotation.Nonnull;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/10/29
 */
public class TestRpcSerializer implements RpcSerializer {

    private final BinaryConverter converter;

    public TestRpcSerializer() {
        List<Class<?>> codecClsList = scanBinaryCodecs();
        List<? extends BinaryPojoCodecImpl<?>> codecImplList = codecClsList.stream()
                .map(TestRpcSerializer::newInstance)
                .toList();

        // 扫描的是Codec类，并不直接是可序列化的类
        List<? extends Class<?>> encoderClsList = codecImplList.stream()
                .map(BinaryPojoCodecImpl::getEncoderClass)
                .toList();

        TypeMetaRegistry typeMetaRegistry = TypeMetaRegistries.fromMapper(new HashSet<>(encoderClsList), cls -> {
            int ns = cls.getPackageName().startsWith("cn.wjybxx.common") ? 1 : 2;
            int lclassId = cls.getName().hashCode();
            return TypeMeta.of(cls, new ClassId(ns, lclassId));
        });
        converter = DefaultBinaryConverter.newInstance(codecImplList, typeMetaRegistry, ConvertOptions.DEFAULT);
    }

    private static BinaryPojoCodecImpl<?> newInstance(Class<?> e) {
        try {
            return (BinaryPojoCodecImpl<?>) e.getConstructor(ArrayUtils.EMPTY_CLASS_ARRAY).newInstance(ArrayUtils.EMPTY_OBJECT_ARRAY);
        } catch (Exception ex) {
            return ExceptionUtils.rethrow(ex);
        }
    }

    private static List<Class<?>> scanBinaryCodecs() {
        List<String> packages = List.of("cn.wjybxx.common", "cn.wjybxx.bigcat");
        List<Class<?>> codecClsList = new ArrayList<>(10);
        for (String pkg : packages) {
            codecClsList.addAll(ClassScanner.findClasses(pkg, e -> e.endsWith("BinCodec"), cls -> {
                return BinaryPojoCodecImpl.class.isAssignableFrom(cls)
                        && !cls.isAnnotationPresent(BinaryPojoCodecScanIgnore.class)
                        && Arrays.stream(cls.getConstructors()).anyMatch(e -> e.getParameterCount() == 0 && Modifier.isPublic(e.getModifiers()));
            }));
        }
        return codecClsList;
    }

    @Nonnull
    @Override
    public byte[] write(@Nonnull Object value) {
        return converter.write(value);
    }

    @Override
    public Object read(@Nonnull byte[] source) {
        return converter.read(source);
    }
}
