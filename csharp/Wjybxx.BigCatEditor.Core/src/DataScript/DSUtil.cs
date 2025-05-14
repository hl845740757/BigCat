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

using System.Collections.Generic;

namespace Wjybxx.BigCatEditor.DataScript
{
public static class DSUtil
{
    /// <summary>
    /// 将继承打平
    /// </summary>
    /// <param name="typeElement">当前类型</param>
    /// <param name="reverse">超类是否在前</param>
    /// <returns></returns>
    public static List<DSNamedTypeElement> FlatInherit(DSNamedTypeElement typeElement, bool reverse = true) {
        List<DSNamedTypeElement> result = new List<DSNamedTypeElement>();
        result.Add(typeElement);
        while ((typeElement = typeElement.BaseType) != null) {
            result.Add(typeElement);
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
    public static List<DSNamedTypeElement> GetAllEnclosedTypes(DSElement root) {
        List<DSNamedTypeElement> result = new List<DSNamedTypeElement>();
        GetAllEnclosedTypes(root, result);
        return result;
    }

    /// <summary>
    /// 获取元素内定义的所有类型
    /// （全部打平，深度遍历）
    /// </summary>
    /// <param name="current"></param>
    /// <param name="outList"></param>
    public static void GetAllEnclosedTypes(DSElement current, List<DSNamedTypeElement> outList) {
        foreach (var element in current.EnclosedElements) {
            if (!element.IsTypeElement) {
                continue;
            }
            outList.Add((DSNamedTypeElement)element);
            if (element.EnclosedElements.Count > 0) {
                GetAllEnclosedTypes(element, outList);
            }
        }
    }

    /// <summary>
    /// 是否包含非运行时类型参数(未确定的类型参数)
    /// </summary>
    public static bool HasNonRuntimeTypeArgument(this DSTypeElement typeElement) {
        if (typeElement.TypeKind == DSTypeKind.TypeParameter) return true;
        if (typeElement is DSNamedTypeElement namedTypeElement) {
            if (namedTypeElement.TypeParameters.Count > 0) return true;
            foreach (DSTypeElement typeArgument in namedTypeElement.TypeArguments) {
                if (HasNonRuntimeTypeArgument(typeArgument)) return true;
            }
        }
        return false;
    }
}
}