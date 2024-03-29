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

import java.util.List;
import java.util.Objects;

/**
 * 服务信息
 *
 * @author wjybxx
 * date - 2023/10/4
 */
public final class ServiceInfo {

    public final int serviceId;
    public final List<Worker> workerList;

    /**
     * @param serviceId  服务id
     * @param workerList 支持该服务的Worker
     */
    public ServiceInfo(int serviceId, List<Worker> workerList) {
        this.serviceId = serviceId;
        this.workerList = Objects.requireNonNull(workerList);
    }

    public ServiceInfo toImmutable() {
        return new ServiceInfo(serviceId, List.copyOf(workerList));
    }

    public ServiceInfo addWorker(Worker worker) {
        Objects.requireNonNull(worker);
        workerList.add(worker);
        return this;
    }
}