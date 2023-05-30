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

package cn.wjybxx.common.rpc;

import cn.wjybxx.common.dson.AutoTypeArgs;
import cn.wjybxx.common.dson.binary.BinarySerializable;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 默认的rpc方法结构体
 * 通过short类型的serviceId和methodId定位被调用方法，可以大大减少数据传输量，而且定位方法更快
 *
 * @author wjybxx
 * date 2023/4/1
 */
@AutoTypeArgs
@BinarySerializable
public class DefaultRpcMethodSpec<V> implements RpcMethodSpec<V> {

    private short serviceId;
    private short methodId;
    private List<Object> methodParams;

    public DefaultRpcMethodSpec() {
        // 用于可能的序列化支持
    }

    public DefaultRpcMethodSpec(short serviceId, short methodId, List<Object> methodParams) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.methodParams = methodParams;
    }

    public short getServiceId() {
        return serviceId;
    }

    public short getMethodId() {
        return methodId;
    }

    public List<Object> getMethodParams() {
        return methodParams;
    }

    public void setServiceId(short serviceId) {
        this.serviceId = serviceId;
    }

    public void setMethodId(short methodId) {
        this.methodId = methodId;
    }

    public void setMethodParams(List<Object> methodParams) {
        this.methodParams = methodParams;
    }

    //

    @Override
    public int getInt(int index) {
        Number number = (Number) methodParams.get(index);
        return number.intValue();
    }

    @Override
    public long getLong(int index) {
        Number number = (Number) methodParams.get(index);
        return number.longValue();
    }

    @Override
    public float getFloat(int index) {
        Number number = (Number) methodParams.get(index);
        return number.floatValue();
    }

    @Override
    public double getDouble(int index) {
        Number number = (Number) methodParams.get(index);
        return number.doubleValue();
    }

    @Override
    public boolean getBoolean(int index) {
        return (Boolean) methodParams.get(index);
    }

    @Override
    public String getString(int index) {
        return (String) methodParams.get(index);
    }

    @Override
    public Object getObject(int index) {
        return methodParams.get(index);
    }

    //

    @Nonnull
    @Override
    public String toSimpleLog() {
        return '{' +
                "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", paramCount=" + methodParams.size() +
                "}";
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return '{' + "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", paramCount=" + methodParams.size() +
                ", params=" + methodParams +
                "}";
    }

    @Override
    public String toString() {
        return "DefaultRpcMethodSpec{"
                + "serviceId=" + serviceId
                + ", methodId=" + methodId
                + ", methodParams=" + methodParams
                + '}';
    }

}