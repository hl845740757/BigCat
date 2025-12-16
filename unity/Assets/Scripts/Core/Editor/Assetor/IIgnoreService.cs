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

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 资源文件过滤服务
/// </summary>
public interface IIgnoreService
{
    /// <summary>
    /// 启动服务
    /// (初始化忽略规则文件)
    /// </summary>
    void Start();

    /// <summary>
    /// 是否需要忽略文件
    /// </summary>
    bool IsIgnore(string assetPath);

    /// <summary>
    /// 停止服务
    /// </summary>
    void Stop() {
    }
}

// 空对象-用于避免NPE
public class NullIgnoreService : IIgnoreService
{
    public void Start() {
    }

    public bool IsIgnore(string assetPath) {
        return false;
    }
}
}