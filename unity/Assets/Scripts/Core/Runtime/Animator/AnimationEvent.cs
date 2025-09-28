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
using Wjybxx.BigCat.UnityCore;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 事件信息
///
/// 注：如果使用Unity内置的序列化，需要使用ScriptableObject才能实现多态，我们并不想将事件数据存储在外部。
/// </summary>
[Serializable]
public sealed class AnimationEvent
{
    public bool enabled = true; // 是否启用
    public float enterTime; // 启动时间
    public float period; // 触发间隔，大于0表示周期触发，需要配置exitTime
    public float exitTime; // 结束时间

    public AnimationEventType type; // 事件类型
    public double doubleParameter; // int, float, long, double
    public string stringParameter; // 字符串参数
    [NonSerialized]
    public object objectParameter; // Unity不能指向自定义对象，因此作为运行时查询缓存字段
    public ObjectPath objectPath; // 目标对象路径
}

/// <summary>
/// 该枚举主要用于支持一些简单的内置事件
/// </summary>
public enum AnimationEventType
{
    Custom = 0, // 默认自定义
    SetSuccess = 1, // 设置任务完成
    SetFailure = 2, // 设置任务失败
    Pause = 3, // 暂停自己，double参数为暂时时间，大于0有效
    PauseGraph = 4, // 暂停整个动画图，double参数为暂时时间，大于0有效
    PlaySound = 5, // 播放音效，string参数为音效路径
}
}