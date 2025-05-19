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

using System.Runtime.CompilerServices;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Rpc调用类型枚举
/// 注意：在写入最终网络包时，建议将invokeType和<see cref="RpcResponse"/>平铺，共用枚举。
/// </summary>
public static class RpcInvokeType
{
    public const int ONEWAY = 1;
    public const int CALL = 2;
    public const int SYNC_CALL = 3;
//    public const int RESPONSE = 4; // 写入网络包时，RpcResponse为4

    /** 是否是消息 -- 远程不需要结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMessage(int type) {
        return type == ONEWAY;
    }

    /** 是否是调用 -- 远程需要结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCall(int type) {
        return type == CALL || type == SYNC_CALL;
    }
}
}