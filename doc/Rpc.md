## RPC基础设计

这里的rpc设计与市面上的Rpc差异较大，并未追求所谓的标准，而更追求实用性。在传统的RPC中，前后端接口是一致的，这是优点也是缺点。  
优点是指：前后端使用相同的接口签名屏蔽了远程和本地的差异，你可以像调用本地方法一样调用远程方法。  
缺点是指：这对接口的使用者产生了限制，极不灵活，当用户知道这就是一个RPC时（其实大家都知道），那为什么不能想同步执行就同步执行，想异步执行就异步执行呢？  
另外，当存在大量的Rpc调用时，这些框架的性能都很差，因为大量的同步调用 -- 这样的设计就像JVM，最后总是需要开辟特殊的接口来支持特殊的调用。

所以，我认为好的Rpc应该为客户端和服务端提供不同的接口，至少在返回值上要使用不同的类型 -- 通过DSL文件生成双端代码的时候更应该如此。

### 返回值 - 客户端接口应屏蔽服务提供者的执行过程

对于Rpc的客户端来说，并不关心远程是如何实现和执行的，我只想同步或异步的获得执行结果，因此客户端的接口返回值总可以是Future。  
当我们定义接口时，将部分接口声明为返回Future，是因为服务的提供者可能不能立即得到结果，但这个不应该对调用者产生影响，因为这里考虑的只是服务的提供者。  
所以，不论是通过DSL生成双端接口，还是通过服务端的接口为客户端生成接口，**都应该屏蔽服务停供者的执行过程（同步或异步）**。

### 执行 - 客户端接口立即执行是非必须的

在传统的Rpc框架中，我们通过RpcClient获得一个接口实例，然后调用接口的方法，就可以获得结果。这里存在一个大大的问题：
*客户端无法选择执行方式，无法选择是异步执行还是同步执行，以及是否需要结果等*。

这个问题的本质：*客户端和服务器的接口是同构的*。

### 定位 - 需要支持指定服务器

许多的Rpc框架不支持指定服务节点，总是底层通过算法分配服务节点，这是有问题的！许多时候我们都需要特定的服务器来指定请求，这在游戏服务器下非常常见。
框架可以做负载均衡，但如果不能指定发送到指定服务器，那也是不好的框架。

### 我的实现

1. **客户端接口和服务器接口异构**。我们没有为客户端生成Rpc接口，而是生成了一个辅助的打包类（Proxy），仅仅是将用户的请求打包成一个RpcMethodSpec。
2. Proxy的方法参数和服务器接口一致，当服务器接口返回值是Future时，捕获Future的泛型参数作为返回值类型，否则直接使用接口返回值。
3. 用户通过RpcClient执行Proxy打包得到的方法请求，同时指定服务目标信息，以及选择是单项通知、异步调用，还是同步调用。
4. 通过int类型的serviceId和methodId定位被调用方法，可减少数据传输量，而且定位方法更快 -- 放弃了部分兼容性检测。

PS: 相信你体验过后会爱上这种简单有效的设计。

### 代码示例

```java
class RpcServiceExample {
    @RpcMethod(methodId = 1)
    public String hello(String msg) {
        return msg;
    }

    /** 测试异步返回 -- JDK的Future */
    @RpcMethod(methodId = 6)
    public CompletableFuture<String> helloAsync(String msg) {
        return FutureUtils.newSucceededFuture(msg);
    }
}

class RpcClientExample {
    public void test() {
        RpcClient rpcClient = getRpcClient();
        rpcClient.send(ServerNodeId.GAME, RpcServiceExampleProxy.hello("这是一个通知，不接收结果"));
        rpcClient.call(ServerNodeId.GAME, RpcServiceExampleProxy.hello("这是一个异步调用，可监听结果"))
                .thenApply(result -> {
                    System.out.println(result);
                    return null;
                });

        String result = rpcClient.syncCall(ServerNodeId.GAME,
                RpcServiceExampleProxy.helloAsync("这是一个同步调用，远程异步执行"));
        System.out.println(result);
    }

    public enum ServerNodeId implements NodeId {
        GAME,
        AUTH
    }
}
```

## 基于protobuf与客户端Rpc通信

客户端与服务器之间的通信模型和服务器与服务器之间大不相同。服务器之间的调用，**请求和响应基本都是成对的**；但客户端与服务器之间则不同：
**客户端（玩家）的一个操作通常会产生数个返回消息，且消息的顺序有讲究**。

举个栗子：玩家穿戴装备，需要从背包中扣除道具（协议+1），通知穿戴成功（协议+1），刷新属性（协议+1），刷新技能（协议+1）...

正常形式的Rpc只能在方法返回之后通知远程结果，有些项目由于没有解决这个问题，因此客户端与服务器之间不是Rpc通信，而是消息通信。
多数情况下，基于消息通信没有太大的影响，但一旦遇见需要准确监听结果的情况，各种骚操作就出现了...这破坏了代码质量，而且并不总是可靠。

看一下BigCat框架中的解决方案。

### RpcContext

要想支持用户任意时间返回结果给远程调用者，我们需要将服务端执行Rpc时上下文提供给用户，并在上下文中提供接口以供用户返回结果给远程调用者；
另外，Context中还需要提供远程的地址，使得用户可以向远端发送消息或Rpc请求。接口示例如下：

```java
interface RpcContext<V> {

    RpcAddr remoteAddr();

    void sendResult(V result);

    void sendError(int code, String msg);
}


```

用户需要自行控制方法的返回时机时，需要将Context声明为方法的第一个参数，且方法的返回值类型必须声明为void，框架会捕获Context的泛型参数作为客户端代理的返回值类型。  
现在，框架支持两种异步：Context 和 Future。

```java
import java.util.concurrent.CompletableFuture;

interface MyService {
    /** 通过Context任意时间点返回结果 */
    void Test(RpcContext<String> ctx, String arg);

    /** 通过Future的方式异步返回结果 */
    CompletableFuture<String> test(String arg);
}
```

Context的用法可见测试用例**RpcTest2**，这里贴部分代码：

```java
class RpcServerExample {
    /** 测试context的代码生成 */
    @RpcMethod(methodId = 7)
    public void contextHello(RpcContext<String> rpcContext, String msg) {
        rpcClient.send(rpcContext.remoteAddr(), RpcClientExampleProxy.onMessage("context -- before"));
        rpcContext.sendResult(msg);
        rpcClient.send(rpcContext.remoteAddr(), RpcClientExampleProxy.onMessage("context -- end\n"));
    }
}
```

### 引入rpc的开销

首先，对于需要返回结果的rpc调用，网络包通常会增大，因为必须要发送requestId（4~8字节）；但对于无需结果的调用（我称之为通知），可以做到和传统消息通信一样的开销。

在基于消息通信的架构中，我们需要为每个Message分配唯一id，通常是4个字节，然后根据id查找类型，进行解码和派发。
在Rpc中，我们限制ServiceId和MethodId各两个字节，这和消息id的开销一样。在解码时，我们根据ServiceId和MethodId拿到方法的参数类型和返回值类型，即可准确解码。
另外，对于不需要结果的Rpc请求，我们通常可以去除requestId，这可能损失一部分特性（不能全都要是不是？），但对于游戏的客户端和服务器通信而言，通常没有影响。

另外，在大型游戏中，如MMORPG，通常只有场景外的功能系统需要使用Rpc，而这部分的消息占比是比较小的，因此这部分引入Rpc的产生的总体开销不会太高，是值得引入的。

### proto文件定义

proto文件规范见[proto文件规范](https://github.com/hl845740757/BigCat/blob/dev/doc/Protobuf.md)