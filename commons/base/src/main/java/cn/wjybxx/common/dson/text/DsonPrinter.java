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

package cn.wjybxx.common.dson.text;

/**
 * @author wjybxx
 * date - 2023/6/3
 */
public interface DsonPrinter {

    void writeName(String name);

    void writeInt32(String name, int value);

    void writeInt64(String name, long value);

    void writeFloat(String name, float value);

    void writeDouble(String name, double value);

    void writeBoolean(String name, boolean value);

    void writeString(String name, String value);

    void writeNull(String name);

    /** 无类型数字 */
    void writeNumber(String name, double value);

    /** 可指定字符串写入模式 */
    void writeString(String name, String value, StringMode mode);

    void writeStartArray(String name);

    void writeStartArray();

    void writeEndArray();

    void writeStartObject(String name);

    void writeStartObject();

    void writeEndObject();

}