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


import cn.wjybxx.common.ExtensibleObject;

import javax.annotation.Nonnull;
import java.util.Map;

/**
 * 如果一个rpc服务实现了该接口，将自动导出这两个扩展方法
 *
 * @author wjybxx
 * date 2023/4/1
 */
@SuppressWarnings("unused")
public interface ExtensibleService extends ExtensibleObject {

    @Nonnull
    @Override
    @RpcMethod(methodId = 9998)
    Map<String, Object> getExtBlackboard();

    @Override
    @RpcMethod(methodId = 9999)
    Object execute(@Nonnull String cmd, Object params) throws Exception;

}