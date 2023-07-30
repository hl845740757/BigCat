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

package cn.wjybxx.common.config;

import cn.wjybxx.common.ex.ParseException;

/**
 * 该异常用于表示用户给定的字符串不是一个合法的数组格式
 *
 * @author wjybxx
 * date 2023/4/16
 */
public class ArrayStringParseException extends ParseException {

    public ArrayStringParseException() {
    }

    public ArrayStringParseException(String message) {
        super(message);
    }

    public ArrayStringParseException(String message, Throwable cause) {
        super(message, cause);
    }

    public ArrayStringParseException(Throwable cause) {
        super(cause);
    }

}