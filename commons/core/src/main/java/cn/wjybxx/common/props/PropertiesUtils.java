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

import cn.wjybxx.common.CollectionUtils;
import org.apache.commons.lang3.math.NumberUtils;

import java.util.HashMap;
import java.util.Map;
import java.util.Properties;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
@SuppressWarnings("unused")
public class PropertiesUtils {

    // region

    public static int getInt(Properties properties, String key) {
        String v = properties.getProperty(key);
        return NumberUtils.toInt(v);
    }

    public static int getInt(Properties properties, String key, int def) {
        String v = properties.getProperty(key);
        return NumberUtils.toInt(v, def);
    }

    public static long getLong(Properties properties, String key) {
        String v = properties.getProperty(key);
        return NumberUtils.toLong(v);
    }

    public static long getLong(Properties properties, String key, long def) {
        String v = properties.getProperty(key);
        return NumberUtils.toLong(v, def);
    }

    public static float getFloat(Properties properties, String key) {
        String v = properties.getProperty(key);
        return NumberUtils.toFloat(v);
    }

    public static float getFloat(Properties properties, String key, float def) {
        String v = properties.getProperty(key);
        return NumberUtils.toFloat(v, def);
    }

    public static double getDouble(Properties properties, String key) {
        String v = properties.getProperty(key);
        return NumberUtils.toDouble(v);
    }

    public static double getDouble(Properties properties, String key, double def) {
        String v = properties.getProperty(key);
        return NumberUtils.toDouble(v, def);
    }

    public static String getString(Properties properties, String key) {
        return properties.getProperty(key);
    }

    public static String getString(Properties properties, String key, String def) {
        return properties.getProperty(key, def);
    }

    public static short getShort(Properties properties, String key) {
        String v = properties.getProperty(key);
        return NumberUtils.toShort(v);
    }

    public static short getShort(Properties properties, String key, short def) {
        String v = properties.getProperty(key);
        return NumberUtils.toShort(v, def);
    }

    public static byte getByte(Properties properties, String key) {
        String v = properties.getProperty(key);
        return NumberUtils.toByte(v);
    }

    public static byte getByte(Properties properties, String key, byte def) {
        String v = properties.getProperty(key);
        return NumberUtils.toByte(v, def);
    }

    public static boolean getBool(Properties properties, String key) {
        String v = properties.getProperty(key);
        return toBoolean(v, false);
    }

    public static boolean getBool(Properties properties, String key, boolean def) {
        String v = properties.getProperty(key);
        return toBoolean(v, def);
    }

    public static boolean toBoolean(String value) {
        return toBoolean(value, false);
    }

    // Commons3中对Bool的解析不符合我们的期望
    public static boolean toBoolean(String value, boolean def) {
        if (value == null || value.isEmpty()) {
            return def;
        }
        value = value.trim().toLowerCase();
        if (value.isEmpty()) {
            return def;
        }
        return switch (value) {
            case "true", "yes", "y", "1" -> true;
            case "false", "no", "n", "0" -> false;
            default -> def;
        };
    }

    // endregion

    public static Map<String, String> toMap(Properties properties) {
        Set<String> keySet = properties.stringPropertyNames();
        HashMap<String, String> hashMap = CollectionUtils.newHashMap(keySet.size()); // key本就无序
        for (String key : keySet) {
            String value = properties.getProperty(key);
            if (value == null) continue;
            hashMap.put(key, value);
        }
        return hashMap;
    }

}