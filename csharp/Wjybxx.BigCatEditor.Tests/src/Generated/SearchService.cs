
using Wjybxx.Commons.Attributes;
using Wjybxx.BigCat.Fx;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.EditorTest.Generated
{
  [Generated("Wjybxx.BigCatEditor.Generator.Rpc.ServiceGenerator")]
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
  }
}
