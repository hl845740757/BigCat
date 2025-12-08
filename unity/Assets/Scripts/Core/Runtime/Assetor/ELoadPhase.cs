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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源加载阶段
///
/// 注：这里只定义大阶段，以避免细节问题。
/// </summary>
public enum ELoadPhase
{
    Pending = 0, // 等待中
    Downloading = 1, // 下载中(下载 + 验证)
    Importing = 2, // 导入中(拷贝 + 解压)
    Loading = 3, // 加载中
    Done = 4, // 结束 -- 可能不会显式赋值，因为是冗余，可通过Promise感知任务完成
}
}