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
import cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinarySerializable;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;

import java.util.IdentityHashMap;

/**
 * {@code  IdentityHashMap.size}是非 transient，也不可直接访问
 *
 * @author wjybxx
 * date 2023/4/14
 */
@AutoTypeArgs
@BinarySerializable(skipFields = "size", annotations = BinaryPojoCodecScanIgnore.class)
public class CustomMapCodecTest<K, V> extends IdentityHashMap<K, V> {

    public CustomMapCodecTest() {
    }

    public void writeObject(BinaryWriter writer) {
        for (Entry<K, V> entry : this.entrySet()) {
            writer.writeObject(entry.getKey());
            writer.writeObject(entry.getValue());
        }
    }

    public void readObject(BinaryReader reader) {
        while (!reader.isAtEndOfObject()) {
            K k = reader.readObject();
            V v = reader.readObject();
            put(k, v);
        }
    }

    public void afterDecode() {

    }

}