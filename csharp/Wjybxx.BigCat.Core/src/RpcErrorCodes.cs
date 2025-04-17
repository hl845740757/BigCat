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

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// rpc错误码
/// </summary>
public static class RpcErrorCodes
{
    /** 调用成功的错误码 */
    public const int SUCCESS = 0;
    // 1 - 10 为特殊状态码，应用层不可见

    // 11 - 30 表客户端异常
    /** 本地发生了未知错误 */
    public const int LOCAL_UNKNOWN_EXCEPTION = 11;
    /** 超时 */
    public const int LOCAL_TIMEOUT = 12;
    /** 本地反序列化请求或结果失败 */
    public const int LOCAL_DESERIALIZE_FAILED = 13;
    /** 发包会话不存在 */
    public const int LOCAL_SESSION_NOT_EXIST = 14;
    /** Session关闭 */
    public const int LOCAL_SESSION_CLOSED = 15;

    // 31 - 50 表服务器异常
    /** 表示服务器调用出现异常的错误码 */
    public const int SERVER_UNKNOWN_EXCEPTION = 31;
    /** 不支持的接口调用 -- 或是服务id非法，或是方法id非法 */
    public const int SERVER_UNSUPPORTED_INTERFACE = 32;
    /** 服务端反序列化请求失败 */
    public const int SERVER_DESERIALIZE_FAILED = 33;
    /** 服务端检测到请求过期 */
    public const int SERVER_REQUEST_EXPIRED = 34;
    /** 会话不存在 */
    public const int SERVER_SESSION_NOT_EXIST = 35;
    /** Worker不存在 */
    public const int SERVER_WORKER_NOT_EXIST = 36;

    /// <summary>
    /// 判断错误码是否属于用户命名空间
    /// </summary>
    /// <param name="code">错误码</param>
    /// <returns>如果是用户空间错误码，则返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUserCode(int code) {
        return code > 100;
    }
}
}