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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using TypeName = Wjybxx.Commons.Poet.TypeName;
using Range = Wjybxx.BigCatTool.Core.Range;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 类型元素：class、struct、enum
///
/// <h3>命名空间</h3>
/// 注意，命名空间是编程语言的概念，而数据脚本是没有命名空间概念的。
/// 在脚本解析过程中，<see cref="TypeName"/>上的ns是文件简单名，而不是编程语言概念的命名空间。
/// 
/// <h3>关于继承</h3>
/// 1.我们只记录显式声明的超类，未显式声明的情况下，基类为null。
/// 2.只有<see cref="DSTypeKind.Class"/>可以有显式超类，值类型和枚举我们不做处理。
/// 3.继承是延迟解析的。
/// 
/// <h3>关于泛型</h3>
/// 0.建议尽量少使用泛型。
/// 1.泛型类的超类如果是泛型，则是已构造泛型 -- 继承传递给超类的参数存储在实参列表。
/// 2.只有直接使用泛型定义类型，获得的才是泛型定义类。
/// 3.内部类的泛型变量是拷贝的，和外部类无关的。
/// 4.泛型支持指定值类型或引用类型，也支持指定包含默认构造函数。
/// 5.泛型不支持指定上界（边界），泛型参数上界对于数据存储来说不是必须的，但不支持可大幅降低复杂度。
/// 6.避免泛型类和非泛型类使用相同的简单名 -- 做此支持会导致较大的复杂度，也会影响脚本的跨语言性。
///
/// <h3>关于内部类</h3>
/// 0.尽量避免使用内部类，因为为内部类生成c#类型是比较麻烦的；如果内部类仅用于为C#类型提供编辑器支持，那么是可以的。
/// 1.避免嵌套超过2层。
///
/// <h3>关于语法</h3>
/// 不要在'{'和'}'所在行声明字段等，内容必须和开始和结束Token字符分离 -- 降低解析难度。
///
/// <h3>关于readonly字段</h3>
/// 工具在生成csharp或其它语言代码时，应当响应字段的readonly诉求，在构造函数中解码字段。
/// </summary>
public sealed class DSNamedType : DSTypeElement
{
#nullable disable
    /// <summary>
    /// 元素类型
    /// </summary>
    private readonly DSElementKind _elementKind;
    /// <summary>
    /// 类型的类型...
    /// </summary>
    private readonly DSTypeKind _typeKind;
    /// <summary>
    /// 基类的名字 -- 类型名是解析文本时解析的
    /// (这里的基类名字可能尚不包含命名空间，延迟解析时需要根据Type查询命名空间)
    /// (已去除空白字符)
    /// </summary>
    private readonly string? _baseTypeSymbol;
    /// <summary>
    /// 基类的类型引用 -- 类型是延迟解析的
    /// </summary>
    private DSNamedType? _baseType;
    /// <summary>
    /// 类型的全名 -- 缓存值，不包含泛型参数
    /// </summary>
    private readonly string _fullName;

    /// <summary>
    /// 泛型形参列表，只有泛型定义类有值。
    ///（包含从外部类拷贝来的）
    /// </summary>
    private readonly ImmutableList<DSTypeParameter> _typeParameters;
    /// <summary>
    /// 泛型实参列表，子类传递给超类的参数在这里。
    /// （包含从外部类拷贝来的）
    /// </summary>
    private readonly ImmutableList<DSTypeElement> _typeArguments;
    /// <summary>
    /// 泛型类的原始定义类
    /// 当构造泛型时，保留指向的原型；
    /// </summary>
    private readonly DSNamedType? _originDefine;
#nullable enable

    /// <summary>
    /// 保留字段编号
    /// </summary>
    private readonly List<Range> reservedNumbers = new();
    /// <summary>
    /// 保留字段名
    /// </summary>
    private readonly List<string> reservedNames = new();

    /// <summary>
    /// 适用非泛型类和泛型定义类
    /// </summary>
    /// <param name="elementKind"></param>
    /// <param name="typeKind"></param>
    /// <param name="className">类型名缓存</param>
    /// <param name="typeParameters">泛型变量</param>
    /// <param name="baseTypeSymbol">基类类型符号</param>
    private DSNamedType(DSElementKind elementKind, DSTypeKind typeKind,
                        ClassName className,
                        IList<DSTypeParameter> typeParameters,
                        string? baseTypeSymbol)
        : base(className.simpleName, className) {
        _elementKind = elementKind;
        _typeKind = typeKind;
        _baseTypeSymbol = baseTypeSymbol;
        _typeParameters = typeParameters.ToImmutableList2();
        _typeArguments = ImmutableList<DSTypeElement>.Empty;
        _originDefine = null;
        _fullName = DSUtil.GetCanonicalName(className);
    }

