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
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatTool.DataScript
{
public static class DSUtil
{
    // 常用扩展集合类型
    public const string TYPE_LINKED_HASHSET = "LinkedHashSet";
    public const string TYPE_LINKED_MAP = "LinkedMap";
    public const string TYPE_ARRAY_MAP = "ArrayMap";
    // 不可变集合
    public const string TYPE_IMMUTABLE_LIST = "ImmutableList";
    public const string TYPE_IMMUTABLE_SET = "ImmutableSet";
    public const string TYPE_IMMUTABLE_MAP = "ImmutableMap";

    public static bool IsType(this DSElementKind kind) {
        return kind == DSElementKind.Class
               || kind == DSElementKind.Strut
               || kind == DSElementKind.Enum
               || kind == DSElementKind.Service
               || kind == DSElementKind.TypeParameter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNamedType(this DSElementKind kind) {
        return kind == DSElementKind.Class
               || kind == DSElementKind.Strut
               || kind == DSElementKind.Enum
               || kind == DSElementKind.Service;
    }

    /// <summary>
    /// 获取类型的根类型
    /// </summary>
    /// <param name="namedType"></param>
    /// <returns></returns>
    public static DSNamedType GetRootType(DSNamedType namedType) {
        while (namedType.BaseType != null) {
            namedType = namedType.BaseType;
        }
        return namedType;
    }

    /// <summary>
    /// 将继承打平(不会访问到object -- 这不是编程语言)
    /// </summary>
    /// <param name="namedType">当前类型</param>
    /// <param name="reverse">超类是否在前</param>
    /// <returns></returns>
    public static List<DSNamedType> FlatInherit(DSNamedType namedType, bool reverse = true) {
        List<DSNamedType> result = new List<DSNamedType>();
        result.Add(namedType);
        while ((namedType = namedType.BaseType) != null) {
            result.Add(namedType);
        }
        if (reverse) {
            result.Reverse();
        }
        return result;
    }

    /// <summary>
    /// 获取元素内定义的所有元素
    /// （全部打平，深度遍历）
    /// </summary>
    /// <returns></returns>
    public static List<DSElement> GetAllEnclosedElements(DSElement root) {
        List<DSElement> result = new List<DSElement>();
        GetAllEnclosedElements(root, result);
        return result;
    }

    /// <summary>
    /// 获取元素内定义的所有元素
    /// （全部打平，深度遍历）
    /// </summary>
    /// <param name="current"></param>
    /// <param name="outList"></param>
    public static void GetAllEnclosedElements(DSElement current, List<DSElement> outList) {
        foreach (var element in current.EnclosedElements) {
            outList.Add(element);
            if (element.EnclosedElements.Count > 0) {
                GetAllEnclosedElements(element, outList);
            }
        }
    }

    /// <summary>
    /// 获取元素内定义的所有类型
    /// （全部打平，深度遍历）
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public static List<DSNamedType> GetAllEnclosedTypes(DSElement root) {
        List<DSNamedType> result = new List<DSNamedType>();
        GetAllEnclosedTypes(root, result);
        return result;
    }

    /// <summary>
    /// 获取元素内定义的所有类型
    /// （全部打平，深度遍历）
    /// </summary>
    /// <param name="current"></param>
    /// <param name="outList"></param>
    public static void GetAllEnclosedTypes(DSElement current, List<DSNamedType> outList) {
        foreach (var element in current.EnclosedElements) {
            if (!element.Kind.IsNamedType()) {
                continue;
            }
            outList.Add((DSNamedType)element);
            if (element.EnclosedElements.Count > 0) {
                GetAllEnclosedTypes(element, outList);
            }
        }
    }

    /// <summary>
    /// 是否包含非运行时类型参数(未确定的类型参数)
    /// </summary>
    public static bool HasNonRuntimeTypeArgument(DSTypeElement typeElement) {
        if (typeElement.TypeKind == DSTypeKind.TypeParameter) return true;
        if (typeElement is DSNamedType namedType) {
            if (namedType.TypeParameters.Count > 0) return true;
            foreach (DSTypeElement typeArgument in namedType.TypeArguments) {
                if (HasNonRuntimeTypeArgument(typeArgument)) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 是否是数字类型
    ///
    /// 数字类型支持Dson文本支持的所有格式，此外还支持Flags格式<code>A|B|C</code>；
    /// 如果其它类型也期望使用支持Flags类型，需要自定义<see cref="DSTypeHandler"/>。
    /// </summary>
    public static bool IsNumberType(DSElement typeElement) {
        if (typeElement.Kind.IsNamedType()) {
            return typeElement.SimpleName switch
            {
                DSKeywords.TYPE_INT32 => true,
                DSKeywords.TYPE_INT64 => true,
                DSKeywords.TYPE_FLOAT => true,
                DSKeywords.TYPE_DOUBLE => true,
                _ => false
            };
        }
        return false;
    }

    /// <summary>
    /// 是否是bool类型
    /// </summary>
    public static bool IsBoolType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_BOOL;
    }

    /// <summary>
    /// 是否是string类型
    /// </summary>
    public static bool IsStringType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_STRING;
    }

    /// <summary>
    /// 是否是字节数组类型
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    public static bool IsBytesType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_BYTES;
    }

    /// <summary>
    /// 是否是日期时间类型
    /// </summary>
    public static bool IsDateTimeType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_DATETIME;
    }

    public static bool IsTimestampType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_TIMESTAMP;
    }

    /// <summary>
    /// 是否是对象指针类型
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    public static bool IsPointerType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_POINTER;
    }

    /// <summary>
    /// 是否是可空值类型
    /// </summary>
    public static bool IsNullableType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_NULLABLE;
    }

    /// <summary>
    /// 是否是Pair类型
    /// </summary>
    public static bool IsPairType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName == DSKeywords.TYPE_PAIR;
    }

    /// <summary>
    /// 是否是List类型
    /// </summary>
    public static bool IsListType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName switch
        {
            DSKeywords.TYPE_LIST => true,
            TYPE_IMMUTABLE_LIST => true,
            _ => false
        };
    }

    /// <summary>
    /// 是否是Set类型
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    public static bool IsSetType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName switch
        {
            DSKeywords.TYPE_HASHSET => true,
            TYPE_LINKED_HASHSET => true,
            TYPE_IMMUTABLE_SET => true,
            _ => false
        };
    }

    /// <summary>
    /// 是否是字典类型
    ///
    /// 注意：这里的仅仅是测试类型的字符串，因此并不完全精确，使用时要小心。
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    public static bool IsMapType(DSElement typeElement) {
        return typeElement.Kind.IsNamedType() && typeElement.SimpleName switch
        {
            DSKeywords.TYPE_MAP => true,
            TYPE_LINKED_MAP => true,
            TYPE_ARRAY_MAP => true,
            TYPE_IMMUTABLE_MAP => true,
            _ => false
        };
    }

    /// <summary>
    /// 是否是集合类型(List或Set)
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionType(DSElement typeElement) {
        return IsListType(typeElement) || IsSetType(typeElement);
    }

    /// <summary>
    /// 是否是集合或字典类型
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollectionOrMapType(DSElement typeElement) {
        return IsListType(typeElement) || IsSetType(typeElement) || IsMapType(typeElement);
    }

    /// <summary>
    /// 是否是原子值类型(不能再切割的类型)
    /// </summary>
    /// <param name="typeElement"></param>
    /// <returns></returns>
    public static bool IsAtomicType(DSElement typeElement) {
        if (!typeElement.Kind.IsNamedType()) return false;
        if (typeElement.Kind == DSElementKind.Enum) {
            return true;
        }
        return typeElement.SimpleName switch
        {
            DSKeywords.TYPE_INT32 => true,
            DSKeywords.TYPE_INT64 => true,
            DSKeywords.TYPE_FLOAT => true,
            DSKeywords.TYPE_DOUBLE => true,
            DSKeywords.TYPE_BOOL => true,
            DSKeywords.TYPE_STRING => true,
            DSKeywords.TYPE_BYTES => true,
            _ => false
        };
    }

    public static bool IsFlagEnum(DSNamedType namedType) {
        DsonObject<string> options = GetOptions(namedType);
        return Annotation.GetBool(options, DSAnnotations.KEY_IS_FLAGS);
    }

    #region Name工具方法

    /// <summary>
    /// 删除Name中的第一部分
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    public static string RemoveFirstName(string fullName) {
        int idx = fullName.IndexOf('.');
        return fullName.Substring(idx + 1);
    }

    /// <summary>
    /// 获取类型Import格式的名字
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    public static string GetCanonicalName(ClassName className) {
        StringBuilder sb = ConcurrentObjectPool.SharedStringBuilderPool.Acquire();
        try {
            sb.Insert(0, className.simpleName);
            sb.Insert(0, '.');
            // 外部类
            while (className.enclosingClassName != null) {
                className = className.enclosingClassName;
                sb.Insert(0, className.simpleName);
                sb.Insert(0, '.');
            }
            // 命名空间(顶层是文件名)
            sb.Insert(0, className.ns);
            return sb.ToString();
        }
        finally {
            ConcurrentObjectPool.SharedStringBuilderPool.Release(sb);
        }
    }

    /// <summary>
    /// 将ClassName转换为我们在源码中使用的字符串符号
    ///
    /// <![CDATA[
    ///    Map<int32, Vector3>
    ///    Map<int32, Vector3?>
    /// ]]>
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static string ToDisplayString(TypeName typeName) {
        if (typeName is TypeParameterName parameterName) {
            return parameterName.name;
        }
        ClassName className = (ClassName)typeName;
        if (className.simpleName == DSKeywords.TYPE_NULLABLE) {
            return ToDisplayString(className.typeArguments[0]) + "?";
        }
        string name = className.simpleName;
        IList<TypeName> declaredTypeArguments = className.declaredTypeArguments;
        if (declaredTypeArguments.Count > 0) {
            StringBuilder sb = new StringBuilder(name);
            sb.Append('<');
            for (int i = 0; i < declaredTypeArguments.Count; i++) {
                if (i > 0) sb.Append(',');
                sb.Append(ToDisplayString(declaredTypeArguments[i]));
            }
            sb.Append('>');
            name = sb.ToString();
        }
        if (className.enclosingClassName != null) {
            return ToDisplayString(className.enclosingClassName) + "." + name;
        } else {
            return className.ns + "." + name; // ns为文件名
        }
    }

    #endregion

    #region 注解处理

    private static readonly ImmutableDictionary<string, ObjectStyle>
        name2ObjectStyleDic = EnumUtil.GetValues<ObjectStyle>()
            .ToImmutableDictionary2(style => style.ToString().ToLower(), style => style);

    private static readonly ImmutableDictionary<string, NumberStyle>
        name2NumberStyleDic = EnumUtil.GetValues<NumberStyle>()
            .ToImmutableDictionary2(style => style.ToString().ToLower(), style => style);

    private static readonly ImmutableDictionary<string, StringStyle>
        name2StringStyleDic = EnumUtil.GetValues<StringStyle>()
            .ToImmutableDictionary2(style => style.ToString().ToLower(), style => style);

    /** 在只读的情况下返回空对象可以避免Null处理 */
    private static readonly DsonObject<string> EMPTY_DSON_OBJECT = new();

    public static DsonObject<string> GetOptions(DSElement element, bool isReadonly = true) {
        Annotation? annotation = element.GetAnnotation(DSAnnotations.OPTIONS);
        if (annotation == null) {
            return isReadonly ? EMPTY_DSON_OBJECT : new DsonObject<string>();
        }
        return annotation.AsObject();
    }

    public static DsonObject<string> GetRpcOptions(DSElement element, bool isReadonly = true) {
        Annotation? annotation = element.GetAnnotation(DSAnnotations.RPC);
        if (annotation == null) {
            return isReadonly ? EMPTY_DSON_OBJECT : new DsonObject<string>();
        }
        return annotation.AsObject();
    }

    public static DsonObject<string> GetCodecOptions(DSElement element, bool isReadonly = true) {
        Annotation? annotation = element.GetAnnotation(DSAnnotations.CODEC);
        if (annotation == null) {
            return isReadonly ? EMPTY_DSON_OBJECT : new DsonObject<string>();
        }
        return annotation.AsObject();
    }

    public static List<string> GetCodecAliases(DsonObject<string> options) {
        if (options.Count == 0 || !options.TryGetValue(DSAnnotations.KEY_ALIAS, out DsonValue value)) {
            return new List<string>();
        }
        DsonArray<string> dsonArray = value.AsArray();
        if (dsonArray.Count == 0) return new List<string>();
        //
        List<string> result = new List<string>(options.Count);
        foreach (DsonValue dsonValue in dsonArray) {
            result.Add(dsonValue.AsString().Trim());
        }
        return result;
    }

    public static ObjectStyle GetObjectStyle(DsonObject<string> options, ObjectStyle defaultStyle = ObjectStyle.Indent) {
        if (!options.TryGetValue(DSAnnotations.KEY_STYLE, out DsonValue value)) {
            return defaultStyle;
        }
        if (value.IsNumber) {
            return (ObjectStyle)value.AsNumber().IntValue;
        }
        string style = value.AsString().ToLower();
        return name2ObjectStyleDic.TryGetValue(style, out ObjectStyle result) ? result : defaultStyle;
    }

    public static NumberStyle GetNumberStyle(DsonObject<string> options, NumberStyle defaultStyle = NumberStyle.Simple) {
        if (!options.TryGetValue(DSAnnotations.KEY_STYLE, out DsonValue value)) {
            return defaultStyle;
        }
        if (value.IsNumber) {
            return (NumberStyle)value.AsNumber().IntValue;
        }
        string style = value.AsString().ToLower();
        return name2NumberStyleDic.TryGetValue(style, out NumberStyle result) ? result : defaultStyle;
    }

    public static StringStyle GetStringStyle(DsonObject<string> options, StringStyle defaultStyle = StringStyle.Auto) {
        if (!options.TryGetValue(DSAnnotations.KEY_STYLE, out DsonValue value)) {
            return defaultStyle;
        }
        if (value.IsNumber) {
            return (StringStyle)value.AsNumber().IntValue;
        }
        string style = value.AsString().ToLower();
        return name2StringStyleDic.TryGetValue(style, out StringStyle result) ? result : defaultStyle;
    }

    #endregion
}
}