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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// RPC方法注解，定于需要导出的方法
///
/// <h3>限制</h3>
/// 1.方法不能是private - 至少是包级访问权限(让生成的代码可访问) -- 建议用接口定义服务。
/// 2.除去Context参数，方法最多可有1个参数。
/// 3.方法参数和结果不能是基本类型，也不能是List和字典，必须是普通结构(class或struct)。
/// 4.方法如果没有返回值，且引入了Context参数，建议将发现参数声明为object。
///
/// <h3>代理方法的返回值</h3>
/// 1.如果方法的返回值为<see cref="ValueFuture{T}"/>和<see cref="IFuture{T}"/>，则会捕获Future的泛型参数作为返回值类型。
/// 2.如果方法的返回值为void，但第一个参数为<see cref="RpcContext{T}"/>，工具会捕获Context的泛型参数作为返回值类型。
/// 3.其它普通方法，其返回值类型就是代理方法的返回值类型。
///
/// <h3>Context</h3>
/// Context有助于实现复杂的消息交互，允许在返回结果前后向对方发送额外的消息，这在与客户端通信的过程中非常有用。
/// 1. 如果需要Ctx，必须将<see cref="RpcContext{T}"/>定义为方法的第一个参数。
/// 2. Context不会导出给客户端的Proxy。
/// 3. 需要自行管理结果的返回实际时，需要设置<see cref="ManualReturn"/>
/// 4. 关于context的用法可查看测试用例(NodeTest)
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
    /// 它的主要作用是配置切面数据，用于拦截器。比如：发包频率限制等。
    /// </summary>
    [StableName]
    public string? CustomData { get; set; } = null;
}
}