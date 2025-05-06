# C# BigCat实现

## BigCat

## BigCatEditor

BigCatEditor为开发期工具，可以依赖BigCat中的模块（程序集）—— 通常用于代码生成，但BigCat中的模块一定不能依赖Editor下的模块。

### Core

Core定义所有的公共数据结构和工具类，不含与项目业务相关的代码。

### Generator

Generator是所有的基础代码生成工具，如根据Excel和Protobuf生成代码的生成器。