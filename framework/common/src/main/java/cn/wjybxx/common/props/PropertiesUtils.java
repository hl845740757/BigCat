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

package cn.wjybxx.common.props;

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.ObjectUtils;

import java.util.HashMap;
import java.util.Map;
import java.util.Properties;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class PropertiesUtils {

    // region convert

    public static String getString(Properties properties, String key) {
        return properties.getProperty(key);
    }

    public static String getString(Properties properties, String key, String def) {
        return properties.getProperty(key, def);
    }

    public static int getInt(Properties properties, String key) {
        return getInt(properties, key, 0);
    }

    public static int getInt(Properties properties, String key, int def) {
        String v = properties.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Integer.parseInt(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static long getLong(Properties properties, String key) {
        return getLong(properties, key, 0);
    }

    public static long getLong(Properties properties, String key, long def) {
        String v = properties.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Long.parseLong(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static float getFloat(Properties properties, String key) {
        return getFloat(properties, key, 0f);
    }

    public static float getFloat(Properties properties, String key, float def) {
        String v = properties.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Float.parseFloat(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static double getDouble(Properties properties, String key) {
        return getDouble(properties, key, 0d);
    }

    public static double getDouble(Properties properties, String key, double def) {
        String v = properties.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Double.parseDouble(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static short getShort(Properties properties, String key) {
        return getShort(properties, key, (short) 0);
    }

    public static short getShort(Properties properties, String key, short def) {
        String v = properties.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        try {
            return Short.parseShort(v);
        } catch (NumberFormatException ignore) {
            return def;
        }
    }

    public static boolean getBool(Properties properties, String key) {
        return getBool(properties, key, false);
    }

    public static boolean getBool(Properties properties, String key, boolean def) {
        String v = properties.getProperty(key);
        if (ObjectUtils.isEmpty(v)) {
            return def;
        }
        return toBoolean(v, def);
    }

    public static boolean toBoolean(String value, boolean def) {
        if (value == null || value.isEmpty()) {
            return def;
        }
        value = value.trim().toLowerCase(); // 固定转小写
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