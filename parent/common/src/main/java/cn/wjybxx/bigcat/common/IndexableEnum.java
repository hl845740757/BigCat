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

package cn.wjybxx.bigcat.common;

/**
 * 可索引的枚举，要求项目中的所有可序列化的和可入库的枚举必须实现该接口。
 *
 * <h>稳定性</h>
 * 相对于{@link Enum#ordinal()}和{@link Enum#name()}，我们自定义的{@link #getNumber()}会更加稳定。<br>
 * 因此，在序列化和持久化时，都使用{@link #getNumber()}，因此我们需要尽可能的保持{@link #getNumber()}的稳定性。<br>
 *
 * @author wjybxx
 * date 2023/3/31
 */
public interface IndexableEnum {

    int getNumber();

}