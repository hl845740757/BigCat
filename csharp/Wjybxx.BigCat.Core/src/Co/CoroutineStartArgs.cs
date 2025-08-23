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
using Wjybxx.BigCat.Util;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程启动参数
/// </summary>
public struct CoroutineStartArgs
{
    /// <summary>
    /// 关联的实体id
    /// (会存储在协程对象上)
    /// </summary>
    public long entityId;
    /// <summary>
    /// 函数启动参数
    /// </summary>
    public object startArg1;
    /// <summary>
    /// 函数启动参数
    /// </summary>
    public object startArg2;
}

/// <summary>
/// 协程启动参数
/// </summary>
public struct CoroutineStartArgs<T, R>
{
    /// <summary>
    /// 关联的实体id
    /// </summary>
    public long entityId;
    /// <summary>
    /// 函数启动参数
    /// </summary>
    public object startArg1;
    /// <summary>
    /// 函数启动参数
    /// </summary>
    public object startArg2;

    /// <summary>
    /// 输入参数解码器
    /// </summary>
    public DataKey<T> inputCodec;
    /// <summary>
    /// 结果参数解码器
    /// </summary>
    public DataKey<R> outputCodec;
}
}