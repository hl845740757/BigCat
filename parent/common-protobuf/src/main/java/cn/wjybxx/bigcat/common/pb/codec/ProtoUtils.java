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

package cn.wjybxx.bigcat.common.pb.codec;

import cn.wjybxx.bigcat.common.ClassScanner;
import com.google.protobuf.*;

import javax.annotation.Nonnull;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.util.Collection;
import java.util.Objects;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * protocol buffer工具类
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class ProtoUtils {

    public static Set<Class<?>> scan(Set<String> packages) {
        return packages.stream()
                .map(scanPackage -> ClassScanner.findClasses(scanPackage,
                        name -> name.indexOf('$') >= 0, // 协议都是内部类
                        ProtoUtils::isProtoBufferClass))
                .flatMap(Collection::stream)
                .collect(Collectors.toUnmodifiableSet());
    }

    private static boolean isProtoBufferClass(Class<?> messageClazz) {
        if (Modifier.isAbstract(messageClazz.getModifiers())) {
            return false;
        }
        return AbstractMessage.class.isAssignableFrom(messageClazz)
                || ProtocolMessageEnum.class.isAssignableFrom(messageClazz);
    }

    /**
     * 寻找protoBuf消息的parser对象
     *
     * @param clazz protoBuffer class
     * @return parser
     */
    public static <T extends MessageLite> Parser<T> findParser(@Nonnull Class<T> clazz) {
        Objects.requireNonNull(clazz);
        try {
            // 这个写法兼容2和3
            final Method method = clazz.getDeclaredMethod("getDefaultInstance");
            method.setAccessible(true);
            final Message instance = (Message) method.invoke(null);
            @SuppressWarnings("unchecked") final Parser<T> parserForType = (Parser<T>) instance.getParserForType();
            return parserForType;
        } catch (Exception e) {
            throw new IllegalArgumentException("bad class " + clazz.getName(), e);
        }
    }

    /**
     * 寻找protobuf枚举的映射信息
     *
     * @param clazz protoBuffer enum
     * @return map
     */
    public static <T extends ProtocolMessageEnum> Internal.EnumLiteMap<T> findMapper(@Nonnull Class<T> clazz) {
        Objects.requireNonNull(clazz);
        try {
            final Method method = clazz.getDeclaredMethod("internalGetValueMap");
            method.setAccessible(true);
            @SuppressWarnings("unchecked") final Internal.EnumLiteMap<T> mapper = (Internal.EnumLiteMap<T>) method.invoke(null);
            return mapper;
        } catch (Exception e) {
            throw new IllegalArgumentException("bad class " + clazz.getName(), e);
        }
    }

}