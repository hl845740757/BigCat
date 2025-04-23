# RPC代码生成工具

## 功能

1. 根据代码的注解生成对应的Proxy
2. 根据Proto文件生成对应的Service和Proxy

PS：我在想，到底还要不要支持根据注解反射生成代码，因为反射会导致依赖业务模块包，不是个好现象。

## Protobuf文件规范

[proto文件规范](../../docs/Protobuf.md)