### RPC

这里的rpc设计与市面上的Rpc差异较大，并未追求所谓的标准，而更追求实用性。在传统的RPC中，前后端接口是一致的，这是优点也是缺点。  
优点是指：前后端使用相同的接口签名屏蔽了远程和本地的差异，你可以像调用本地方法一样调用远程方法。  
缺点是指：这对接口的使用者产生了限制，极不灵活，当用户知道这就是一个RPC时（其实大家都知道），那为什么不能想同步执行就同步执行，想异步执行就异步执行呢？  
另外，当存在大量的Rpc调用时，这些框架的性能都很差，因为大量的同步调用 -- 这样的设计就像JVM，最后总是需要开辟特殊的接口来支持特殊的调用。

所以，我认为好的Rpc应该为客户端和服务端提供不同的接口，至少在返回值上要使用不同的类型 -- 通过DSL文件生成双端代码的时候更应该如此。

#### 返回值 - 客户端接口应屏蔽服务提供者的执行过程

对于Rpc的客户端来说，并不关心远程是如何实现和执行的，我只想同步或异步的获得执行结果，因此客户端的接口返回值总可以是Future。  
当我们定义接口时，将部分接口声明为返回Future，是因为服务的提供者可能不能立即得到结果，但这个不应该对调用者产生影响，因为这里考虑的只是服务的提供者。  
所以，不论是通过DSL生成双端接口，还是通过服务端的接口为客户端生成接口，**都应该屏蔽服务停供者的执行过程（同步或异步）**。

#### 执行 - 客户端接口立即执行是非必须的

在传统的Rpc框架中，我们通过RpcClient获得一个接口实例，然后调用接口的方法，就可以获得结果。这里存在一个大大的问题!  
由于接口直接执行了请求，**使用者就无法决定这个请求的目标**
，这在某些场合是不满足的需求的，比如：客户端就希望特定的服务器执行请求，这在游戏服务器下非常常见。  
框架可以做负载均衡，但如果不能指定发送到指定服务器，那也是不好的框架。

#### 我的实现

1. 我们没有为客户端生成Rpc接口，而是生成了一个辅助的打包类（Proxy），仅仅是将用户的请求打包成一个RpcMethodSpec。
2. Proxy的方法参数和服务器接口一致，当服务器接口返回值是Future时，捕获Future的泛型参数作为返回值类型，否则直接使用接口返回值。
3. 用户通过RpcClient执行Proxy打包得到的方法请求，同时指定服务目标信息，以及选择是单项通知、异步调用，还是同步调用。
4. 通过short类型的serviceId和methodId定位被调用方法，可减少数据传输量，而且定位方法更快 -- 放弃了部分兼容性检测。

PS: 相信你体验过后会爱上这种简单有效的设计。

#### 代码示例

```java
class RpcServiceExample {
    @RpcMethod(methodId = 1)
    public String hello(String msg) {
        return msg;
    }

    /** 测试异步返回 -- JDK的Future */
    @RpcMethod(methodId = 6)
    public CompletableFuture<String> helloAsync1(String msg) {
        return FutureUtils.newSucceededFuture(msg);
    }
    
    /** 测试异步返回 -- 单线程Future */
    @RpcMethod(methodId = 7)
    public FluentFuture<String> helloAsync2(String msg) {
        return SameThreads.newSucceededFuture(msg);
    }
}

class RpcUserExample {
    public void rpcTest() throws Exception {
        RpcClient rpcClient = getRpcClient();
        rpcClient.send(ServerNodeSpec.GAME, RpcServiceExampleProxy.hello("这是一个通知，不接收结果"));
        rpcClient.call(ServerNodeSpec.GAME, RpcServiceExampleProxy.hello("这是一个异步调用，可监听结果"))
                .thenApply(result -> {
                    System.out.println(result);
                    return null;
                });

        String result = rpcClient.syncCall(ServerNodeSpec.GAME,
                RpcServiceExampleProxy.helloAsync2("这是一个同步调用，远程异步执行"));
        System.out.println(result);
    }

    public enum ServerNodeSpec implements NodeSpec {
        GAME,
        AUTH
    }
}
```