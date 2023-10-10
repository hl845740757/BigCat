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

/**
 * 我们支持的元注解类型
 *
 * @author wjybxx
 * date - 2023/10/9
 */
public class AnnotationTypes {

    /** rpc服务 */
    public static final String SERVICE = "RpcService";
    /** rpc方法 */
    public static final String METHOD = "RpcMethod";

    /** 服务端切面参数 */
    public static final String SPARAM = "Sparam";
    /** 客户端切面参数 */
    public static final String CPARAM = "Cparam";

}