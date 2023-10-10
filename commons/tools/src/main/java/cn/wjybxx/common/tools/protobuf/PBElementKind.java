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

/**
 * pb元素类型
 *
 * @author wjybxx
 * date - 2023/10/9
 */
public enum PBElementKind {

    /** 文件（包） */
    FILE,

    /** 服务类 */
    SERVICE,

    /** 消息类 */
    MESSAGE,

    /** 枚举类 */
    ENUM,

    /** RPC方法 */
    METHOD,

    /** 字段 */
    FIELD,

    /** 枚举值 */
    ENUM_VALUE,
}
