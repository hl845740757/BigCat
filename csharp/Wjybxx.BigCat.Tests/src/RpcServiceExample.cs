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

using System.Collections.Generic;
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject.Attributes;

namespace Wjybxx.BigCat.Tests
{
[RpcService(ServiceId = 11)]
public class RpcServiceExample : EventLoopModule, IExtensibleService
{
#nullable disable
    [Inject] private IRpcClient rpcClient;
    private readonly Dictionary<string, object> extBlackboard = new();
#nullable restore

    #region rpc

    [RpcMethod(MethodId = 1, ArgSharable = true, ResultSharable = true)]
    public Response Echo(Request request) {
        return new Response()
        {
            StringVal = request.String1
        };
    }

    [RpcMethod(MethodId = 2, ArgSharable = true, ResultSharable = true, CustomData = "{interval : 500}")]
    public Response Hello(Request request) {
        return new Response()
        {
            StringVal = request.String1
        };
    }

    /** 测试void返回值 */
    [RpcMethod(MethodId = 3, ArgSharable = true, ResultSharable = true)]
    public void Hello2(Request request) {
    }

    /** 测试异步返回 */
    [RpcMethod(MethodId = 4, ArgSharable = true, ResultSharable = true)]
    public IFuture<Response> HelloAsync(Request request) {
        Response response = new Response()
        {
            StringVal = request.String1
        };
        return Promise<Response>.FromResult(response);
    }

    /** 测试异步返回 */
    [RpcMethod(MethodId = 5, ArgSharable = true, ResultSharable = true)]
    public ValueFuture<Response> HelloAsync2(Request request) {
        Response response = new Response()
        {
            StringVal = request.String1
        };
        return ValueFuture<Response>.FromResult(response);
    }

    /** 测试context的代码生成 -- 参数和结果都不设置为可共享的，测试反序列化 */
    [RpcMethod(MethodId = 6)]
    public void ContextHello(ref RpcContext<Response> rpcContext, Request request) {
        rpcClient.Send(rpcContext.RemoteAddr, RpcClientExampleProxy.OnMessage(Request.OfString("context -- before")));
        {
            Response response = new Response()
            {
                StringVal = request.String1
            };
            rpcContext.SendResult(response);
        }
        rpcClient.Send(rpcContext.RemoteAddr, RpcClientExampleProxy.OnMessage(Request.OfString("context -- end\n")));
    }

    #endregion

    public Dictionary<string, object> ExtBlackboard => extBlackboard;

    public IExtensibleService.ExecuteResult Execute(IExtensibleService.ExecuteRequest request) {
        return new IExtensibleService.ExecuteResult();
    }
}
}