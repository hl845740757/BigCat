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
 * 对象的文档版本
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonDocObject extends DsonObject<String> {

    private DocClassId classId = DocClassId.OBJECT;

    public DsonDocObject() {
    }

    public DsonDocObject(int initialCapacity) {
        super(initialCapacity);
    }

    public DocClassId getClassId() {
        return classId;
    }

    public DsonDocObject setClassId(DocClassId classId) {
        this.classId = ObjectUtils.nullToDef(classId, DocClassId.OBJECT);
        return this;
    }

    /** @return this */
    public DsonDocObject append(String key, DsonValue value) {
        return (DsonDocObject) super.append(key, value);
    }

}