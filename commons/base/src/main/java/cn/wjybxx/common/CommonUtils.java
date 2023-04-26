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

package cn.wjybxx.common;

/**
 * 无处安放的工具方法放在这里
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class CommonUtils {

    /**
     * 如果给定的参数存在（不为null），则返回参数本身，否则返回给定的默认值
     * {@link java.util.Objects#requireNonNullElse(Object, Object)}不允许def为null
     */
    public static <V> V presentOrElse(V obj, V def) {
        return obj == null ? def : obj;
    }
}