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

using Wjybxx.BigCat.Assetor;

namespace Wjybxx.BigCat.MVC
{
/// <summary>
/// 打开窗口参数
/// </summary>
public sealed class WindowOpenArgs
{
    /// <summary>
    /// 窗口的数据模型
    ///
    /// 注：如果未指定数据，则表示根据Window的配置从默认的视图数据查找。
    /// </summary>
    public object? dataModel;
    /// <summary>
    /// 用户自定义数据
    /// </summary>
    public object? userData;

    /// <summary>
    /// 如果目标窗口已打开，是否顶掉旧窗口
    /// </summary>
    public bool reopen = false;
    /// <summary>
    /// 加载的超时时间
    /// </summary>
    public double timeout = -1;

    /// <summary>
    /// 父窗口实例id
    /// </summary>
    public int pInstId;
    /// <summary>
    /// 是否忽略父窗口关闭信号
    /// (是否不跟随父窗口一起关闭)
    /// </summary>
    public bool nohup;

    /// <summary>
    /// 资源对象句柄
    /// 注：该参数由框架赋值，用户避免访问。
    /// </summary>
    public AssetHandle assetHandle;
}
}