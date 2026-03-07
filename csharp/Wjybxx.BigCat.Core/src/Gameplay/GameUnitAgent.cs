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

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 游戏对象的内部代理，辅助管理<see cref="GameUnit"/>
///
/// 注：游戏的单位的Agent由Scene创建，而非由用户创建。
/// </summary>
public interface GameUnitAgent
{
    /// <summary>
    /// 启动GameObject
    ///
    /// 1.该方法在游戏对象加入场景时调用。
    /// 2.主要逻辑包括：修正对象数据，调整组件的顺序。
    /// </summary>
    void Start(GameUnit gameUnit) {
    }

    /// <summary>
    /// 执行GameObject的基础行为Update
    /// </summary>
    /// <param name="gameUnit"></param>
    /// <param name="deltaTime"></param>
    void Update(GameUnit gameUnit, double deltaTime) {
    }

    /// <summary>
    /// 停止GameObject
    ///
    /// 1.该方法在游戏对象离开场景时调用。
    /// 2.主要用于清理数据
    /// 3.可以在这里执行延迟销毁
    /// </summary>
    void Stop(GameUnit gameUnit) {
    }

    /// <summary>
    /// 当GameObject加入子场景时调用
    /// </summary>
    /// <param name="gameUnit"></param>
    void OnEnterSubScene(GameUnit gameUnit) {
    }

    /// <summary>
    /// 当GameObject离开子场景时调用
    /// </summary>
    /// <param name="gameUnit"></param>
    void OnLeaveSubScene(GameUnit gameUnit) {
    }

    /// <summary>
    /// GameObject的Active状态发生变化
    /// </summary>
    /// <param name="gameUnit"></param>
    void OnActiveChanged(GameUnit gameUnit) {
    }
}
}