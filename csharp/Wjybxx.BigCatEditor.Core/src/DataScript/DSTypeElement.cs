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

using System.Text;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 类型元素
/// 普通类型、类型变量的共同超类
/// </summary>
public abstract class DSTypeElement : DSElement
{
    /// <summary>
    /// 类型的全限定名
    /// 注意：ns不是c#或java的命名空间，而是文件简单名<see cref="DSFile.SimpleName"/>。
    /// </summary>
    protected readonly TypeName _typeName;

    protected DSTypeElement(string simpleName, TypeName typeName) : base(simpleName) {
        _typeName = typeName;
    }

    /// <summary>
    /// 类型名缓存
    /// 注意：ns不是c#或java的命名空间，而是文件简单名<see cref="DSFile.SimpleName"/>。
    /// </summary>
    public TypeName TypeName => _typeName;

    /// <summary>
    /// 类型的原始定义
    /// </summary>
    public abstract override DSTypeElement OriginDefine { get; }

    /// <summary>
    /// 类型
    /// </summary>
    public abstract DSTypeKind TypeKind { get; }
    /// <summary>
    /// 是否是泛型类
    /// </summary>
    public abstract bool IsGenericType { get; }

    /// <summary>
    /// 是否是引用类型
    /// </summary>
    public bool IsReferenceType => TypeKind == DSTypeKind.Class;
    /// <summary>
    /// 是否是值类型
    /// </summary>
    public bool IsValueType => TypeKind == DSTypeKind.Struct || TypeKind == DSTypeKind.Enum;

    #region equals

    public abstract override bool Equals(object? obj);

    public abstract override int GetHashCode();

    public static bool operator ==(DSTypeElement? left, DSTypeElement? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DSTypeElement? left, DSTypeElement? right) {
        return !Equals(left, right);
    }

    protected override void ToString(StringBuilder sb) {
        sb.Append(", typeName='").Append(_typeName.ReflectionName()).Append('\'');
    }

    #endregion
}
}