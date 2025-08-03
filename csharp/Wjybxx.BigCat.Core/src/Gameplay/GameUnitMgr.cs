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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Logger;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 游戏实体管理器
///
/// 注：每个场景一个，非全局单例。
/// </summary>
public sealed class GameUnitMgr
{
    /// <summary>
    /// 游戏对象列表的初始大小，客户端小一些
    /// </summary>
#if UNITY_2021_3_OR_NEWER || CLIENT_PROJECT
    private const int INIT_CAPACITY = 30;
#else
    private const int INIT_CAPACITY = 100;
#endif

    private readonly Scene scene;
    /// <summary>
    /// 所有的游戏单位
    /// 注意：外部不可直接修改。
    /// </summary>
    private readonly IndexedDynamicArray<GameUnit> gameUnitList = new(GIndexHelper.MAIN_HELPER, INIT_CAPACITY, 0);
    /// <summary>
    /// 游戏单位的字典映射
    /// 注意：不能用于迭代。
    /// </summary>
    private readonly Dictionary<long, GameUnit> gameUnitDic = new Dictionary<long, GameUnit>(INIT_CAPACITY);

    // 缓存列表 -- 9个应该足够，玩家、Npc、角色（玩家+Npc）、非角色（子弹+触发器）、怪物（便于统计）、预设对象、其它
    private readonly IndexedDynamicArray<GameUnit> cacheList1 = new(GIndexHelper.GetInst(1), 10, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList2 = new(GIndexHelper.GetInst(2), 20, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList3 = new(GIndexHelper.GetInst(3), 30, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList4 = new(GIndexHelper.GetInst(4), 30, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList5 = new(GIndexHelper.GetInst(5), 20, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList6 = new(GIndexHelper.GetInst(6), 20, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList7 = new(GIndexHelper.GetInst(7), 20, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList8 = new(GIndexHelper.GetInst(8), 20, 0.1f);
    private readonly IndexedDynamicArray<GameUnit> cacheList9 = new(GIndexHelper.GetInst(9), 20, 0.1f);

    public GameUnitMgr(Scene scene) {
        this.scene = scene;
    }

    /// <summary>
    /// 包含所有游戏单位的列表
    /// 注：不可直接修改
    /// </summary>
    public IndexedDynamicArray<GameUnit> GameUnitList => gameUnitList;
    /// <summary>
    /// 包含所有游戏单位的字典
    /// 注：不可直接修改
    /// </summary>
    public Dictionary<long, GameUnit> GameUnitDic => gameUnitDic;

    /// <summary>
    /// 添加游戏对象
    /// </summary>
    public void Add(GameUnit gameUnit) {
        gameUnit.Scene = scene;
        if (gameUnit.Status == ComponentStatus.New) {
            gameUnit.SetInitialized();
        }
        //
        gameUnitDic.Add(gameUnit.InstId, gameUnit); // 检测重复
        gameUnitList.Add(gameUnit);
        scene.Agent.OnGameUnitAdded(gameUnit);
        try {
            gameUnit.Agent?.Start(gameUnit); // 启动对象
        }
        catch (Exception ex) {
            Scene.logger.Warn(ex, "gameUnit.Start caught exception");
        }
    }

    /// <summary>
    /// 删除游戏单位
    /// </summary>
    /// <param name="gameUnit"></param>
    /// <returns></returns>
    public bool Remove(GameUnit gameUnit) {
        if (gameUnitDic.TryGetValue(gameUnit.InstId, out GameUnit exist)
            && ReferenceEquals(exist, gameUnit)) {
            RemoveImpl(gameUnit);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 删除游戏对象
    ///
    /// 注：不返回对象，因为可能已被回收。
    /// </summary>
    /// <param name="instId"></param>
    public void Remove(long instId) {
        if (gameUnitDic.TryGetValue(instId, out GameUnit exist)) {
            RemoveImpl(exist);
        }
    }

    private void RemoveImpl(GameUnit gameUnit) {
        try {
            gameUnit.Agent?.Stop(gameUnit);
        }
        catch (Exception ex) {
            Scene.logger.Warn(ex, "gameUnit.Stop caught exception");
        }
        gameUnit.Scene = null;
        //
        gameUnitList.Remove(gameUnit);
        RemoveFromCacheLists(gameUnit);
        scene.Agent.OnGameUnitRemoved(gameUnit);
    }

    private void RemoveFromCacheLists(GameUnit gameUnit) {
        // 虽然看起来调用很多，但速度很快
        cacheList1.Remove(gameUnit);
        cacheList2.Remove(gameUnit);
        cacheList3.Remove(gameUnit);
        cacheList4.Remove(gameUnit);
        cacheList5.Remove(gameUnit);
        cacheList6.Remove(gameUnit);
        cacheList7.Remove(gameUnit);
        cacheList8.Remove(gameUnit);
        cacheList9.Remove(gameUnit);
    }

    public void Clear() {
        gameUnitDic.Clear();
        gameUnitList.Clear();
        for (int listId = 1; listId <= 9; listId++) {
            GetGameUnitList(listId).Clear();
        }
    }

    internal void Destroy() {
        gameUnitList.BeginItr();
        for (int i = 0, len = gameUnitList.Length; i < len; i++) {
            GameUnit gameUnit = gameUnitList.Set(i, null);
            if (gameUnit == null) continue;
            try {
                gameUnit.Destroy();
            }
            catch (Exception ex) {
                Scene.logger.Warn(ex);
            }
        }
        gameUnitList.EndItr();
        Clear();
    }

    /// <summary>
    /// 获取缓存列表
    /// </summary>
    /// <param name="listId">列表，建议业务定义为枚举</param>
    /// <returns></returns>
    public IndexedDynamicArray<GameUnit> GetGameUnitList(int listId) {
        return listId switch
        {
            0 => gameUnitList,
            1 => cacheList1,
            2 => cacheList2,
            3 => cacheList3,
            4 => cacheList4,
            5 => cacheList5,
            6 => cacheList6,
            7 => cacheList7,
            8 => cacheList8,
            9 => cacheList9,
            _ => throw new ArgumentOutOfRangeException(nameof(listId), listId, null)
        };
    }
}
}