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

import javax.annotation.Nonnull;
import java.util.List;

/**
 * protobuf枚举
 *
 * @author wjybxx
 * date - 2023/10/7
 */
public final class PBEnum extends PBTypeElement {

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.ENUM;
    }

    public List<PBEnumValue> getEnumValueList() {
        return getEnclosedElements().stream()
                .filter(e -> e.getKind() == PBElementKind.ENUM_VALUE)
                .map(e -> (PBEnumValue) e)
                .toList();
    }

}