using Wjybxx.Commons.Attributes;
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCatTool.Tests.Generated
{
[Generated("Wjybxx.BigCatTool.Generator.Protobuf.ServiceGenerator")]
[RpcService(ServiceId = 1)]
public interface SearchService
{
    [RpcMethod(MethodId = 1)]
    SearchResponse Search(SearchRequest request);

    [RpcMethod(MethodId = 2)]
    ValueFuture<SearchResponse> SearchAsync(ref RpcContext<SearchResponse> rpcCtx, SearchRequest request);

    /// <summary>
    /// 以下两种形式不符合proto的rpc语法，但符合我们的约定
    /// </summary>
    [RpcMethod(MethodId = 3)]
    SearchResponse Search3(SearchRequest request);

    [RpcMethod(MethodId = 4)]
    void Search4();

    /// <summary>
    /// 最新语法，支持直接在方法后面指定方法id
    /// </summary>
    [RpcMethod(MethodId = 5)]
    void Search5();
}
}
