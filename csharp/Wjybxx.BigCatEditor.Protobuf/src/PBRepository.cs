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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// pb文件仓库，用于建立索引，提供查询
///
/// 注意：顶层元素禁止重复
/// </summary>
public class PBRepository
{
    private readonly LinkedDictionary<string, PBFile> fileMap = new();
    private readonly LinkedDictionary<string, PBElement> topElementNameMap = new();

    public PBRepository AddFile(PBFile pbFile) {
        string simpleName = pbFile.SimpleName;
        // 检查重复
        if (fileMap.ContainsKey(simpleName)) {
            throw new ArgumentException("duplicate fileName " + simpleName);
        }
        fileMap[simpleName] = pbFile;

        // 添加索引
        topElementNameMap[simpleName] = pbFile;
        foreach (PBElement element in pbFile.EnclosedElements) {
            topElementNameMap[element.SimpleName] = element;
        }
        return this;
    }

    public PBFile RemoveFile(string simpleName) {
        if (fileMap.Remove(simpleName, out PBFile pbFile)) {
            // 删除索引
            topElementNameMap.Remove(simpleName);
            foreach (PBElement element in pbFile.EnclosedElements) {
                topElementNameMap.Remove(element.SimpleName);
            }
        }
        return pbFile;
    }

    /** 获取所有的文件 -- 不可修改 */
    public ICollection<PBFile> GetFiles() {
        return fileMap.Values;
    }

    /** 获取排序后的所有的文件 -- 根据文件名排序，有助于逻辑的稳定性 */
    public List<PBFile> GetSortedFiles() {
        List<PBFile> result = new(fileMap.Values);
        result.Sort((a, b) => string.Compare(a.SimpleName, b.SimpleName, StringComparison.Ordinal));
        return result;
    }

    /** 获取指定文件 */
    public PBFile? GetFile(string fileSimpleName) {
        fileMap.TryGetValue(fileSimpleName, out PBFile pbFile);
        return pbFile;
    }

    /// <summary>
    /// 获取顶层元素
    /// </summary>
    /// <param name="elementName">顶层元素名，或文件名</param>
    /// <returns></returns>
    public PBElement? GetTopElement(string elementName) {
        topElementNameMap.TryGetValue(elementName, out PBElement element);
        return element;
    }

    /// <summary>
    /// 获取顶层元素关联的文件
    /// </summary>
    /// <param name="elementName">elementName 顶层元素名，或文件名</param>
    /// <returns></returns>
    public PBFile? GetFileOfTopElement(string elementName) {
        if (!topElementNameMap.TryGetValue(elementName, out PBElement element)) {
            return null;
        }
        if (element.Kind == PBElementKind.File) {
            return (PBFile)element;
        }
        return (PBFile)element.EnclosingElement;
    }
}
}