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
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCatTool.DataScript
{
public static class DSUtil
{
    /// <summary>
    /// 默认的内建类型
    /// </summary>
    public static readonly ImmutableList<DSNamedType> builtinTypes = new[]
    {
        // 原子类型
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_INT32),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_INT64),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_FLOAT),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_DOUBLE),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_BOOL),
        DSNamedType.NewClassType(DSKeywords.TYPE_NAME_STRING),
        DSNamedType.NewClassType(DSKeywords.TYPE_NAME_BYTES),
        // 内建结构
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_DATETIME),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_TIMESTAMP),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_PAIR),
        // 基础容器
        DSNamedType.NewClassType(DSKeywords.TYPE_NAME_LIST),
        DSNamedType.NewClassType(DSKeywords.TYPE_NAME_HASH_SET),
        DSNamedType.NewClassType(DSKeywords.TYPE_NAME_MAP),
        // 装箱类型
        DSNamedType.NewClassType(DSKeywords.TYPE_NAME_OBJECT),
        DSNamedType.NewStructType(DSKeywords.TYPE_NAME_NULLABLE, new List<DSTypeParameter>(1)
        {
            new DSTypeParameter("T", TypeParameterConstraints.ValueTypeConstraint)
        })
    }.ToImmutableList2();

    public static bool IsType(this DSElementKind kind) {
        return kind == DSElementKind.Class
               || kind == DSElementKind.Strut
               || kind == DSElementKind.Enum
               || kind == DSElementKind.TypeParameter;
    }

    public static bool IsNamedType(this DSElementKind kind) {
        return kind == DSElementKind.Class
               || kind == DSElementKind.Strut
               || kind == DSElementKind.Enum;
    }

    /// <summary>
    /// 将继承打平
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
    /// 获取第一级名字
    ///
    /// <code>A.B => A</code>
    /// <code>A.B.C => A</code>>
    /// </summary>
    public static string GetFirstName(string fullName) {
        int idx = fullName.IndexOf('.');
        return idx < 0 ? fullName : fullName.Substring2(0, idx);
    }

    /// <summary>
    /// 获取第二级名字
    ///
    /// <code>A.B => B</code>
    /// <code>A.B.C => B</code>
    /// </summary>
    public static string GetSecondName(string fullName) {
        int start = fullName.IndexOf('.');
        int end = fullName.LastIndexOf('.');
        if (start < 0) {
            throw new ArgumentException("name is invalid: " + fullName);
        }
        if (start < end) { // A.B.C
            return fullName.Substring2(start + 1, end);
        }
        return fullName.Substring2(start + 1); // A.B
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
            }
            // 命名空间(顶层是文件名)
            sb.Insert(0, className.ns);
            return sb.ToString();
        }
        finally {
            ConcurrentObjectPool.SharedStringBuilderPool.Release(sb);
        }
    }
}
}