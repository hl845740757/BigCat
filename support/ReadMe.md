# 所有项目依赖的基础模块

## annotations模块

annotations模块存储最基础的一些注解，这些注解被其它commons模块依赖，也被apt模块依赖。

## apt-base模块

所有apt模块都依赖的基础模块，这里实现了apt的基础流程管理和apt的基础工具类。  
apt-base的最大目的在于支持跨项目复用组件。

## common-apt

服务于所有commons的注解处理器