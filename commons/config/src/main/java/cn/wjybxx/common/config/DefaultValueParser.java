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

package cn.wjybxx.common.config;

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.*;
import org.apache.commons.lang3.StringUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.Immutable;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.Array;
import java.util.*;

/**
 * 无状态的，可并发调用
 *
 * @author wjybxx
 * date 2023/4/15
 */
@Immutable
@ThreadSafe
public class DefaultValueParser implements ValueParser {

    // 基本数据类型
    public static final String STRING = "string";
    public static final String INT32 = "int32";
    public static final String INT64 = "int64";
    public static final String FLOAT = "float";
    public static final String DOUBLE = "double";
    public static final String BOOL = "bool";
    // json并不是真实的类型，只是标注为json有利于工具对字符串的格式进行检查
    public static final String JSON = "json";

    private static final List<String> INTEGER_TYPES = List.of(INT32, INT64);
    private static final List<String> FLOAT_DOUBLE_TYPES = List.of(FLOAT, DOUBLE);
    private static final List<String> BOOL_TYPE = List.of(BOOL);

    public static final Set<String> BASIC_TYPES = Set.of(STRING, INT32, INT64, FLOAT, DOUBLE, BOOL);
    public static final Set<String> SUPPORTED_TYPES;

    static {
        Set<String> tempSupportedTypes = new HashSet<>(32);
        tempSupportedTypes.addAll(BASIC_TYPES);
        // 一维和二维数组
        for (String typeString : BASIC_TYPES) {
            tempSupportedTypes.add(typeString + "[]");
            tempSupportedTypes.add(typeString + "[][]");
        }
        tempSupportedTypes.add(JSON);
        SUPPORTED_TYPES = Set.copyOf(tempSupportedTypes);
    }

    private static final DefaultValueParser INSTANCE = new DefaultValueParser();

    public static DefaultValueParser getInstance() {
        return INSTANCE;
    }

    private static boolean isExpectedType(List<String> expectedTypeStrings, String typeString) {
        for (int index = 0, size = expectedTypeStrings.size(); index < size; index++) {
            final String expectedTypeString = expectedTypeStrings.get(index);
            if (expectedTypeString.equals(typeString)) {
                return true;
            }
        }
        return false;
    }

    private static void checkTypeString(List<String> expectedTypeStrings, String typeString) {
        if (!isExpectedType(expectedTypeStrings, typeString)) {
            String msg = String.format("expectedTypeStrings: %s, realType: %s", expectedTypeStrings, typeString);
            throw new IllegalArgumentException(msg);
        }
    }

    @Override
    public Set<String> supportedTypes() {
        return SUPPORTED_TYPES;
    }

    @Override
    public String readAsString(String typeString, String value) {
        return value;
    }

    @Override
    public int readAsInt(String typeString, String value) {
        Objects.requireNonNull(value, "value");
        checkTypeString(INTEGER_TYPES, typeString);
        return Integer.parseInt(value);
    }

    @Override
    public long readAsLong(String typeString, String value) {
        Objects.requireNonNull(value, "value");
        checkTypeString(INTEGER_TYPES, typeString);
        return Long.parseLong(value);
    }

    @Override
    public float readAsFloat(String typeString, String value) {
        Objects.requireNonNull(value, "value");
        checkTypeString(FLOAT_DOUBLE_TYPES, typeString);
        return Float.parseFloat(value);
    }

    @Override
    public double readAsDouble(String typeString, String value) {
        Objects.requireNonNull(value, "value");
        checkTypeString(FLOAT_DOUBLE_TYPES, typeString);
        return Double.parseDouble(value);
    }

    @Override
    public boolean readAsBool(String typeString, String value) {
        Objects.requireNonNull(value, "value");
        checkTypeString(BOOL_TYPE, typeString);
        return parseBool(value);
    }

    private static boolean parseBool(String value) {
        return value.equals("1") ||
                value.equalsIgnoreCase("true");
    }

