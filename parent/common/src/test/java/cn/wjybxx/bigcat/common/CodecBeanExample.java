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

package cn.wjybxx.bigcat.common;

import cn.wjybxx.bigcat.common.codec.AutoTypeArgs;
import cn.wjybxx.bigcat.common.codec.FieldImpl;
import cn.wjybxx.bigcat.common.codec.binary.BinarySerializable;
import cn.wjybxx.bigcat.common.codec.document.AutoFields;
import cn.wjybxx.bigcat.common.codec.document.DocumentSerializable;
import it.unimi.dsi.fastutil.ints.Int2IntMap;
import it.unimi.dsi.fastutil.ints.Int2IntOpenHashMap;

import javax.annotation.Nullable;
import java.util.EnumMap;
import java.util.Map;

/**
 * 编译之后，将 parent/common/target/generated-test-sources/test-annotations 设置为 test-resource 目录，
 * 就可以看见生成的代码是什么样的。
 *
 * @author wjybxx
 * date 2023/4/7
 */
@AutoTypeArgs
@AutoFields
@DocumentSerializable
@BinarySerializable
public class CodecBeanExample {

    private int age;
    private String name;

    private Map<Integer, String> age2NameMap;
    private EnumMap<Sex, String> sex2NameMap;

    @FieldImpl(Int2IntOpenHashMap.class)
    private Int2IntMap currencyMap;

    @BinarySerializable
    @DocumentSerializable
    public enum Sex implements IndexableEnum {

        MALE(1),
        FEMALE(2);

        public final int number;

        Sex(int number) {
            this.number = number;
        }

        @Override
        public int getNumber() {
            return number;
        }

        private static final IndexableEnumMapper<Sex> MAPPER = EnumUtils.mapping(values());

        @Nullable
        public static Sex forNumber(int number) {
            return MAPPER.forNumber(number);
        }

        public static Sex checkedForNumber(int number) {
            return MAPPER.checkedForNumber(number);
        }
    }
}