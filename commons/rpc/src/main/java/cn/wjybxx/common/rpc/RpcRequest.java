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


import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.FieldImpl;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.log.DebugLogFriendlyObject;
import cn.wjybxx.dson.DsonType;
import org.apache.commons.codec.binary.Hex;

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.List;

/**
 * rpc请求结构体
 *
 * @author wjybxx
 * date 2023/4/1
 */
@AutoSchema
@BinarySerializable
public final class RpcRequest extends RpcProtocol implements DebugLogFriendlyObject {

    /** 请求id */
    private long requestId;
    /** 调用类型 */
    private int invokeType;

    /** 服务id */
    private int serviceId;
    /** 方法id */
    private int methodId;
    /**
     * 方法参数
     * 1.正确设值的情况下不为null，为{@link byte[]}或{@link List}
     * 2.如果为bytes，表示已经序列化
     * 3.如果为List，表示尚未序列化；支持无参和单参数为null的情况
     */
    @FieldImpl(writeProxy = "writeParameters", readProxy = "readParameters")
    private Object parameters;

    /** 方法参数是否可共享 -- 详见{@link RpcMethodSpec} */
    private transient boolean sharable;

    public RpcRequest() {
        // 可能的序列化支持
    }

    public RpcRequest(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        super(conId, srcAddr, destAddr);
    }

    public RpcRequest(long conId, RpcAddr srcAddr, RpcAddr destAddr,
                      long requestId, int invokeType, RpcMethodSpec<?> methodSpec) {
        super(conId, srcAddr, destAddr);
        this.requestId = requestId;
        this.invokeType = invokeType;
        this.serviceId = methodSpec.getServiceId();
        this.methodId = methodSpec.getMethodId();
        this.parameters = methodSpec.getParameters();
        this.sharable = methodSpec.isSharable();
    }

    // region 业务方法

    /** 是否已序列化 */
    public boolean isSerialized() {
        return parameters instanceof byte[];
    }

    /** 是否已反序列化 */
    public boolean isDeserialized() {
        return parameters instanceof List<?>;
    }

    /** 参数转bytes */
    public byte[] bytesParameters() {
        assert parameters != null;
        return (byte[]) parameters;
    }

    /** 参数转List */
    @SuppressWarnings("unchecked")
    public List<Object> listParameters() {
        assert parameters != null;
        return (List<Object>) parameters;
    }

    // endregion

    // region getter/setter

    public long getRequestId() {
        return requestId;
    }

    public RpcRequest setRequestId(long requestId) {
        this.requestId = requestId;
        return this;
    }

    public int getInvokeType() {
        return invokeType;
    }

    public RpcRequest setInvokeType(int invokeType) {
        this.invokeType = invokeType;
        return this;
    }

    public int getServiceId() {
        return serviceId;
    }

    public RpcRequest setServiceId(int serviceId) {
        this.serviceId = serviceId;
        return this;
    }

    public int getMethodId() {
        return methodId;
    }

    public RpcRequest setMethodId(int methodId) {
        this.methodId = methodId;
        return this;
    }

    public Object getParameters() {
        return parameters;
    }

    public RpcRequest setParameters(Object parameters) {
        this.parameters = parameters;
        return this;
    }

    public boolean isSharable() {
        return sharable;
    }

    public RpcRequest setSharable(boolean sharable) {
        this.sharable = sharable;
        return this;
    }
    // endregion

    @Nonnull
    @Override
    public String toSimpleLog() {
        return "{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }

    @Nonnull
    @Override
    public String toDetailLog() {
        return toString();
    }

    @Override
    public String toString() {
        return "RpcRequest{" +
                "requestId=" + requestId +
                ", invokeType=" + invokeType +
                ", serviceId=" + serviceId +
                ", methodId=" + methodId +
                ", parameters=" + parametersString() +
                ", conId=" + conId +
                ", srcAddr=" + srcAddr +
                ", destAddr=" + destAddr +
                '}';
    }

    private String parametersString() {
        Object parameters = this.parameters;
        if (parameters == null) {
            return "null";
        }
        if (parameters.getClass() == byte[].class) {
            return Hex.encodeHexString((byte[]) parameters);
        }
        // List类型
        return parameters.toString();
    }

    // region 序列化优化
    // 1.自动处理延迟序列化问题
    // 2.避免多态写入List类型信息

    public void writeParameters(BinaryObjectWriter writer, int name) {
        if (parameters == null) {
            throw new IllegalStateException("parameters is null");
        }
        if (parameters instanceof byte[] bytes) {
            writer.writeValueBytes(name, DsonType.ARRAY, bytes);
        } else {
            List<Object> parameters = listParameters();
            writer.writeStartArray(name, getListTypeArgInfo(parameters));
            for (Object ele : parameters) {
                writer.writeObject(0, ele);
            }
            writer.writeEndArray();
        }
    }

    public void readParameters(BinaryObjectReader reader, int name) {
        List<Object> parameters = new ArrayList<>(1);
        reader.readStartArray(name, TypeArgInfo.ARRAYLIST);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            parameters.add(reader.readObject(0));
        }
        reader.readEndArray();
        this.parameters = parameters;
    }

    static TypeArgInfo<?> getListTypeArgInfo(List<Object> list) {
        return list.getClass() == ArrayList.class ? TypeArgInfo.ARRAYLIST : TypeArgInfo.of(list.getClass());
    }
    // endregion

}