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

using Wjybxx.Commons;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 该接口用于解除同步调用对{@link RpcResponse}的依赖
/// </summary>
public readonly struct RpcResult
{
    private readonly int errorCode;
    private readonly object? data;

    public RpcResult(int errorCode, object? data) {
        this.errorCode = errorCode;
        this.data = data;
    }

    /** 结果转String，只有失败的情况下可调用 */
    public string? ErrorMsg {
        get {
            if (errorCode == 0) {
                throw new IllegalStateException("errorCode == 0");
            }
            return (string)data;
        }
    }

    /** 是否成功 */
    public bool IsSucceeded => errorCode == 0;
    /** 是否失败 */
    public bool IsFailed => errorCode != 0;

    #region props

    public int ErrorCode => errorCode;

    public object? Data => data;

    #endregion
}
}