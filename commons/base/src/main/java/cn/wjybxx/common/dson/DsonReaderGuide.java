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

import cn.wjybxx.common.annotation.Beta;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
@Beta
public enum DsonReaderGuide {

    /** 当前应该读取type */
    READ_TYPE,
    /** 当前应该读取name或fullNumber */
    READ_NAME,
    /** 当前应该根据type决定应该怎样读值 */
    READ_VALUE,

    /** 当前应该读数组 */
    START_ARRAY,
    /** 当前应该结束读数组 */
    END_ARRAY,

    /** 当前应该读Object */
    START_OBJECT,
    /** 当前应该结束读Object */
    END_OBJECT,

    /** 当前应该关闭Reader */
    CLOSE,

}