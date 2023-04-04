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

/**
 * 如果你的项目使用了Spring等工具，需要使用{@link #CLASS_NAME}做类型别名；
 * 其它情况下，建议使用{@link #TYPE_ALIAS}，这可以保持类型信息简短和可跨语言。
 *
 * @author wjybxx
 * date 2023/4/3
 */
public enum TypeKeyMode {

    /**
     * 使用类型的全限定名。
     * 兼容Spring等工具
     */
    CLASS_NAME("_class"),

    /**
     * 类型别名。
     * 注意：一个对象的类型别名也是唯一的，且应当与语言无关的。请尽量保持简短和清晰。
     */
    TYPE_ALIAS("_typeAlias");

    public final String typeKey;

    TypeKeyMode(String typeKey) {
        this.typeKey = typeKey;
    }

    public String getTypeKey() {
        return typeKey;
    }

}