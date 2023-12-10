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

package cn.wjybxx.common.codec.document;

import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.pool.DefaultObjectPool;
import cn.wjybxx.common.pool.ObjectPool;
import cn.wjybxx.dson.*;
import it.unimi.dsi.fastutil.objects.ObjectLinkedOpenHashSet;

import javax.annotation.Nonnull;
import java.util.Iterator;
import java.util.Objects;
import java.util.Set;

/**
 * 默认实现之所以限定{@link DsonObjectReader}，是因为文档默认情况下用于解析数据库和文本文件，
 * 文档中的字段顺序可能和类定义不同，因此顺序读的容错较低。
 *
 * @author wjybxx
 * date - 2023/4/23
 */
public class DefaultDocumentObjectReader extends AbstractObjectReader implements DocumentObjectReader {

    private static final ThreadLocal<ObjectPool<ObjectLinkedOpenHashSet<String>>> LOCAL_POOL
            = ThreadLocal.withInitial(() -> new DefaultObjectPool<>(
            ObjectLinkedOpenHashSet::new,
            ObjectLinkedOpenHashSet::clear, 2, 16));

    private final ObjectPool<ObjectLinkedOpenHashSet<String>> keySetPool;

    public DefaultDocumentObjectReader(DefaultDocumentConverter converter, DsonObjectReader reader) {
        super(converter, reader);
        this.keySetPool = LOCAL_POOL.get(); // 缓存下来，技减少查询
    }

    @Override
    public boolean readName(String name) {
        DsonReader reader = this.reader;
        if (reader.getContextType() == DsonContextType.ARRAY) {
            if (name != null) throw new IllegalArgumentException("the name of array element must be null");
            if (reader.isAtValue()) {
                return true;
            }
            if (reader.isAtType()) {
                return reader.readDsonType() != DsonType.END_OF_OBJECT;
            }
            return reader.getCurrentDsonType() != DsonType.END_OF_OBJECT;
        }
        if (reader.isAtValue()) {
            if (reader.getCurrentName().equals(name)) {
                return true;
            }
            reader.skipValue();
        }
        // 用户未调用readDsonType，可指定下一个key的值
        if (reader.isAtType()) {
            KeyIterator keyItr = (KeyIterator) reader.attachment();
            if (keyItr.keySet.contains(name)) {
                keyItr.setNext(name);
                reader.readDsonType();
                reader.readName();
                return true;
            }
            return false;
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            reader.readName(name);
            return true;
        }
    }

    @Override
    public void readStartObject(@Nonnull TypeArgInfo<?> typeArgInfo) {
        super.readStartObject(typeArgInfo);

        DsonObjectReader reader = (DsonObjectReader) this.reader;
        KeyIterator keyItr = new KeyIterator(reader.getkeySet(), keySetPool.get());
        reader.setKeyItr(keyItr, DsonNull.NULL);
        reader.attach(keyItr);
    }

    @Override
    public void readEndObject() {
        // 需要在readEndObject之前保存下来
        KeyIterator keyItr = (KeyIterator) reader.attach(null);
        super.readEndObject();

        keySetPool.returnOne(keyItr.keyQueue);
        keyItr.keyQueue = null;
    }

    private static class KeyIterator implements Iterator<String> {

        Set<String> keySet;
        ObjectLinkedOpenHashSet<String> keyQueue;

        public KeyIterator(Set<String> keySet, ObjectLinkedOpenHashSet<String> keyQueue) {
            this.keySet = keySet;
            this.keyQueue = keyQueue;
            keyQueue.addAll(keySet);
        }

        public void setNext(String key) {
            Objects.requireNonNull(key);
            keyQueue.addAndMoveToFirst(key);
        }

        @Override
        public boolean hasNext() {
            return keyQueue.size() > 0;
        }

        @Override
        public String next() {
            return keyQueue.removeFirst();
        }
    }
}