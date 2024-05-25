/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

import cn.wjybxx.base.ClassScanner;
import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.bigcat.rpc.RpcSerializer;
import cn.wjybxx.dsoncodec.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.lang3.ArrayUtils;

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

    private final DsonConverter converter;

    public TestRpcSerializer() {
        List<Class<?>> codecClsList = scanBinaryCodecs();
        List<? extends DsonCodec<?>> codecImplList = codecClsList.stream()
                .map(TestRpcSerializer::newInstance)
                .toList();

        // 扫描的是Codec类，并不直接是可序列化的类
        List<? extends Class<?>> encoderClsList = codecImplList.stream()
                .map(DsonCodec::getEncoderClass)
                .toList();

        TypeMetaRegistry typeMetaRegistry = TypeMetaRegistries.fromMapper(new HashSet<>(encoderClsList), cls -> {
            return TypeMeta.of(cls, ObjectStyle.INDENT, cls.getSimpleName());
        });
        converter = DefaultDsonConverter.newInstance(typeMetaRegistry, codecImplList, ConverterOptions.DEFAULT);
    }

    private static DsonCodec<?> newInstance(Class<?> e) {
        try {
            return (DsonCodec<?>) e.getConstructor(ArrayUtils.EMPTY_CLASS_ARRAY).newInstance(ArrayUtils.EMPTY_OBJECT_ARRAY);
        } catch (Exception ex) {
            return ObjectUtils.rethrow(ex);
        }
    }

    private static List<Class<?>> scanBinaryCodecs() {
        List<String> packages = List.of("cn.wjybxx.common", "cn.wjybxx.bigcat");
        List<Class<?>> codecClsList = new ArrayList<>(10);
        for (String pkg : packages) {
            codecClsList.addAll(ClassScanner.findClasses(pkg, e -> e.endsWith("BinCodec"), cls -> {
                return DsonCodec.class.isAssignableFrom(cls)
                        && !cls.isAnnotationPresent(DsonCodecScanIgnore.class)
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
