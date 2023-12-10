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

package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * @author wjybxx
 * date - 2023/12/5
 */
@ClassImpl(singleton = "getInstance")
@BinarySerializable
@DocumentSerializable
public class SingletonTest {

    private final String name;
    private final int age;

    private SingletonTest(String name, int age) {
        this.name = name;
        this.age = age;
    }

    private static final SingletonTest INST = new SingletonTest("wjybxx", 29);

    public static SingletonTest getInstance() {
        return INST;
    }

    public String getName() {
        return name;
    }

    public int getAge() {
        return age;
    }
}
