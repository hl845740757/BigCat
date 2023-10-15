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

package cn.wjybxx.common;

import cn.wjybxx.common.annotation.StableName;

/**
 * 轻量枚举
 * 相对于{@link Enum#ordinal()}和{@link Enum#name()}，我们自定义的{@link #getNumber()}会更加稳定。
 * 因此，在序列化和持久化时，都使用{@link #getNumber()}。
 *
 * @author wjybxx
 * date 2023/3/31
 */
public interface EnumLite {

    @StableName
    int getNumber();

}