# C# BigCat实现

## 编译问题

BigCat项目默认通过Nuget依赖Commons项目中的模块，但作者不可能Commons稍有改动就上传到Nuget，因此开发期间多使用本地Nuget仓库。
如果大家编译项目出现错误，可以下载Commons仓库，本地打包后添加到Nuget路径。

## 核心架构介绍

### 线程进程架构

![线程架构](../docs/res/rpc_t0.png)

### Rpc消息流转

![进程间Rpc](../docs/res/rpc_t2.png)

------------------------------------------------

## 模块划分

1. `Wjybxx.BigCat.Apt` 为注解处理器工具，在编译时执行生成静态代码 -- 非运行时代码。
2. `Wjybxx.BigCat.XXX` 为运行时代码
3. `Wjybxx.BigCatTool.XXX` 为开发期工具代码，主要包含表格和协议处理工具，代码生成工具...
4. `Wjybxx.BigCatEditor.XXX`为开发期编辑器代码，主要包含数据编辑器和场景编辑器...

约束：

1. 运行时代码不可以依赖Editor相关的任何代码，编辑器的作用在于导出运行时需要的配置。
2. Tool和Editor并未拆分为独立的solution，但应尽可能避免依赖运行时的程序集。