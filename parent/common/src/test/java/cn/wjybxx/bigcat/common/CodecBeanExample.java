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
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinarySerializable;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import cn.wjybxx.bigcat.common.codec.document.AutoFields;
import cn.wjybxx.bigcat.common.codec.document.DocumentReader;
import cn.wjybxx.bigcat.common.codec.document.DocumentSerializable;
import cn.wjybxx.bigcat.common.codec.document.DocumentWriter;
import it.unimi.dsi.fastutil.ints.Int2IntMap;
import it.unimi.dsi.fastutil.ints.Int2IntOpenHashMap;

import javax.annotation.Nullable;
import java.util.*;

/**
 * 编译之后，将 parent/common/target/generated-test-sources/test-annotations 设置为 test-resource 目录，
 * 就可以看见生成的代码是什么样的。
 * <p>
 * 这里为了减少代码，字段都定义为了public，避免getter/setter影响阅读
 *
 * @author wjybxx
 * date 2023/4/7
 */
@AutoTypeArgs
@AutoFields
@DocumentSerializable
@BinarySerializable
public class CodecBeanExample {

    public int age;
    public String name;

    public Map<Integer, String> age2NameMap;
    public Map<Sex, String> sex2NameMap1;
    public EnumMap<Sex, String> sex2NameMap2;
    @FieldImpl(EnumMap.class)
    public Map<Sex, String> sex2NameMap3;

    public Set<Sex> sexSet1;
    public EnumSet<Sex> sexSet2;
    @FieldImpl(EnumSet.class)
    public Set<Sex> sexSet3;

    public List<String> stringList1;
    public ArrayList<String> stringList2;
    @FieldImpl(LinkedList.class)
    public List<String> stringList3;

    public Int2IntOpenHashMap currencyMap1;
    @FieldImpl(Int2IntOpenHashMap.class)
    public Int2IntMap currencyMap2;

    @FieldImpl(writeProxy = "writeCustom", readProxy = "readCustom")
    public Object custom;

    //
    public void writeCustom(BinaryWriter writer) {
        writer.writeObject(custom, TypeArgInfo.OBJECT);
    }

    public void readCustom(BinaryReader reader) {
        this.custom = reader.readObject(TypeArgInfo.OBJECT);
    }

    public void writeCustom(DocumentWriter writer) {

    }

    public void readCustom(DocumentReader reader) {

    }

    //
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

        public static final IndexableEnumMapper<Sex> MAPPER = EnumUtils.mapping(values());

        @Nullable
        public static Sex forNumber(int number) {
            return MAPPER.forNumber(number);
        }

        public static Sex checkedForNumber(int number) {
            return MAPPER.checkedForNumber(number);
        }
    }
}