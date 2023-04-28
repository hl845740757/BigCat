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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.ObjectUtils;

/**
 * 对象的二进制版本，number代替name
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonBinObject extends DsonObject<FieldNumber> {

    private BinClassId classId = BinClassId.OBJECT;

    public DsonBinObject() {
    }

    public DsonBinObject(int initialCapacity) {
        super(initialCapacity);
    }

    public BinClassId getClassId() {
        return classId;
    }

    public DsonBinObject setClassId(BinClassId classId) {
        this.classId = ObjectUtils.nullToDef(classId, BinClassId.OBJECT);
        return this;
    }

    /** @return this */
    public DsonBinObject append(FieldNumber key, DsonValue value) {
        return (DsonBinObject) super.append(key, value);
    }

    // endregion
}