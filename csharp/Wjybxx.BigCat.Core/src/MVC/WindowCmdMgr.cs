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

namespace Wjybxx.BigCat.MVC
{
/// <summary>
/// 窗口命令管理器
///
/// 1.窗口命令是逻辑层和视图层交互的对象，由于命令较少，我们直接定义为一个个方法。
/// 2.我们的业务模块只依赖该管理器，而不依赖<code>WindowMgr</code>，这允许我们在没有窗口的情况下保证逻辑层可运行。
/// </summary>
public interface WindowCmdMgr
{
    /// <summary>
    /// 打开窗口
    /// 
    /// 注：不绑定回调，因为窗口关联的行为库是确定的，因此通过数据确定处理函数即可。
    /// </summary>
    /// <param name="windowAddr">窗口地址</param>
    /// <param name="openArgs">打开参数</param>
    void Open(string windowAddr, WindowOpenArgs openArgs);

    /// <summary>
    /// 关闭窗口
    /// </summary>
    /// <param name="windowAddr">窗口地址</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    void Close(string windowAddr, bool force = false);

    /// <summary>
    /// 关闭多个窗口
    /// </summary>
    /// <param name="windowAddrList">窗口地址</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    void Close(List<string> windowAddrList, bool force = false);

    /// <summary>
    /// 关闭当前桌面的具有任一指定Tag的窗口
    /// </summary>
    /// <param name="tags">需要关闭的窗口类型</param>
    /// <param name="force">是否强制关闭常驻窗口</param>
    void CloseTagged(HashSet<int> tags, bool force = false);

    /// <summary>
    /// 关闭当前桌面所有普通窗口(非常驻窗口)
    /// </summary>
    /// <param name="force">是否强制关闭常驻窗口</param>
    void CloseAll(bool force = false);

    /// <summary>
    /// 切换桌面
    ///
    /// 注：切换桌面时，跨桌面窗口会跟随切换，其它窗口默认隐藏（而非关闭）。
    /// </summary>
    /// <param name="desktopId">目标桌面id</param>
    void SwitchDesktop(int desktopId);

    /// <summary>
    /// 将指定窗口移动到目标桌面
    /// </summary>
    /// <param name="windowAddr">窗口地址</param>
    /// <param name="desktopId">桌面id</param>
    void MoveToDesktop(string windowAddr, int desktopId);
}
}