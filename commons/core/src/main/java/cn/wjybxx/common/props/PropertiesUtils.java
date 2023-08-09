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

import org.apache.commons.lang3.BooleanUtils;
import org.apache.commons.lang3.math.NumberUtils;

import java.util.Properties;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class PropertiesUtils {

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

    public static boolean getBool(Properties properties, String key) {
        String v = properties.getProperty(key);
        return BooleanUtils.toBoolean(v);
    }

    public static boolean getBool(Properties properties, String key, boolean def) {
        String v = properties.getProperty(key);
        return v != null ? BooleanUtils.toBoolean(v) : def;
    }

    public static String getString(Properties properties, String key) {
        return properties.getProperty(key);
    }

    public static String getString(Properties properties, String key, String def) {
        return properties.getProperty(key, def);
    }

}