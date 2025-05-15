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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Rpc服务注解
/// (继承无效)
///
/// 1.虽然C#的接口命名规范推荐接口以字母I开头，但Rpc相关的服务要避免应当避免I字符开头。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class RpcServiceAttribute : Attribute
{
    /// <summary>
    /// 服务id
    /// 
    /// 1.serviceId 小于 0，则表示本地服务
    /// 2.serviceId 大于 0 表示公共服务，建议1表示连接管理服务
    /// 3.在与客户端通信的服务中，取值范围为 [-32767, 32767]，即2字节内，且可以转正值
    /// 4.serviceId 要好好规划，合理的serviceId分配有助于拦截器测试上下文
    /// </summary>
    /// <returns></returns>
    public int ServiceId { get; set; }

    /// <summary>
    /// 自定义扩展数据，通常是json或dson格式。
    /// 它的主要作用是配置切面数据，用于拦截器。比如：某些消息只能在玩家在场景的时候处理。
    /// </summary>
    [StableName]
    public string? CustomData { get; set; } = null;

    /// <summary>
    /// 是否生成服务端用的<code>Exporter</code>
    /// </summary>
    public bool GenExporter { get; set; } = true;
    /// <summary>
    /// 是否生成客户端用的<code>Proxy</code>
    /// </summary>
    public bool GenProxy { get; set; } = true;
}
}