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

import cn.wjybxx.dson.DsonContextType;
import cn.wjybxx.dson.DsonReader;
import cn.wjybxx.dson.DsonType;

/**
 * 顺序解码没有额外的开销，但数据兼容性会变差。
 * 如果觉得{@link DefaultDocumentObjectReader}的开销有点大，可以选择顺序解码模式
 *
 * @author wjybxx
 * date - 2023/4/23
 */
public class SequentialObjectReader extends AbstractObjectReader implements DocumentObjectReader {

    public SequentialObjectReader(DefaultDocumentConverter converter, DsonReader reader) {
        super(converter, reader);
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
            return reader.getCurrentName().equals(name);
        }
        if (reader.isAtType()) {
            if (reader.readDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
        }
        reader.readName(name);
        return true;
    }

}