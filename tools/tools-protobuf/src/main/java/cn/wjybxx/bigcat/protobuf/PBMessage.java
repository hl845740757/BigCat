/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.protobuf;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * protobuf消息
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public final class PBMessage extends PBTypeElement {

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.MESSAGE;
    }

    public List<PBField> getFieldS() {
        return getEnclosedElements().stream()
                .filter(e -> e.getKind() == PBElementKind.FIELD)
                .map(e -> (PBField) e)
                .toList();
    }

    public List<PBMessage> getEnclosedMessages() {
        return getEnclosedElements().stream()
                .filter(e -> e.getKind() == PBElementKind.MESSAGE)
                .map(e -> (PBMessage) e)
                .toList();
    }

}