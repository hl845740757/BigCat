# README

## 目录结构说明

1. `data_script` 存放数据脚本源文件，包括excel和编辑器依赖的所有数据结构定义
2. `protobuf` 存放pb协议文件源文件，单机项目可能不使用
3. `excel` 存放excel文件，excel文件统只有第一个页签是数据页，其它页签是辅助页签（注释）
4. `editor_assets` 存放编辑器资产文件(非ScriptableObject文件)，如行为树编辑器数据，场景编辑器数据，通常为Dson文本
5. `editor_out` 存放编辑器导出的文件，比如行为树数据、场景数据，通常为Dson文本或Dson二进制文件；单机项目可能直接导出到`Assets/Resources`目录下。

在网络游戏项目中，这里的许多项目都应该放在公共仓库中；但目前我不打算做网游，因此直接放在客户端unity目录下。