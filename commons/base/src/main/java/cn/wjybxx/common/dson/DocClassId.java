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

import org.apache.commons.lang3.StringUtils;

import java.util.Objects;

/**
 * 在文档型编解码中，可读性是比较重要的，因此字符串来映射类型
 *
 * @author wjybxx
 * date - 2023/4/21
 */
public final class DocClassId implements ClassId {

    public static final DocClassId OBJECT = new DocClassId((String) null);

    private final String value;

    public DocClassId(String value) {
        this.value = value;
    }

    public String getValue() {
        return value;
    }

    public boolean isObjectClassId() {
        return this == OBJECT || StringUtils.isBlank(value);
    }

    //
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DocClassId that = (DocClassId) o;

        return Objects.equals(value, that.value);
    }

    @Override
    public int hashCode() {
        return value != null ? value.hashCode() : 0;
    }

    @Override
    public String toString() {
        return "DocClassId{" +
                "value='" + value + '\'' +
                '}';
    }

    //
    public static DocClassId of(String clsName) {
        return StringUtils.isBlank(clsName) ? OBJECT : new DocClassId(clsName);
    }

}