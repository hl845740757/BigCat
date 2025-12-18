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

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 路径信息缓存
/// </summary>
internal class PathCache
{
    private readonly Dictionary<string, string> normalizeCache;
    private readonly Dictionary<string, string> parentDirectoryCache;

    public PathCache(int count = 1000) {
        this.normalizeCache = new Dictionary<string, string>(count);
        this.parentDirectoryCache = new Dictionary<string, string>(1000);
    }

    public string NormalizedPath(string path) {
        if (!normalizeCache.TryGetValue(path, out string value)) {
            value = UnityEditorUtil.NormalizeAssetPath(path);
            normalizeCache[path] = value;
        }
        return value;
    }

    /// <summary>
    /// 获取文件夹名字
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public string GetDirectoryName(string assetPath) {
        int index = assetPath.LastIndexOf('/');
        return index == -1 ? "" : assetPath.Substring(0, index);
    }

    /// <summary>
    /// 获取父文件夹路径
    /// </summary>
    /// <param name="directory">当前路径</param>
    /// <param name="normalize">返回值是否规格化</param>
    /// <returns></returns>
    public string GetParentPath(string directory, bool normalize = false) {
        if (!parentDirectoryCache.TryGetValue(directory, out string value)) {
            // Path.GetDirectoryName会将斜杆转为反斜杠...
            value = GetDirectoryName(directory);
            parentDirectoryCache[directory] = value;
        }
        return normalize ? NormalizedPath(value) : value;
    }

    /// <summary>
    /// 获取子文件夹的深度
    /// 注：参数要么都是规格化路径，要么都是操作系统的路径
    /// </summary>
    /// <param name="relativeRoot">相对根目录</param>
    /// <param name="current">当前目录</param>
    /// <returns></returns>
    public int GetDirectoryDepth(string relativeRoot, string current) {
        if (current.Length < relativeRoot.Length) {
            throw new InvalidOperationException($"path: {relativeRoot}, subPath: {current}");
        }
        int r = 0;
        while (current.Length > relativeRoot.Length && current != relativeRoot) {
            current = GetParentPath(current);
            r++;
        }
        return r;
    }
}
}