    /** 用于构造泛型 -- 内部元素的处理由外部克隆再添加 */
    internal DSNamedType(DSNamedType originDefine, ClassName className, List<DSTypeElement> typeArguments)
        : base(className.simpleName, className) {
        _originDefine = originDefine;
        _baseTypeSymbol = null;
        _fullName = originDefine._fullName;

        _elementKind = originDefine._elementKind;
        _typeKind = originDefine._typeKind;
        _typeParameters = ImmutableList<DSTypeParameter>.Empty;
        _typeArguments = typeArguments.ToImmutableList2();
    }

    #region core

    public override DSElementKind Kind => _elementKind;
    public override DSTypeKind TypeKind => _typeKind;
    /// <summary>
    /// 类型名缓存
    /// 注意：ns不是c#或java的命名空间，而是文件简单名<see cref="DSFile.SimpleName"/>。
    /// </summary>
    public new ClassName TypeName => (ClassName)typeName;
    /// <summary>
    /// 类型的全限定名(不包含泛型信息，不支持泛型类和非泛型类重名)
    /// 
    /// <code>FileName.A.B.C.D</code>
    /// </summary>
    /// <value></value>
    public string FullName => _fullName;

    /// <summary>
    /// 是否是泛型类
    /// </summary>
    public override bool IsGenericType => _typeParameters.Count > 0 || _typeArguments.Count > 0;
    /// <summary>
    /// 获取类型的原始定义：
    /// 如果当前是泛型类，则返回泛型类定义；否则返回自身；用于获取类型的原始注解等数据。
    /// </summary>
    public override DSNamedType OriginDefine => _originDefine != null ? _originDefine : this;

    /// <summary>
    /// 是否是泛型定义类
    /// </summary>
    public bool IsGenericTypeDefinition => _typeParameters.Count > 0;
    /// <summary>
    /// 是否是已构造泛型(可能仍包含泛型变量，来自持有类型字段的类，或是子类)
    /// </summary>
    public bool IsConstructedGenericType => _typeArguments.Count > 0;

    /// <summary>
    /// 是否是可空值类型
    /// (有大量特殊逻辑)
    /// </summary>
    public bool IsNullableType => IsValueType && SimpleName == DSKeywords.TYPE_NULLABLE;

    /// <summary>
    /// 当前类型声明的泛型变量
    /// (只有原始定义类可访问)
    /// </summary>
    public IList<DSTypeParameter> DeclaredTypeParameters {
        get {
            IList<TypeName> declaredTypeArguments = TypeName.declaredTypeArguments;
            if (declaredTypeArguments.Count == 0) {
                return ImmutableList<DSTypeParameter>.Empty;
            }
            List<DSTypeParameter> r = new List<DSTypeParameter>(TypeParameters);
            r.RemoveRange(0, TypeName.typeArguments.Count - declaredTypeArguments.Count);
            return r;
        }
    }

    #endregion

    #region logic

    /// <summary>
    /// 获取指定Name的字段
    /// </summary>
    /// <param name="name"></param>
    /// <param name="flatInherit"></param>
    /// <returns></returns>
    public DSField? GetField(string name, bool flatInherit = true) {
        foreach (var element in EnclosedElements) {
            if (element.Kind == DSElementKind.Field && element.SimpleName == name) {
                return (DSField?)element;
            }
        }
        if (flatInherit && _baseType != null) {
            return _baseType.GetField(name);
        }
        return null;
    }

