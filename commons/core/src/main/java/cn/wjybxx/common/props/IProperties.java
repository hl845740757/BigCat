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

package cn.wjybxx.common.props;

import it.unimi.dsi.fastutil.doubles.DoubleArrayList;
import it.unimi.dsi.fastutil.doubles.DoubleList;
import it.unimi.dsi.fastutil.floats.FloatArrayList;
import it.unimi.dsi.fastutil.floats.FloatList;
import it.unimi.dsi.fastutil.ints.IntArrayList;
import it.unimi.dsi.fastutil.ints.IntList;
import it.unimi.dsi.fastutil.longs.LongArrayList;
import it.unimi.dsi.fastutil.longs.LongList;
import org.apache.commons.lang3.StringUtils;
import org.apache.commons.lang3.math.NumberUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * 主要为{@link java.util.Properties}提供快捷的解析方式
 * （项目的配置文件，我使用Dson文本）
 *
 * @author wjybxx
 * date 2023/4/14
 */
public interface IProperties extends Map<String, String> {

    // region 单值

    @Nullable
    String getAsString(String key);

    default String getAsString(String key, String defaultValue) {
        final String value = getAsString(key);
        return null != value ? value : defaultValue;
    }

    default int getAsInt(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return Integer.parseInt(value);
    }

    default int getAsInt(String key, int defaultValue) {
        return NumberUtils.toInt(getAsString(key), defaultValue);
    }

    default long getAsLong(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return Long.parseLong(value);
    }

    default long getAsLong(String key, long defaultValue) {
        return NumberUtils.toLong(getAsString(key), defaultValue);
    }

    default float getAsFloat(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return Float.parseFloat(value);
    }

    default float getAsFloat(String key, float defaultValue) {
        return NumberUtils.toFloat(getAsString(key), defaultValue);
    }

    default double getAsDouble(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return Double.parseDouble(value);
    }

    default double getAsDouble(String key, double defaultValue) {
        return NumberUtils.toDouble(getAsString(key), defaultValue);
    }

    /**
     * bool值解析允许由不同的方式
     *
     * @throws NullPointerException 如果键对应的值为null
     */
    default boolean getAsBool(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return PropertiesUtils.toBoolean(value, false);
    }

    default boolean getAsBool(String key, final boolean defaultValue) {
        final String value = getAsString(key);
        return PropertiesUtils.toBoolean(value, defaultValue);
    }

    default short getAsShort(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return Short.parseShort(value);
    }

    default short getAsShort(String key, short defaultValue) {
        return NumberUtils.toShort(getAsString(key), defaultValue);
    }

    default byte getAsByte(String key) {
        final String value = getAsString(key);
        Objects.requireNonNull(value);
        return Byte.parseByte(value);
    }

    default byte getAsByte(String key, byte defaultValue) {
        return NumberUtils.toByte(getAsString(key), defaultValue);
    }
    // endregion

    // region 数组

    /** @apiNote 如果默认的分割方式不合适，用户可以获取原始的字符串进行自定义分割 */
    @Nonnull
    default List<String> getAsStringArray(String key) {
        final String value = getAsString(key);
        if (StringUtils.isBlank(value)) {
            return new ArrayList<>();
        }
        final String[] splitArray = StringUtils.split(value, ',');
        final ArrayList<String> result = new ArrayList<>(splitArray.length);
        for (String e : splitArray) {
            result.add(e.trim());
        }
        return result;
    }

    default IntList getAsIntArray(String key) {
        final List<String> stringArray = Objects.requireNonNull(getAsStringArray(key));
        final IntList result = new IntArrayList(stringArray.size());
        for (String e : stringArray) {
            result.add(Integer.parseInt(e));
        }
        return result;
    }

    default LongList getAsLongArray(String key) {
        final List<String> stringArray = Objects.requireNonNull(getAsStringArray(key));
        final LongList result = new LongArrayList(stringArray.size());
        for (String e : stringArray) {
            result.add(Long.parseLong(e));
        }
        return result;
    }

    default FloatList getAsFloatArray(String key) {
        final List<String> stringArray = Objects.requireNonNull(getAsStringArray(key));
        final FloatList result = new FloatArrayList(stringArray.size());
        for (String e : stringArray) {
            result.add(Float.parseFloat(e));
        }
        return result;
    }

    default DoubleList getAsDoubleArray(String key) {
        final List<String> stringArray = Objects.requireNonNull(getAsStringArray(key));
        final DoubleList result = new DoubleArrayList(stringArray.size());
        for (String e : stringArray) {
            result.add(Double.parseDouble(e));
        }
        return result;
    }

    default String get(String key, String def) {
        return getAsString(key, def);
    }

    // endregion

    /** @return 可能是快照 */
    @Nonnull
    @Override
    Set<String> keySet();

    /** @return 可能是快照 */
    @Nonnull
    @Override
    Collection<String> values();

    /** @return 可能是快照 */
    @Nonnull
    @Override
    Set<Entry<String, String>> entrySet();

}