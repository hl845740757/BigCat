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

import java.util.ArrayList;
import java.util.List;

/**
 * 测试用公共请求结构
 *
 * @author wjybxx
 * date - 2025/3/14
 */
@DsonSerializable
public class Request {

    private int val1;
    private int val2;
    private String string1;
    private String string2;
    private List<String> stringList = new ArrayList<>();

    public static Request ofString(String val) {
        return new Request().setString1(val);
    }

    public static Request ofInt(int val) {
        return new Request().setVal1(val);
    }

    // region getter/setter

    public int getVal1() {
        return val1;
    }

    public Request setVal1(int val1) {
        this.val1 = val1;
        return this;
    }

    public int getVal2() {
        return val2;
    }

    public Request setVal2(int val2) {
        this.val2 = val2;
        return this;
    }

    public String getString1() {
        return string1;
    }

    public Request setString1(String string1) {
        this.string1 = string1;
        return this;
    }

    public String getString2() {
        return string2;
    }

    public Request setString2(String string2) {
        this.string2 = string2;
        return this;
    }

    public List<String> getStringList() {
        return stringList;
    }

    public Request setStringList(List<String> stringList) {
        this.stringList = stringList;
        return this;
    }
    // endregion


    @Override
    public String toString() {
        return "Request{" +
                "val1=" + val1 +
                ", val2=" + val2 +
                ", string1='" + string1 + '\'' +
                ", string2='" + string2 + '\'' +
                ", stringList=" + stringList +
                '}';
    }
}