    /// <summary>
    /// 获取所有的字段，默认超类字段在前
    /// </summary>
    /// <param name="flatInherit">是否拉取继承的字段，默认true</param>
    /// <returns></returns>
    public List<DSField> GetFields(bool flatInherit = true) {
        if (!flatInherit) {
            return EnclosedElements.Where(e => e.Kind == DSElementKind.Field)
                .Cast<DSField>()
                .ToList();
        }
        List<DSField> result = new List<DSField>();
        foreach (DSNamedType typeElement in DSUtil.FlatInherit(this)) {
            foreach (DSElement element in typeElement.EnclosedElements) {
                if (element.Kind == DSElementKind.Field) {
                    result.Add((DSField)element);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 获取所有的枚举值
    /// </summary>
    /// <returns></returns>
    public List<DSEnumValue> GetEnumValues() {
        return EnclosedElements.Where(e => e.Kind == DSElementKind.EnumValue)
            .Cast<DSEnumValue>()
            .ToList();
    }

    /// <summary>
    /// 根据枚举名查询对应的枚举值
    /// </summary>
    /// <param name="name">枚举名</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    public DSEnumValue? GetEnumValue(string name, bool ignoreCase = false) {
        foreach (DSElement enclosedElement in EnclosedElements) {
            if (enclosedElement.Kind != DSElementKind.EnumValue) {
                continue;
            }
            bool match = ignoreCase
                ? string.Equals(enclosedElement.SimpleName, name, StringComparison.OrdinalIgnoreCase)
                : string.Equals(enclosedElement.SimpleName, name);
            if (match) {
                return enclosedElement as DSEnumValue;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取定义类型的文件，内建类型返回对应的虚拟文件
    /// </summary>
    /// <returns></returns>
    public DSFile GetEnclosingFile() {
        DSElement enclosing = OriginDefine.EnclosingElement;
        while (enclosing.Kind != DSElementKind.File) {
            enclosing = enclosing.EnclosingElement;
        }
        return (DSFile)enclosing;
    }

    #endregion

    #region reversed

    /// <summary>
    /// 添加保留的字段编号
    /// </summary>
    /// <param name="number"></param>
    public void AddReservedNumber(int number) {
        reservedNumbers.Add(new Range(number, number));
    }

    /// <summary>
    /// 添加保留字段编号区间
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public void AddReservedNumber(int start, int end) {
        reservedNumbers.Add(new Range(start, end));
    }

    /// <summary>
    /// 添加保留字段名
    /// </summary>
    /// <param name="name"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddReservedName(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        reservedNames.Add(name);
    }

    #endregion

#nullable disable

    #region props

    /// <summary>
    /// 基类型的类型符号 -- 未解析的原始字符串
    ///
    /// 1.只有原始定义类可访问。
    /// 2.业务不要据此判断是否有超类，可根据<see cref="BaseType"/>判断。
    /// </summary>
    public string? BaseTypeSymbol => _baseTypeSymbol;

    /// <summary>
    /// 基类的类型引用，未显式声明的情况下为null
    /// </summary>
    public DSNamedType? BaseType {
        get => _baseType;
        set => _baseType = value;
    }

    public ImmutableList<DSTypeParameter> TypeParameters => _typeParameters;
    public ImmutableList<DSTypeElement> TypeArguments => _typeArguments;
    public List<Range> ReservedNumbers => reservedNumbers;
    public List<string> ReservedNames => reservedNames;

    #endregion

    #region equals

    private bool Equals(DSNamedType other) {
        return typeName.Equals(other.typeName)
               && _elementKind == other._elementKind
               && _typeKind == other._typeKind
               && CollectionUtil.SequenceEqual(_typeParameters, other._typeParameters)
               && CollectionUtil.SequenceEqual(_typeArguments, other._typeArguments);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is DSNamedType other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = typeName.GetHashCode() * 397 ^ (int)_elementKind;
        hashCode = (hashCode * 397) ^ (int)_typeKind;
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(_typeParameters);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(_typeArguments);
        return hashCode;
    }

    #endregion

    #region factory

    public static DSNamedType NewClassType(ClassName className, IList<DSTypeParameter>? typeParameters = null, string? baseTypeSymbol = null) {
        if (typeParameters == null) {
            typeParameters = CreateTypeParameters(className);
        } else {
            CheckTypeParameters(className, typeParameters);
        }
        return new DSNamedType(DSElementKind.Class, DSTypeKind.Class, className, typeParameters, baseTypeSymbol);
    }

    public static DSNamedType NewStructType(ClassName className, IList<DSTypeParameter>? typeParameters = null) {
        if (typeParameters == null) {
            typeParameters = CreateTypeParameters(className);
        } else {
            CheckTypeParameters(className, typeParameters);
        }
        return new DSNamedType(DSElementKind.Strut, DSTypeKind.Struct, className, typeParameters, null);
    }

    public static DSNamedType NewEnumType(ClassName className) {
        return new DSNamedType(DSElementKind.Enum, DSTypeKind.Enum, className, ImmutableList<DSTypeParameter>.Empty, null);
    }

    private static void CheckTypeParameters(ClassName className, IList<DSTypeParameter> typeParameters) {
        if (typeParameters.Count != className.typeArguments.Count) {
            throw new ArgumentException($"Class {className} does not have the same number of type arguments.");
        }
        for (int i = 0; i < typeParameters.Count; i++) {
            DSTypeParameter typeParameter = typeParameters[i];
            TypeParameterName typeArgumentName = (TypeParameterName)className.typeArguments[i];
            if (typeParameter.SimpleName != typeArgumentName.name) {
                throw new ArgumentException($"TypeParameter name mismatch, expected {typeParameter.SimpleName}, but found: {typeArgumentName.name}");
            }
        }
    }

    /** 根据name中的泛型变量构建类型参数 -- 不包含特殊约束时可使用 */
    private static IList<DSTypeParameter> CreateTypeParameters(ClassName className) {
        if (!className.IsGenericType) {
            return ImmutableList<DSTypeParameter>.Empty;
        }
        List<DSTypeParameter> typeParameters = new List<DSTypeParameter>(className.typeArguments.Count);
        foreach (TypeName typeArgument in className.typeArguments) {
            TypeParameterName typeParameterName = (TypeParameterName)typeArgument;
            typeParameters.Add(new DSTypeParameter(typeParameterName.name, TypeParameterConstraints.None));
        }
        return typeParameters;
    }

    #endregion
}
}