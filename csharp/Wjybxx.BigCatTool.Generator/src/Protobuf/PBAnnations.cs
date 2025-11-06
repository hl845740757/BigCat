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

namespace Wjybxx.BigCatTool.Generator.Protobuf
{
/// <summary>
/// Protobuf工具使用到的注解
/// </summary>
public static class PBAnnations
{
    /// <summary>
    /// 服务和方法的Rpc选项
    ///
    /// <h3>用于类型时</h3>
    /// <code>//@Rpc {id: 1, async: true, ctx: true, manual: true}</code>
    /// - id表示为服务分配的id
    /// - async 表示服务端接口是否为异步模式；默认值为false
    /// - ctx 表示是否需要RpcContext参数；默认值为false
    /// - manual 表示是否手动管理返回时机，默认值为false; 如果为true，应当声明tx。
    ///
    /// 注：服务上的async等参数为参数的模式值，避免每个方法重复配置。
    ///
    /// <h3>用于方法时</h3>
    /// <code>//@Rpc {async: true, ctx: true, manual: true}</code>
    /// - async 表示服务端接口是否为异步模式；默认值为false
    /// - ctx 表示是否需要RpcContext参数；默认值为false
    /// - manual 表示是否手动管理返回时机，默认值为false; 如果为true，应当声明tx。
    ///
    /// 注：方法上的async等属性用于覆盖service上的默认值。
    /// </summary>
    public const string RPC = "Rpc";
    /// <summary>
    /// RPC切面数据，与具体的应用相关
    /// </summary>
    public const string RPC_CUSTOM = "RpcCustom";
}
}