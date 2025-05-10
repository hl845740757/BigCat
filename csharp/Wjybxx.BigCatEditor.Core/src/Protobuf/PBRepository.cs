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
using System.IO;
using Wjybxx.BigCatEditor.Core;
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
    /// <summary>
    /// 文件简单名到文件的映射
    /// </summary>
    private readonly LinkedDictionary<string, PBFile> fileMap = new();
    /// <summary>
    /// 顶层元素名到元素的映射
    /// key为[fileSimpleName, elementName]
    /// </summary>
    private readonly LinkedDictionary<StringPair, PBElement> topElementMap = new();

    /// <summary>
    /// 构建最终数据 
    /// 如果项目使用了<code>import public</code>特性，需要调用该方法
    /// </summary>
    public void Build() {
        HashSet<string> tempSet = new HashSet<string>(16);
        foreach (PBFile file in fileMap.Values) {
            tempSet.Clear();
            ResolvePublicImports(file, tempSet, 0);
            file.ResolvedImports.AddAll(tempSet);
        }
    }

    private void ResolvePublicImports(PBFile entryFile, HashSet<string> result, int deep) {
        if (deep > 32) {
            throw new InvalidOperationException("something is error, deep: " + deep);
        }
        // pb规范是包含".proto"后缀的，是可以引用其它目录的文件吗?
        foreach (string importFileName in entryFile.Imports.Keys) {
            string fileSimpleName = Path.GetFileNameWithoutExtension(importFileName);
            PBFile curFile = GetFile(fileSimpleName);
            if (curFile == null) {
                throw new InvalidOperationException($"{entryFile.FileName} cant resolve import: {importFileName}");
            }
            foreach (var pair in curFile.Imports) {
                if (pair.Value == PBKeywords.PUBLIC) {
                    result.Add(pair.Key);
                    ResolvePublicImports(curFile, result, deep + 1);
                }
            }
        }
    }

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="pbFile"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public PBRepository AddFile(PBFile pbFile) {
        string simpleName = pbFile.SimpleName;
        // 检查重复
        if (fileMap.ContainsKey(simpleName)) {
            throw new ArgumentException("duplicate fileName " + simpleName);
        }
        fileMap[simpleName] = pbFile;
        // 添加索引
        foreach (PBElement element in pbFile.EnclosedElements) {
            var key = new StringPair(simpleName, element.SimpleName);
            topElementMap[key] = element;
        }
        return this;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="simpleName"></param>
    /// <returns></returns>
    public PBFile RemoveFile(string simpleName) {
        if (fileMap.Remove(simpleName, out PBFile pbFile)) {
            // 删除索引
            foreach (PBElement element in pbFile.EnclosedElements) {
                var key = new StringPair(simpleName, element.SimpleName);
                topElementMap.Remove(key);
            }
        }
        return pbFile;
    }

    /// <summary>
    /// 获取所有的文件 -- 不可修改
    /// </summary>
    /// <returns></returns>
    public ICollection<PBFile> GetFiles() {
        return fileMap.Values;
    }

    /// <summary>
    /// 获取排序后的所有的文件 -- 根据文件名排序，有助于逻辑的稳定性
    /// </summary>
    /// <returns></returns>
    public List<PBFile> GetSortedFiles() {
        List<PBFile> result = new(fileMap.Values);
        result.Sort((a, b) => string.Compare(a.SimpleName, b.SimpleName, StringComparison.Ordinal));
        return result;
    }

    /// <summary>
    /// 获取指定文件
    /// </summary>
    /// <param name="simpleName">文件简单名，不包含proto后缀</param>
    /// <returns></returns>
    public PBFile? GetFile(string simpleName) {
        fileMap.TryGetValue(simpleName, out PBFile pbFile);
        return pbFile;
    }

    /// <summary>
    /// 获取顶层元素
    /// </summary>
    /// <param name="fileSimpleName">文件简单名</param>
    /// <param name="elementName">顶层元素名</param>
    /// <returns></returns>
    public PBElement? GetTopElement(string fileSimpleName, string elementName) {
        var key = new StringPair(fileSimpleName, elementName);
        topElementMap.TryGetValue(key, out PBElement element);
        return element;
    }
}
}