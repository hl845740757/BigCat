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

package cn.wjybxx.bigcat.common.codec.document;

import cn.wjybxx.bigcat.common.codec.EntityConverterUtils;
import cn.wjybxx.bigcat.common.IndexableEnum;

import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DocumentConverterUtils extends EntityConverterUtils {

    /** 枚举对象持久化时，使用number持久化 */
    public static final String NUMBER_KEY = "number";
    /**
     * {@link IndexableEnum}类型枚举作为对象的key时是否按照普通枚举处理
     * 当提供了新的实现时，应将该值置为false，或者不调用这里{@link #decodeName(String, Class)}和{@link #encodeName(Object, Class)}方法
     */
    public static volatile boolean indexableEnumAsCommonEnum = true;

    private static final int NAME_CACHE_SIZE = 200;
    private static final String[] arrayElementNameCache = new String[NAME_CACHE_SIZE];

    static {
        String[] nameCache = arrayElementNameCache;
        for (int idx = 0; idx < nameCache.length; idx++) {
            nameCache[idx] = Integer.toString(idx).intern();
        }
    }

    public static String arrayElementName(int idx) {
        if (idx >= 0 && idx <= NAME_CACHE_SIZE - 1) {
            return arrayElementNameCache[idx];
        }
        return Integer.toString(idx);
    }

    /** @return 如果返回null，外部自行处理 */
    @Nullable
    public static String encodeName(Object objKey, Class<?> declaredType) {
        if (objKey instanceof String) {
            return (String) objKey;
        }
        Class<?> keyClass = boxIfPrimitiveType(objKey.getClass());
        if (keyClass == Integer.class || keyClass == Long.class || keyClass == Short.class) {
            return objKey.toString(); // 整数
        }
        if (objKey instanceof IndexableEnum) {
            if (indexableEnumAsCommonEnum) {
                return objKey.toString();
            }
            return null; // 外部解码器处理
        }
        if (objKey instanceof Enum<?>) { // 枚举
            return objKey.toString();
        }
        return null;
    }

    /** @return 如果返回null，外部自行处理 */
    @SuppressWarnings({"unchecked", "rawtypes"})
    @Nullable
    public static <T> T decodeName(String stringKey, Class<T> declaredType) {
        if (declaredType == String.class || declaredType == Object.class) {
            return (T) stringKey;
        }
        Class<?> keyClass = EntityConverterUtils.boxIfPrimitiveType(declaredType);
        if (keyClass == Integer.class) {
            return (T) Integer.valueOf(stringKey);
        }
        if (keyClass == Long.class) {
            return (T) Long.valueOf(stringKey);
        }
        if (keyClass == Short.class) {
            return (T) Short.valueOf(stringKey);
        }
        if (IndexableEnum.class.isAssignableFrom(declaredType)) {
            if (indexableEnumAsCommonEnum) {
                return (T) Enum.valueOf((Class) keyClass, stringKey);
            }
            return null; // 外部解码器处理
        }
        if (keyClass.isEnum()) {
            return (T) Enum.valueOf((Class) keyClass, stringKey);
        }
        return null;
    }

}