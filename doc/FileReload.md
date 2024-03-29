# 文件加载和热更新指南

## 特性

1. 原子性，要么全部更新，要么全部丢弃。
2. 高性能，并行读取无依赖的文件。
3. 顺序一致性，总是按照相同的顺序读取有依赖的文件。
4. 支持监听文件reload事件

## 流程

文件加载可分为**6个大阶段**：

1. 解析文件依赖
2. 读表阶段（并发 + 串行）
3. 构建沙盒FileDataMgr（Assign + Link）
4. 验证文件数据正确性
5. 发布到真实环境
6. 热更新通知

其中，读表阶段又由两个小阶段构成：

1. 并发读取无依赖文件
2. 串行读取有依赖的文件

构建FileDataMgr又由两个步骤构成：

1. 将表格数据发布到FileDataMgr - Assign
2. 链接文件数据 - Link

在以上所有步骤中，只有并发读表阶段是多线程执行，其它阶段都是在主线程执行。

加载流程图如下：
![文件加载流程](https://github.com/hl845740757/BigCat/blob/dev/doc/res/fileload.png)

### 解析文件依赖

解析文件依赖，与文件数据之间的链接紧密相关。 在实际的开发中，表格之间的依赖是常发生的，eg：装备表依赖物品表，那我们是在读表时提前链接呢，还是运行时通过id查询呢？

两种我都经历过，通过id运行时查询可以减少耦合，读表过程更加简单，只需要解析表格自身的内容，缺点是增加了运行时开销，而且大量的根据id查询配置的代码有点丑陋（查一个配置可能要读取多张表格）；
在读表期间进行链接，可以提高运行时性能，也更容易避免数据缺失的情况（遗漏validator校验），而且只需要写一次id查询的代码。

历史项目的框架在表格加载这一块都没有很好的处理，导致团队成员开发起来不是很顺手，主要两个问题：

1. 依赖处理不智能，我们需要再一个全局地方定义文件的加载顺序 -- 在某些场景这是必要的，也是常见的方案。
2. 文件加载流程不够清晰，难插入表格校验和依赖解析钩子。

在该项目中，FileReader可以独立实现，声明自己依赖的文件即可，框架会通过依赖图决定表格的加载顺序 -- 不支持环形依赖。

框架提供了两种链接数据的方式：

1. FileReader 读取表格时立即链接
2. FileDataLinker 延迟链接数据

FileReader的方式可最大程度的保证表格数据不可变（各种final字段），一个很好的指导是：
**配置表应该尽可能使用不可变的数据结构**。  
缺点是读取有依赖的文件时必须串行读取，读表时间增加，另外也无法处理环形依赖。

FileDataLinker的方式更加灵活，可提高读表速度，因为它不要求要链接的文件之间按顺序加载 ——
如果所有的文件都是延迟链接数据，那么所有的表格都是可以在并发阶段读取的。
链接文件之间数据通常是很快的，因此带来的速度提升会很明显。  
缺点是引用字段不能是final的，因此表格数据存在运行时被修改的可能。

项目应该根据自己的实际情况决定选用何种方式，如果项目表格较多，表格依赖复杂，建议使用Linker延迟链接数据。

#### 注意事项

1. 文件之间不能形成循环依赖
   > 依赖分析时能检测出环，如果出现环则抛出异常；如果确实需要环形依赖，则需要一个Linker来延迟链接数据。
   也就是说，至少有一个外键字段不能是**final**的。

2. 在依赖上同优先级的FileReader，通过FileName的定义顺序确定其优先级
   > 这可以保证FileReader总是按照相同的顺序执行。  
   > 之所以根据定义顺序确定优先级，而不是字符串顺序，是因为定义顺序更加直观和可控。

### 读表阶段

读表阶段分为了 并发读取 和 串行读取阶段，为什么要拆分呢？  
在实际的环境下，有依赖的文件还是占小部分(可能不到10%)，而在稍大一点的项目中，
文件数量经常超过500，如果这么多文件1个个串行读取，耗时是非常长的。  
我的第一个项目配置表超过500个，且是串行读取的，电脑配置稍差的同事启服需要将近2分钟...因此并行读取配置文件是很有必要的。

读表阶段建议**完成自身表格的数据校验**，及时抛出错误是有利的，这可以避免你定义较多的validator来校验文件，也可以提前终止热更新。

一个很好的指导：**启服一定要快**。  
虽然做表格的热更新并不困难，但仍然存在部分表格无法热更新的情况，因此重启服务器验证配置的情况时有发生，再加上一些其它的测试需要，
策划和测试们重启本地服务器的频率是很高的，如果工具慢或启服慢，大家将不愿意进行频繁的验证，项目隐藏的问题就越多。

一个更全面的指导：**工具一定要易于使用，且足够快**。
我经常看不惯同事开发的一些工具，要么使用起来不顺手，要么速度太慢，我就会重写...
工具不好，会极大的影响其它同事的工作效率，也就影响了项目的进度和质量。

### 构建沙盒

由于策划的配置文件可能存在错误，也可能本地环境和线上环境不一致，因此热更新时要保证原子性，避免一部分表格更新，一部分更新失败；
因此我们需要构建一个沙盒，在沙盒中进行模拟更新，如果数据正确，再发布到正式环境。

#### 链接

将数据发布到FileDataMgr后，我们开始链接文件的之间的数据，由于是在文件已经读取完成之后链接，因此称之为*延迟链接*。
延迟链接阶段也可以抛出异常，但仅限于依赖的数据不存在时。

#### 非完全隔离沙盒

在我们的实现中，沙盒与外部数据并不是完全隔离的，如果项目使用了延迟链接数据（Linker）方案，
那么可能出现 *外部数据引用待更新的数据* 和 *待更新数据引用正式数据*的情况，是不是看起来破坏了原子性？

首先，之所以不构建完全隔离的沙盒，是因为开销太大，你需要再读取一遍所有的文件，内存和耗时都是不可接受的，所以我们需要换一种方式实现原子性。  
在这里，我们采用了**失败回退**的方式，因此当热更新失败时，我们再回退到正式环境执行一次Link，从而修正正式环境的数据。

#### 注意事项

1. 你不应该让Linker执行链接数据以外的事情。
2. Linker应当保持幂等，相同的输入应当保持相同的结果。
3. Linker之间不应该有依赖。

### 验证

在读取文件内容、链接阶段，都是可以附带一部分数据校验的，但这些校验可能是不完全的；
如果需要更多的校验，可以添加validator，validator会在验证阶段执行。

#### 注意事项

1. 你不应该让validator执行验证数据以外的事情
2. validator应当保持幂等，相同的输入应当保持相同的结果。
3. validator之间不应该有依赖。

### 发布到正式环境

当在沙盒执行热更新成功时，表明数据是正确的，这时可将数据发布到正式环境，其过程与构建沙箱环境相似，
将数据发布到正式的FileDataMgr，然后执行Link。

### 热更新通知

每一个Listener可监听特定的文件，当监听的任一文件发生改变时Listener就会收到通知，通知过程存在以下特征：

1. 在一次更新中，每一个Listener只会被通知一次，当监听的多个文件发生变化时，也只会被通知一次。
2. Listener之间的通知没有顺序保证。

#### 注意事项

1. Listener之间不应该有依赖。
2. Listener的行为应该保持幂等，有利于测试和保证正确性 -- 最好是无状态的。