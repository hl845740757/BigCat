# DSON-APT

Dson.Apt是为Dson.Codec提供的工具，用于生成目标类的编解码类（源代码），以避免运行时的反射开销。
由于C#的编译时源码生成器尚不成熟，使用案例甚少，因此Dson.Apt是基于反射分析类型的，因此尽量避免循环依赖 -- 目标Bean最好是独立的Assembly。

PS：Dson.Apt的最佳应用是`Btree.Codec`模块，行为树的所有Codec都是通过Apt自动生成的，而非手动编写的。