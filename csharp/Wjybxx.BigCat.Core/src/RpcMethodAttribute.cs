#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// RPC方法注解
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class RpcMethodAttribute : Attribute
{
    /// <summary>
    /// 该方法在该类中的唯一id
    ///
    /// 注意：
    /// 1.取值范围为闭区间[1, 9999]。
    /// 2.由该id和serviceId构成唯一索引。
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// 方法参数是否可共享
    /// 当方法参数可共享时，序列化会延迟到IO线程 —— 理论上可做到进程内rpc不序列化。
    ///
    /// 1.该属性用于配置默认值，减少实现者调用<see cref="RpcMethodSpec.Sharable"/>设置共享属性。
    /// 2.主要用于避免本地Rpc调用时的序列化过程
    /// </summary>
    public bool ArgSharable { get; set; } = false;

    /// <summary>
    /// 方法返回值是否可共享
    /// 当返回值可共享时，序列化会延迟到IO线程
    ///
    /// 1.该属性用于配置默认值，减少实现者调用<see cref="RpcContext{T}.IsSharable"/>
    /// 2.主要用于避免本地Rpc调用时的序列化过程
    /// </summary>
    public bool ResultSharable { get; set; } = false;

    /// <summary>
    /// 是否由用户手动返回结果
    ///
    /// 1.该属性用于配置默认值，减少实现者调用<see cref="RpcContext{T}.IsManualReturn"/>
    /// 2.如果用户手动返回结果，方法参数第一个必须是<see cref="RpcContext{T}"/>，且应当使用in修饰
    /// 3.方法的直接返回值为<code>void</code>时才需要设置
    /// </summary>
    public bool ManualReturn { get; set; } = false;

    /// <summary>
    /// 自定义扩展数据，通常是json或dson格式。
    /// 它的主要作用是配置切面数据，用于拦截器。比如：某些消息只能在玩家在场景的时候处理。
    /// </summary>
    [StableName]
    public string? CustomData { get; set; } = null;
}
}