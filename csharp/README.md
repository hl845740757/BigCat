# C# BigCat实现

## BigCat

## BigCatEditor

BigCatEditor为开发期工具，不可依赖BigCat下的模块，BigCat下的模块也不可依赖BigCat下的模块。

### Core

Core定义所有的公共数据结构和工具类，不含与项目业务相关的代码。

### Generator

Generator是所有的基础代码生成工具，如根据Excel和Protobuf生成代码的生成器。