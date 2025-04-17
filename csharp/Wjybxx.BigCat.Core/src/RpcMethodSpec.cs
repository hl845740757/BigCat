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

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// rpc方法描述信息
/// 1.不记录方法参数类型，没有太大的意义
/// 2.该对象为临时对象，不序列化
/// 3.该对象赢得保持简单，以便用户可自行构造
/// </summary>
public struct RpcMethodSpec
{
#nullable disable
    private int serviceId;
    private int methodId;
    private object parameter;
    private bool sharable;

    /// <summary>
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="parameter">方法参数</param>
    /// <param name="sharable">方法参数是否可共享</param>
    public RpcMethodSpec(int serviceId, int methodId, object parameter, bool sharable = false) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameter = parameter;
        this.sharable = sharable;
    }

    public int ServiceId {
        get => serviceId;
        set => serviceId = value;
    }
    public int MethodId {
        get => methodId;
        set => methodId = value;
    }
    public object Parameter {
        get => parameter;
        set => parameter = value;
    }
    public bool Sharable {
        get => sharable;
        set => sharable = value;
    }

    public override string ToString() {
        return $"{nameof(serviceId)}: {serviceId}," +
               $" {nameof(methodId)}: {methodId}," +
               $" {nameof(parameter)}: {parameter}," +
               $" {nameof(sharable)}: {sharable}";
    }
}

/// <summary>
/// rpc方法描述信息
/// 1.不记录方法参数类型，没有太大的意义
/// 2.该对象为临时对象，不序列化
/// 3.该对象赢得保持简单，以便用户可自行构造
///
/// 注意：C#端void函数默认生成的客户端代理泛型参数为object，使得我们可以监听任意
/// </summary>
/// <typeparam name="V">方法的返回值类型，用于编码提示和创建Promise</typeparam>
public struct RpcMethodSpec<V>
{
    private int serviceId;
    private int methodId;
    private object parameter;
    private bool sharable;

    /// <summary>
    /// </summary>
    /// <param name="serviceId">服务id</param>
    /// <param name="methodId">方法id</param>
    /// <param name="parameter">方法参数</param>
    /// <param name="sharable">方法参数是否可共享</param>
    public RpcMethodSpec(int serviceId, int methodId, object parameter, bool sharable = false) {
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameter = parameter;
        this.sharable = sharable;
    }

    public RpcMethodSpec Unwrap() => new RpcMethodSpec(serviceId, methodId, parameter, sharable);

    public int ServiceId {
        get => serviceId;
        set => serviceId = value;
    }
    public int MethodId {
        get => methodId;
        set => methodId = value;
    }
    public object Parameter {
        get => parameter;
        set => parameter = value;
    }
    public bool Sharable {
        get => sharable;
        set => sharable = value;
    }

    public override string ToString() {
        return $"{nameof(serviceId)}: {serviceId}," +
               $" {nameof(methodId)}: {methodId}," +
               $" {nameof(parameter)}: {parameter}," +
               $" {nameof(sharable)}: {sharable}";
    }
}
}