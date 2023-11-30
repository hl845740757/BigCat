# Protobuf

## Protobuf文件规范

1. 字段和方法必须在单行内完成定义，不可以换行
2. Service、Message、Enum的结束符`}`需要换行
3. Service内不可嵌套定义Message，Service需要保持独立
4. 文件名、顶层消息名、服务名不可重复 -- 方便定位
5. Rpc方法只可引用**顶层消息类**，不可引用嵌套消息
6. 仅支持单行注释`//`，不支持多行注释块`/** */`
7. 注释行与要注释的元素之间不可有空行，行尾注释有效。

ps：

1. 这些限制主要为了降低解析的复杂度。
2. 更完整的proto示例文件见`testres`目录下的`test.proto`文件
3. 测试用例见`PBParserTest`类文件
4. 建议使用proto3

## 示例

```
    //@RpcService {id: 1}
    service HelloServer { // '{'可换行可不换行
    
      //服务端同步返回结果
      //@RpcMethod {id: 1, mode: 0}
      rpc Hello(HelloRequest request) returns (HelloResponse); // 分号是必须的
      
      // 服务端需要异步完成
      // @RpcMethod {id: 2, mode: 1}
      rpc HelloAsync(HelloRequest request) returns (HelloResponse);
      
    } // '}'必须换行
```

## 元注解

我们通过特殊的注释为pb元素添加元信息，其格式为：`//@Type {}`，其中：

* `//@`表示该行注释为元注解，`//`与`@`之间可有空格
* `Type`表示元注解的类型
* `{}`内为注解的内容，为`Dson`格式；`Type`与`{}`之间的空格不是必须的

## Rpc服务

服务的定义语法与Protobuf定义服务的语法基本一致，但存在以下区别：

1. 支持方法重载 -- 我们的rpc是根据id定位的，命名可重复

若使用以上特性，则不符合proto的标准语法，proto文件插件会报语法异常。

服务的元注有三个，分别为：

```
    //@RpcService {id: 1}
    //@Sparam {}
    //@Cparam {}
```

* id表示为服务分配的id。
* Sparam 为服务端用参数，dson格式；单独成行，利于解析和书写
* Cparam 为客户端用参数，dson格式

### 限制

1. id区间为 \[-32767, 32767]，即2字节内，且可以转正值

## Rpc方法

方法的定义与protobuf定义方法的语法大体相似，但存在以下区别：

1. 支持无参和无返回值。
2. 支持参数名。

若使用以上特性，则不符合proto的标准语法，proto文件插件会报语法异常。

方法的元注解有三个，分别为：

```
    //@RpcMethod {id: 1, mode: 1, ctx: true}
    //@Sparam {}
    //@Cparam {}
```

* id 表示方法在服务内的id
* mode 表示服务端接口的模式；默认值为0（解析器指定）
* ctx 表示在**非Ctx异步模式**下是否需要**RpcGenericContext**参数；默认值为false
* Sparam 为服务端用参数，dson格式；单独成行，利于解析和书写
* Cparam 为客户端用参数，dson格式

mode枚举：

* 0 表示普通模式，接口和pb中定义一致
* 1 表示异步模式，服务端接口的返回值将包装为Future
* 2 表示Ctx异步模式，服务端接口的第一个参数为RpcContext，且方法的返回值为void

### 限制

1. 方法**至多有一个参数**，类型仅支持Message类型
2. 方法**至多有一个返回值**，类型仅支持Message类型。
3. id区间为 \[0, 9999]

## 指导

1. 服务的命名应具备一定的规则，规则的命名可减少大量的配置
2. serviceId要好好规划，合理的serviceId分配有助于拦截器测试上下文
3. 不建议使用proto的标准rpc方法定义，不支持无参和无返回值类型有一定不便。
4. 是否使用方法重载，视项目使用的语言而定

## 排错

在protoc抛出异常时，大家会习惯性的去查看源文件，在这里是不行的，或者说要掌握一些额外的规则。

1. 原始proto文件不会直接被protoc编译，protoc编译的是预处理之后的临时文件，所以排错可能要查看临时文件。
2. Service定义会被注释掉，但仍会写入临时文件，以方便排错。
3. 生成的**临时文件与原文件的差异仅在头部**，而为了尽可能减少文件误差，parser提供了行填充设置，**允许将X行固定为文件首部**。
