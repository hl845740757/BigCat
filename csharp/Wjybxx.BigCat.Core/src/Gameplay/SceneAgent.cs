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

using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 场景类的内部代理，辅助管理<see cref="Scene"/>
///
/// 注：<see cref="SceneAgent"/>与<see cref="Scene"/>的生命周期绑定。
/// </summary>
[Immutable]
public interface SceneAgent
{
    /// <summary>
    /// 注入场景实例
    /// (此时可以修正Scene的组件信息)
    /// </summary>
    void Inject(Scene scene);

    #region 生命周期

    /// <summary>
    /// 场景启动
    /// </summary>
    void OnStart() {
    }

    /// <summary>
    /// 场景进入暂停状态
    /// </summary>
    /// <param name="extraInfo">附加信息</param>
    void OnPausing(object? extraInfo) {
    }

    /// <summary>
    /// 场景恢复运行
    /// </summary>
    /// <param name="extraInfo">附加信息</param>
    void OnResume(object? extraInfo) {
    }

    /// <summary>
    /// 场景停止（关闭）
    /// </summary>
    void OnStop() {

    }

    /// <summary>
    /// 清理运行过程中产生的临时数据
    /// </summary>
    void Reset() {

    }

    #endregion

    /// <summary>
    /// 场景的激活状态变化
    /// </summary>
    void OnActiveChanged() {
    }

    /// <summary>
    /// 游戏对象添加到<see cref="GameUnitMgr"/>后调用，主要用于维护缓存列表。
    ///
    /// 注：可以在此时绑定<see cref="GameUnitAgent"/>。
    /// </summary>
    /// <param name="gameUnit"></param>
    void OnGameUnitAdded(GameUnit gameUnit) {
    }

    /// <summary>
    /// 游戏对象从<see cref="GameUnitMgr"/>删除后调用，主要用于删除额外的缓存数据。
    /// 
    /// 注：已自动从<see cref="GameUnitMgr"/>的缓存列表中删除。
    /// </summary>
    /// <param name="gameUnit"></param>
    void OnGameUnitRemoved(GameUnit gameUnit) {
    }

    /// <summary>
    /// 游戏对象在<see cref="GameUnitMgr"/>中的索引发生变化时调用。
    /// 
    /// 1.添加和移除时也会调用。
    /// 2.主要用于维护缓存数据
    /// </summary>
    /// <param name="gameUnit"></param>
    /// <param name="prevIndex">之前的索引</param>
    void OnGameUnitIndexChanged(GameUnit gameUnit, int prevIndex) {
    }

    /// <summary>
    /// 自定义事件
    /// </summary>
    /// <param name="eventData"></param>
    void OnCustomEvent(object eventData) {
    }
}
}