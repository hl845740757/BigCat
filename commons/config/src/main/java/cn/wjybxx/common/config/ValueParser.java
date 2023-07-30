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

import java.util.Set;

/**
 * 用于解析表格中的数据
 * 在接口层面，我们约定值可以向上扩展，不能向下扩展，即：int32可读取为int64，但int64不能读取为int32
 * 用户可以在读取后向下转换，但这里抛出异常以提醒程序员犯了错误。
 *
 * @author wjybxx
 * date 2023/4/15
 */
public interface ValueParser {

    /**
     * @return 支持的值类型
     */
    Set<String> supportedTypes();

    /**
     * 1.字符串通常是保持为配置的文本的。
     * 2.任意类型都可以读取为字符串，因为保留的用户的文本输入，都是字符串
     *
     * @return 获取单元格的字符串内容。
     */
    String readAsString(String typeString, String value);

    /**
     * @return 如果单元格是约定的int类型，则返回对应的int值
     */
    int readAsInt(String typeString, String value);

    /**
     * @return 如果单元格是约定的int或long类型，则返回对应的long值
     */
    long readAsLong(String typeString, String value);

    /**
     * @return 如果单元格是约定的float类型，则返回对应的float值
     */
    float readAsFloat(String typeString, String value);

    /**
     * @return 如果单元格是约定的float或double类型，则返回对应的double值
     */
    double readAsDouble(String typeString, String value);

    /**
     * @return 如果单元格是约定的bool类型，则返回对应的bool值。
     */
    boolean readAsBool(String typeString, String value);

    /**
     * @param typeToken 类型令牌，用于捕获类型信息；仅支持上面的6种接口的1维和2维数组
     * @return 如果单元格是约定的数组类型，则返回对应的数组类型。
     */
    <T> T readAsArray(String typeString, String value, Class<T> typeToken);

}