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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// RPC异常
/// </summary>
public abstract class RpcException : Exception
{
    private readonly int errorCode;

    protected RpcException(int errorCode)
        : base(FormatMessage(null, errorCode)) {
        this.errorCode = errorCode;
    }

    protected RpcException(int errorCode, string? message)
        : base(FormatMessage(message, errorCode)) {
        this.errorCode = errorCode;
    }

    protected RpcException(int errorCode, string? message, Exception? innerException)
        : base(FormatMessage(message, errorCode)) {
        this.errorCode = errorCode;
    }

    private static string FormatMessage(string? message, int errorCode) {
        if (string.IsNullOrWhiteSpace(message)) return "code: " + errorCode;
        return "msg: " + message + ", code: " + errorCode;
    }

    public int ErrorCode => errorCode;
}
}