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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Worker引用的持有者，用于容器内组件准确获取Worker的引用。
/// </summary>
public sealed class WorkerHolder
{
#nullable disable
    /// <summary>
    /// 建议在在启动Worker前就初始化引用
    /// </summary>
    public IWorker Worker { get; set; }
}
}