    @Override
    public <T> T readAsArray(@Nonnull String typeString, @Nullable String value, @Nonnull Class<T> typeToken) {
        Objects.requireNonNull(typeToken, "typeToken");
        Objects.requireNonNull(value, "value");
        checkArrayTypeString(typeString);
        checkArrayTypeToken(typeToken);
        checkDimensionalEquals(typeString, typeToken);

        if (StringUtils.isBlank(value)) {
            @SuppressWarnings("unchecked") final T emptyArray = (T) Array.newInstance(typeToken.getComponentType(), 0);
            return emptyArray;
        }

        if (getArrayDimensional(typeToken) == 1) {
            List<String> elements = new ArrayList<>(10);
            try (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, DsonCharStream.newCharStream(value, DsonMode.RELAXED))) {
                if (reader.readDsonType() != DsonType.ARRAY) throw new IllegalArgumentException(value);
                readOneDimensionArray(reader, elements);
                if (reader.readDsonType() != DsonType.END_OF_OBJECT) throw new IllegalArgumentException(value);
            }
            @SuppressWarnings("unchecked") final T result = (T) convert2Array(typeString, typeToken, elements.toArray(String[]::new));
            return result;
        } else {
            // 读取为二维list
            List<List<String>> twoDimensionElements = new ArrayList<>(4);
            try (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, DsonCharStream.newCharStream(value, DsonMode.RELAXED))) {
                if (reader.readDsonType() != DsonType.ARRAY) throw new IllegalArgumentException(value);
                readTwoDimensionArray(reader, twoDimensionElements);
                if (reader.readDsonType() != DsonType.END_OF_OBJECT) throw new IllegalArgumentException(value);
            }
            // list转数组
            @SuppressWarnings("unchecked") final T result = (T) Array.newInstance(typeToken.getComponentType(), twoDimensionElements.size());
            for (int index = 0, elementsSize = twoDimensionElements.size(); index < elementsSize; index++) {
                final List<String> elements = twoDimensionElements.get(index);
                final String subTypeString = typeString.substring(0, typeString.indexOf(']') + 1);
                final Object oneDimensionalArray = convert2Array(subTypeString, typeToken.getComponentType(), elements.toArray(String[]::new));
                Array.set(result, index, oneDimensionalArray);
            }
            return result;
        }
    }

    private static void readTwoDimensionArray(DsonTextReader reader, List<List<String>> twoDimensionElements) {
        reader.readStartArray();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            ArrayList<String> elements = new ArrayList<>(10);
            readOneDimensionArray(reader, elements);
            twoDimensionElements.add(elements);
        }
        reader.readEndArray();
    }

    private static void readOneDimensionArray(DsonTextReader reader, List<String> elements) {
        reader.readStartArray();
        reader.setCompClsNameToken(DsonTexts.clsNameTokenOfType(DsonType.STRING));
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            elements.add(reader.readString(null));
        }
        reader.readEndArray();
    }

    private static Object convert2Array(String typeString, final Class<?> typeToken, final String[] elements) {
        final Class<?> componentType = typeToken.getComponentType();
        if (componentType == String.class) {
            return elements;
        }

        final String componentTypeString = typeString.substring(0, typeString.indexOf('['));
        if (componentType == int.class) {
            checkTypeString(INTEGER_TYPES, componentTypeString);
            return Arrays.stream(elements)
                    .mapToInt(Integer::parseInt)
                    .toArray();
        }
        if (componentType == long.class) {
            checkTypeString(INTEGER_TYPES, componentTypeString);
            return Arrays.stream(elements)
                    .mapToLong(Long::parseLong)
                    .toArray();
        }
        if (componentType == double.class) {
            checkTypeString(FLOAT_DOUBLE_TYPES, componentTypeString);
            return Arrays.stream(elements)
                    .mapToDouble(Double::parseDouble)
                    .toArray();
        }

        if (componentType == float.class) {
            checkTypeString(FLOAT_DOUBLE_TYPES, componentTypeString);
            float[] result = new float[elements.length];
            for (int index = 0; index < elements.length; index++) {
                result[index] = Float.parseFloat(elements[index]);
            }
            return result;
        }
        if (componentType == boolean.class) {
            checkTypeString(BOOL_TYPE, componentTypeString);
            boolean[] result = new boolean[elements.length];
            for (int index = 0; index < elements.length; index++) {
                result[index] = parseBool(elements[index]);
            }
            return result;
        }
        throw new IllegalArgumentException("unsupported component type: " + componentType);
    }

    private static void checkArrayTypeString(String typeString) {
        if (!SUPPORTED_TYPES.contains(typeString) || !typeString.endsWith("[]")) {
            throw new IllegalArgumentException("tyString must end with [] or [][], typeString: " + typeString);
        }
    }

    private static void checkArrayTypeToken(Class<?> typeToken) {
        if (!typeToken.isArray()) {
            throw new IllegalArgumentException("typeToken must be an array, typeToken: " + typeToken);
        }
    }

    private static void checkDimensionalEquals(String typeString, Class<?> typeToken) {
        final int typeStringDimensional = getArrayDimensional(typeString);
        final int typeTokenDimensional = getArrayDimensional(typeToken);
        if (typeTokenDimensional != typeStringDimensional) {
            final String msg = String.format("typeString and typeToken have different dimensions, typeString: %s, typeToken: %s",
                    typeString, typeToken);
            throw new IllegalArgumentException(msg);
        }
    }

    private static int getArrayDimensional(String typeString) {
        return typeString.endsWith("[][]") ? 2 : 1;
    }

    private static int getArrayDimensional(Class<?> typeToken) {
        int result = 0;
        while (typeToken.isArray()) {
            result++;
            typeToken = typeToken.getComponentType();
        }
        return result;
    }

}