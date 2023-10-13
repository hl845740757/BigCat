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

import javax.annotation.Nonnull;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Objects;
import java.util.Set;

/**
 * protobuf的rpc服务
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public final class PBService extends PBTypeElement {

    /** 服务id -- 从注解中获得的缓存值 */
    private int serviceId;
    /** 是否生成客户端用proxy -- 建议由parser根据service的名字计算 */
    private boolean genProxy = true;
    /** 是否生成服务端用exporter */
    private boolean genExporter = true;

    /** 生成的Service接口需要继承的接口 -- 建议由parser根据service的名字计算 */
    private final Set<String> superinterfaces = new LinkedHashSet<>();

    // region

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.SERVICE;
    }

    /** @param name 接口全路径 */
    public PBService addSuperinterface(String name) {
        Objects.requireNonNull(name);
        superinterfaces.add(name);
        return this;
    }

    public List<PBMethod> getMethods() {
        return getEnclosedElements().stream()
                .filter(e -> e.getKind() == PBElementKind.METHOD)
                .map(e -> (PBMethod) e)
                .toList();
    }
    // endregion

    public int getServiceId() {
        return serviceId;
    }

    public PBService setServiceId(int serviceId) {
        this.serviceId = serviceId;
        return this;
    }

    public boolean isGenProxy() {
        return genProxy;
    }

    public PBService setGenProxy(boolean genProxy) {
        this.genProxy = genProxy;
        return this;
    }

    public boolean isGenExporter() {
        return genExporter;
    }

    public PBService setGenExporter(boolean genExporter) {
        this.genExporter = genExporter;
        return this;
    }

    public Set<String> getSuperinterfaces() {
        return superinterfaces;
    }
}