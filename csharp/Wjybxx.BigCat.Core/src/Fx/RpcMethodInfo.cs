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
using Google.Protobuf;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// RPC方法信息
/// (不定义为泛型，避免额外的复杂度，parser直接强转)
/// </summary>
public sealed class RpcMethodInfo : IEquatable<RpcMethodInfo>
{
    /** 服务名 -- 本地debug用，不参与equals比较 */
    public readonly string serviceName;
    /** 方法名 -- 本地debug用 */
    public readonly string methodName;

#nullable disable
    /** 服务id */
    public readonly int serviceId;
    /** 方法id */
    public readonly int methodId;
    /** 方法参数类型 -- 无参数时为null */
    public readonly Type parameterType;
    /** 方法结果类型 -- 无结果时为null */
    public readonly Type resultType;
#nullable enable
    // pb特殊支持
    /** 不为null则表示参数为pb类型，不参与equals比较 */
    public readonly MessageParser? parameterParser;
    /** 不为null则表示结果为pb类型 */
    public readonly MessageParser? resultParser;

    public RpcMethodInfo(string serviceName, string methodName,
                         int serviceId, int methodId,
                         Type? parameterType, Type? resultType) {
        this.serviceName = serviceName;
        this.methodName = methodName;
        this.serviceId = serviceId;
        this.methodId = methodId;
        this.parameterType = VoidToNull(parameterType);
        this.resultType = VoidToNull(resultType);

        this.parameterParser = FindParser(this.parameterType);
        this.resultParser = FindParser(this.resultType);
    }

    #region util

    private static Type? VoidToNull(Type? clazz) {
        if (clazz == null || clazz == typeof(VoidClass) || clazz == typeof(void)) {
            return null;
        }
        return clazz;
    }

    private static MessageParser? FindParser(Type? clazz) {
        if (clazz == null) {
            return null;
        }
        if (!typeof(IMessage).IsAssignableFrom(clazz)) {
            return null;
        }
        return ProtobufUtils.FindParser(clazz);
    }

    #endregion

    #region equals

    public bool Equals(RpcMethodInfo? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return serviceId == other.serviceId
               && methodId == other.methodId
               && parameterType == other.parameterType
               && resultType == other.resultType;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is RpcMethodInfo other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = serviceId;
        hashCode = (hashCode * 397) ^ methodId;
        hashCode = (hashCode * 397) ^ (parameterType != null ? parameterType.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (resultType != null ? resultType.GetHashCode() : 0);
        return hashCode;
    }

    public static bool operator ==(RpcMethodInfo? left, RpcMethodInfo? right) {
        return Equals(left, right);
    }

    public static bool operator !=(RpcMethodInfo? left, RpcMethodInfo? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(serviceName)}: {serviceName}," +
               $" {nameof(methodName)}: {methodName}," +
               $" {nameof(serviceId)}: {serviceId}," +
               $" {nameof(methodId)}: {methodId}," +
               $" {nameof(parameterType)}: {parameterType}," +
               $" {nameof(resultType)}: {resultType}";
    }

    #endregion
}
}