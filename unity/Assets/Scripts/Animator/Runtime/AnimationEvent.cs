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
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 事件信息
///
/// 注：如果使用Unity内置的序列化，需要使用ScriptableObject才能实现多态，我们并不想将事件数据存储在外部。
/// </summary>
[DsonSerializable]
public sealed class AnimationEvent
{
    public bool enabled = true; // 是否启用
    public float enterTime; // 启动时间
    public float period; // 触发间隔，大于0表示周期触发，需要配置exitTime
    public float exitTime; // 结束时间

    public AnimationEventType type; // 事件类型
    public double doubleParameter; // int, float, long, double
    public string stringParameter; // 字符串参数
    public object objectParameter; // 对象参数(内联)
    public ObjectPtr objectPtr; // 对象参数(引用)
}

/// <summary>
/// 该枚举主要用于支持一些简单的内置事件
/// </summary>
public enum AnimationEventType
{
    Custom = 0, // 默认自定义
    SetSuccess = 1, // 设置任务完成
    SetFailure = 2, // 设置任务失败
    Pause = 3, // 暂停自己
    PauseGraph = 4, // 暂停整个动画图
    SetWeight = 5, // 调整动画的融合权重
    SetLayer = 6, // 调整动画的渲染层级
}
}