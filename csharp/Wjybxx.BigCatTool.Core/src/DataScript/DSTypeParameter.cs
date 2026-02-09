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

using System.Reflection;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 类型的泛型参数
///
/// <h3>泛型约束</h3>
/// 泛型约束支持new、struct、class三个约束，对应的枚举值为
/// <see cref="GenericParameterAttributes.DefaultConstructorConstraint"/>
/// <see cref="GenericParameterAttributes.NotNullableValueTypeConstraint"/>、
/// <see cref="GenericParameterAttributes.ReferenceTypeConstraint"/>
///
/// TODO: 相等性测试未测试定义该类型的类 -- 可能需要记录定义该泛型变量的类型符号（类型or函数）
/// </summary>
public sealed class DSTypeParameter : DSTypeElement
{
    /// <summary>
    /// 泛型变量约束
    /// </summary>
    private readonly TypeParameterConstraints constraints;

    public DSTypeParameter(string name, TypeParameterConstraints constraints)
        : base(name, TypeParameterName.Get(name)) {
        this.constraints = constraints;
    }

    public DSTypeParameter(DSTypeParameter originDefine)
        : base(originDefine.Name, originDefine.TypeName) {
        this.constraints = originDefine.constraints;
    }

    public override DSElementKind Kind => DSElementKind.TypeParameter;
    public override DSTypeKind TypeKind => DSTypeKind.TypeParameter;
    public override bool IsGenericType => false;
    public override DSElement OriginDefine => this;
    public new TypeParameterName TypeName => (TypeParameterName)typeName;

    /// <summary>
    /// 泛型变量约束
    /// </summary>
    public TypeParameterConstraints Constraints => constraints;
    /// <summary>
    /// 泛型变量是否约束为值类型
    /// </summary>
    public bool HasValueTypeConstraint => constraints.HasFlag(TypeParameterConstraints.ValueTypeConstraint);
    /// <summary>
    /// 泛型变量是否约束为引用类型
    /// </summary>
    public bool HasReferenceTypeConstraint => constraints.HasFlag(TypeParameterConstraints.ReferenceTypeConstraint);
    /// <summary>
    /// 泛型变量是否约束为必须包含空构造函数
    /// </summary>
    public bool HasDefaultConstructorConstraint => constraints.HasFlag(TypeParameterConstraints.DefaultConstructorConstraint);

    #region equals

    private bool Equals(DSTypeParameter other) {
        return typeName.Equals(other.TypeName)
               && constraints == other.constraints;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is DSTypeParameter other && Equals(other);
    }

    public override int GetHashCode() {
        return (typeName.GetHashCode() * 397) ^ (int)constraints;
    }

    #endregion
}
}