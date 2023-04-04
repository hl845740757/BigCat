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

import javax.annotation.Nullable;

/**
 * 类型名注册表
 * 在文档型编解码中，可读性是比较重要的，因此类型信息使用字符串，而不是数字。
 * <p>
 * 1.对象的类型名应当是稳定的，且应该尽量保持简短
 * 2.
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface TypeNameRegistry {

    /**
     * 通过类型获取类型的字符串标识
     */
    @Nullable
    String ofType(Class<?> type);

    /**
     * 通过字符串名字找到类型信息
     */
    @Nullable
    Class<?> ofName(String typeName);

    default String checkedOfType(Class<?> type) {
        String r = ofType(type);
        if (r == null) {
            throw new IllegalArgumentException("type: " + type);
        }
        return r;
    }

    default Class<?> checkedOfName(String name) {
        Class<?> r = ofName(name);
        if (r == null) {
            throw new IllegalArgumentException("name: " + name);
        }
        return r;
    }
}