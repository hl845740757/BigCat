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

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonBinArray extends DsonArray {

    private BinClassId classId = BinClassId.OBJECT;

    public DsonBinArray() {
    }

    public DsonBinArray(List<DsonValue> values) {
        super(values);
    }

    DsonBinArray(List<DsonValue> values, boolean copy) {
        super(values, copy);
    }

    public BinClassId getClassId() {
        return classId;
    }

    public DsonBinArray setClassId(BinClassId classId) {
        this.classId = classId == null ? BinClassId.OBJECT : classId;
        return this;
    }
}