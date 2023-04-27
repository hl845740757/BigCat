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

package cn.wjybxx.common;

import cn.wjybxx.common.dson.AutoTypeArgs;
import cn.wjybxx.common.dson.ClassImpl;
import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.common.dson.binary.BinarySerializable;

import java.util.IdentityHashMap;

/**
 * {@code  IdentityHashMap.size}是非 transient，也不可直接访问
 *
 * @author wjybxx
 * date 2023/4/14
 */
@AutoTypeArgs
@ClassImpl(skipFields = "size")
@BinarySerializable(annotations = BinaryPojoCodecScanIgnore.class)
public class CustomMapCodecTest<K, V> extends IdentityHashMap<K, V> {

    public CustomMapCodecTest() {
    }

    public void writeObject(BinaryObjectWriter writer) {
        for (Entry<K, V> entry : this.entrySet()) {
            writer.writeObject(0, entry.getKey());
            writer.writeObject(0, entry.getValue());
        }
    }

    public void readObject(BinaryObjectReader reader) {
        while (!reader.isAtEndOfObject()) {
            K k = reader.readObject(0);
            V v = reader.readObject(0);
            put(k, v);
        }
    }

    public void afterDecode() {

    }

}