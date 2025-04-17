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
using Wjybxx.Commons.Ex;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// Rpc服务端异常
/// </summary>
public class RpcServerException : RpcException
{
    public RpcServerException(int errorCode) : base(errorCode) {
    }

    public RpcServerException(int errorCode, string? message) : base(errorCode, message) {
    }

    public RpcServerException(int errorCode, string? message, Exception? innerException) : base(errorCode, message, innerException) {
    }

    /// <summary>
    /// 不填充堆栈，没有意义（因为错误信息是远端的）
    /// </summary>
    public override string? StackTrace => null;

    public static Exception NewServerException(int errorCode, string? errorMsg) {
        if (RpcErrorCodes.IsUserCode(errorCode)) {
            return new ErrorCodeException(errorCode, errorMsg);
        } else {
            return new RpcServerException(errorCode, errorMsg);
        }
    }
}
}