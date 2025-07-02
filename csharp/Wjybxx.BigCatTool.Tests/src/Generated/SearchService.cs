using Wjybxx.Commons.Attributes;
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCatTool.Tests.Generated
{/// <summary>
/// @Rpc{id: 1}
/// </summary>
[Generated("Wjybxx.BigCatTool.Generator.Protobuf.ServiceGenerator")]
[RpcService(ServiceId = 1)]
public interface SearchService
{
    /// <summary>
    /// @Rpc {id: 1}
    /// </summary>
    [RpcMethod(MethodId = 1)]
    SearchResponse Search(SearchRequest request);

    /// <summary>
    /// @Rpc{id: 2, async: true, ctx: true}
    /// @RpcCustom{interval: 500}
    /// </summary>
    [RpcMethod(MethodId = 2, CustomData = "{interval: 500}")]
    ValueFuture<SearchResponse> SearchAsync(ref RpcContext<SearchResponse> rpcCtx, SearchRequest request);

    /// <summary>
    /// 以下两种形式不符合proto的rpc语法，但符合我们的约定
    /// @Rpc{id: 3, async: false}
    /// </summary>
    [RpcMethod(MethodId = 3)]
    SearchResponse Search3(SearchRequest request);

    /// <summary>
    /// @Rpc{id: 4, async: false}
    /// </summary>
    [RpcMethod(MethodId = 4)]
    void Search4();

    /// <summary>
    /// 最新特殊支持，支持直接在方法后面指定方法id，支持用冒号':'代替returns
    /// @Rpc{async: false}
    /// </summary>
    [RpcMethod(MethodId = 5)]
    void Search5();
}
}
