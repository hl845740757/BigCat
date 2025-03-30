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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

/**
 * 测试用公共结果
 *
 * @author wjybxx
 * date - 2025/3/14
 */
@DsonSerializable
public class Response {

    private int val;
    private String string;

    public int getVal() {
        return val;
    }

    public Response setVal(int val) {
        this.val = val;
        return this;
    }

    public String getString() {
        return string;
    }

    public Response setString(String string) {
        this.string = string;
        return this;
    }

    @Override
    public String toString() {
        return "Response{" +
                "val=" + val +
                ", string='" + string + '\'' +
                '}';
    }
}