# C# BigCat实现

## BigCat

## BigCatEditor

BigCatEditor为开发期工具，不可依赖BigCat下的模块。

Q: 哪些需要定义为独立的程序集？  
A: 只有那些与业务无关，可以多项目共用的工具才建立独立的程序集，比如我们的protobuf解析工具；与项目的工具和编辑器，放一个程序集即可。