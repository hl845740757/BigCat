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

import cn.wjybxx.common.annotation.Beta;
import cn.wjybxx.common.ex.ParseException;

/**
 * 该异常表示给定的字符串值无效
 * 注意：仅仅表示<b>字符串值</b>格式异常
 *
 * @author wjybxx
 * date 2023/4/16
 */
@Beta
public class JsonStringParseException extends ParseException {

    public JsonStringParseException() {
    }

    public JsonStringParseException(String message) {
        super(message);
    }

    public JsonStringParseException(String message, Throwable cause) {
        super(message, cause);
    }

    public JsonStringParseException(Throwable cause) {
        super(cause);
    }

}