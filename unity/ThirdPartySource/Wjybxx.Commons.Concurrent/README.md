# Concurrent模块

1. 提供了Java的Executor和Future框架，并提供了对应的await语法支持。
2. 提供了基于Disruptor的高性能EventLoop实现。
3. 提供了ValueFutureTask -- 类比系统库的ValueTask。

## C#系统并发库缺陷

个人使用C#系统库有几点很难受：

1. 系统库的Task其实是Future，这个名字的误导性很强，概念混淆。
2. await语法不支持显式传参，回调线程是根据ThreadLocal的【同步上下文】（SyncContext）确定的；await还会隐式捕获【执行上下文】（ExecutionContext）；
3. await隐式捕获上下文，导致的结果是：**简单的问题更加简单，复杂的问题更加复杂**。
4. Task不支持死锁检测
5. TaskCompletionSource泛型类和非泛型类之间是非继承的，我们确实统一的Api获取结果和取消任务。

## ReleaseNotes

### 1.2.1

升级commons.core依赖

### 1.2.0

C#的异常派发机制和java不同，之前的future异常处理是按照java写的，导致异步任务的异常信息堆栈丢失。

### 1.1.1 ~ 1.1.2

1. `DisruptorEventLoop`适配`Disruptor`模块的等待超时修改。
2. fix `ValueFuture`的await错误。
3. 增加 `GlobalEventLoop`和`ManualResetPromise`。