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


import cn.wjybxx.base.annotation.StableName;

/**
 * rpc方法描述信息
 * 1.不记录方法参数类型，没有太大的意义
 * 2.该对象为临时对象，不序列化
 * 3.该对象赢得保持简单，以便用户可自行构造
 *
 * @param <V> 用于捕获返回值类型
 * @author wjybxx
 * date 2023/4/1
 */
@SuppressWarnings("unused")
public final class RpcMethodSpec<V> {

    private int serviceId;
    private int methodId;
    private Object parameter;
    private boolean sharable;

    public RpcMethodSpec(int serviceId, int methodId, Object parameter) {
        this(serviceId, methodId, parameter, false);
    }

    /**
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param parameter 方法参数
     * @param sharable  方法参数是否可共享
     */
    @StableName(comment = "生成的代码调用")
    public RpcMethodSpec(int serviceId, int methodId, Object parameter, boolean sharable) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameter = parameter;
        this.sharable = sharable;
    }

    /**
     * 该接口主要开放给生成的代码，用于池化{@link RpcMethodSpec}
     *
     * @param serviceId 服务id
     * @param methodId  方法id
     * @param parameter 方法参数
     * @param sharable  方法参数是否可共享
     */
    @StableName(comment = "生成的代码调用")
    public void init(int serviceId, int methodId, Object parameter, boolean sharable) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameter = parameter;
        this.sharable = sharable;
    }

    // region getter

    public int getServiceId() {
        return serviceId;
    }

    public void setServiceId(int serviceId) {
        this.serviceId = serviceId;
    }

    public int getMethodId() {
        return methodId;
    }

    public void setMethodId(int methodId) {
        this.methodId = methodId;
    }

    public Object getParameter() {
        return parameter;
    }

    public void setParameter(Object parameter) {
        this.parameter = parameter;
    }

    public boolean isSharable() {
        return sharable;
    }

    public void setSharable(boolean sharable) {
        this.sharable = sharable;
    }

    // endregion

    @Override
    public String toString() {
        return "RpcMethodSpec{" +
                "serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameter=" + parameter +
                ", sharable=" + sharable +
                '}';
    }
}