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

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口代理
/// </summary>
public interface WindowAgent
{
    /// <summary>
    /// 注入窗口
    /// (此时可以修正Window的组件信息)
    /// </summary>
    /// <param name="window"></param>
    void Inject(Window window);

    /// <summary>
    /// 获取绑定的Window
    /// </summary>
    Window Window { get; }

    #region 生命周期

    /// <summary>
    /// 启动窗口
    /// </summary>
    void OnStart() {
    }

    /// <summary>
    /// 窗口被暂停Update
    /// </summary>
    /// <param name="extraInfo"></param>
    void OnPause(object extraInfo) {
    }

    /// <summary>
    /// 窗口恢复运行
    /// </summary>
    /// <param name="extraInfo"></param>
    void OnResume(object extraInfo) {
    }

    /// <summary>
    /// 停止窗口
    /// </summary>
    void OnStop() {
    }

    /// <summary>
    /// 清理运行过程中产生的临时数据
    /// </summary>
    void Reset() {

    }

    #endregion

    #region 窗口状态

    /// <summary>
    /// 窗体展示模式变更
    /// </summary>
    /// <param name="prevMode"></param>
    void OnDisplayModeChanged(WindowDisplayMode prevMode) {
    }

    /// <summary>
    /// 窗口被切换到新桌面
    ///
    /// 注：特殊窗体可能需要更新数据
    /// </summary>
    void OnDesktopChanged() {
    }

    /// <summary>
    /// 焦点事件
    /// </summary>
    /// <param name="hasFocus"></param>
    void OnFocus(bool hasFocus) {
    }

    #endregion

    /// <summary>
    /// 自定义事件
    /// </summary>
    /// <param name="eventData"></param>
    void OnCustomEvent(object eventData) {
    }
}
}