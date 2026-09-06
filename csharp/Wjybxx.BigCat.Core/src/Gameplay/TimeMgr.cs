#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 全局时间管理器
/// 注：使用全局时间管理器时，需要再全局的主循环处更新时间。
/// </summary>
public sealed class TimeMgr : GTime
{
#if UNITY_2021_3_OR_NEWER
    private long _serverTime;
    private long _timeElapsed;

    /// <summary>
    /// 全局单例，方便客户端业务编码
    /// </summary>
    public static TimeMgr Inst { get; set; }

    /// <summary>
    /// 最新服务器时间
    /// </summary>
    public long ServerTime => _serverTime + _timeElapsed;

    /// <summary>
    /// 设置服务器的初始时间
    /// </summary>
    /// <param name="serverTime">最新服务器时间</param>
    public void SetServerTime(long serverTime) {
        _serverTime = serverTime;
        _timeElapsed = 0;
    }

    /// <summary>
    /// 更新服务器时间
    /// </summary>
    public void ServerUpdate() {
        _timeElapsed = (long)(UnityEngine.Time.realtimeSinceStartupAsDouble * 1000f);
    }
#endif
}
}