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

package cn.wjybxx.common.eventbus;

import java.util.Objects;

/**
 * 扩展主键，用于在支持事件源键派发的EventBus中进行注册和取消
 *
 * @author wjybxx
 * date 2023/4/6
 */
public class MasterKeyX {

    private final Object sourceKey;
    private final Object masterKey;

    public MasterKeyX(Object sourceKey, Object masterKey) {
        this.sourceKey = Objects.requireNonNull(sourceKey);
        this.masterKey = Objects.requireNonNull(masterKey);
    }

    public Object getSourceKey() {
        return sourceKey;
    }

    public Object getMasterKey() {
        return masterKey;
    }

}