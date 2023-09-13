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

/**
 * Rpc远程调用地址。
 * 1. 地址应当是一个稳定值，就像主机的hostname -- 对于游戏服务器，通常是ServerType + ServerId。
 * 2. 建议使用静态方法代替构造函数，提供更友好的API -- 或提供Utils类。
 * 3. 必须实现 {@link #equals(Object)}和{@link #hashCode()}，建议实现{@link #toString()}方法。
 * 4. 建议实现为不可变对象，如果不是不可变对象，不建议复用。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcAddr {

}