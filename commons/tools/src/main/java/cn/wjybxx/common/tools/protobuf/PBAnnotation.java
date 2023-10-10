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

package cn.wjybxx.common.tools.protobuf;

import cn.wjybxx.dson.DsonObject;
import cn.wjybxx.dson.Dsons;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/10/9
 */
public class PBAnnotation {

    /** 注解类型 */
    public final String type;
    /** 注解值 -- dson格式 */
    public final String value;

    private transient DsonObject<String> dsonValue;

    public PBAnnotation(String type, String value) {
        this.type = Objects.requireNonNull(type);
        this.value = Objects.requireNonNull(value);
    }

    public DsonObject<String> getDsonValue() {
        if (dsonValue == null) {
            dsonValue = Dsons.fromJson(value).asObject();
        }
        return dsonValue;
    }

    public String getType() {
        return type;
    }

    public String getValue() {
        return value;
    }

    @Override
    public String toString() {
        return "PBAnnotation{" +
                "type='" + type + '\'' +
                ", value='" + value + '\'' +
                '}';
    }
}