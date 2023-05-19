## BigCat

BigCat(大猫)是一个游戏工具和MMO框架项目，项目的目标是像大型猫科动物一样优秀！
高代码质量，高运行性能，高开发效率 -- 这也许将是你见过的代码质量最好的项目！

### 项目划分

项目在最高层将分为4个子项目，分别为：support、commons、tools、framework

1. support是必须先安装为jar包的工具包，主要是注解处理器 -- support不能和其它项目同时打开编译，会产生错误。
2. commons是基础工具包，是运行时需要的包；Commons也分多个模块，可选择性依赖。
3. tools是辅助工具包，是开发期间使用的，比如：导表工具、协议预处理工具等。**其它项目都不直接依赖tools，只依赖它产生的文件。**
4. framework框架包，是游戏相关的部分。

### 如何编译该项目

1. 该项目的4个子项目需要分别独立编译。
2. 进入support项目，clean install 安装apt到本地maven仓库，卸载support项目，不可与其它项目一开编译。
3. 进入commons项目，可正常开始编译 -- 如果之前出错导致无法编译，请先clean清理缓存。
4. 进入framework或tools项目，可正常开始编译。

PS：我现在是在根目录下打开项目，编写apt时将support项目加载进来，安装apt以后卸载support项目(unlink)。

Q：编译报生成的XXX文件不存在？  
A：请先确保support项目安装成功，如果已安装成功，请仔细检查编译输出的错误信息，通常是忘记getter等方法，修改错误后先clean，然后再编译。

Q：编译成功，但文件曝红，找不到文件？  
A：请将各个模块 target/generated-sources/annotations 设置为源代码目录（mark directory as resources root）。

### 项目主要内容

1. 基础工具集：Future、EventBus、Rpc、序列化、热更新、状态机、行为树....
2. 主循环框架
3. 角色和场景框架，技能框架...

### 已实现

1. Future和EventLoop - concurrent包，包含Disruptor高性能事件循环。
2. Rpc + 注解处理器 - [关于Rpc的设计解释](https://github.com/hl845740757/BigCat/blob/dev/doc/Rpc.md)
3. EventBus + 注解处理器
4. Dson序列化 - [Dson是什么](https://github.com/hl845740757/BigCat/blob/dev/doc/Dson.md)
5. 表格对象和读取Excel工具

### 资料

1. 代码：本人(wjybxx)对代码质量有较高的要求，代码就是第一资料。在存在特殊设定的地方，你多数时候都可以看见关于我的思路和意图的注释，
   不过可能不会太详尽，因为部分设计是难以简单解释的。另外，测试用例也值得阅读，既有测试正确性的用例，也有工具使用方式的用例。
2. 文档：在顶层目录有一个Doc文件夹，对一些特殊的设计进行了解释，比如：Rpc。
3. 书籍：本人在21年底的时候有计划写一本游戏开发相关的书，不过到现在(2023.3.31)
   都还未开始，只拟了一个目录。后续真正创作的时候，将会包含该项目中的一些设定的详细解释。  
   (其实，书籍的主要内容其实来源于我这几年的笔记，超1000篇，思考了太多问题，项目是笔记落地的体现)

## 其它

### 项目前身

项目的前身为fastjgame(2019.5-2021.3，现已private)，当初也是想写一个开发工具集和框架，但实际上更像是用于学习和实验的一个仓库，原因如下：

1. 部分组件的实现有很明显的模仿性质（尤其Netty），缺少自己的思想。
2. 部分设计脱离了实际业务，重的是技术而不是实用性 -- 一心造多线程的服务器。
3. 那时我对角色系统、技能、AI还未有好的解决方案。

不过，fastjgame还是实现了一些好的工具，将会迁移到该仓库，或直接迁移或进行优化后迁移，这包括：

1. 注解处理器
2. Rpc
3. Future和EventBus
4. 序列化（Pb二进制和Bson文档）
5. 代码和表格热更新
6. 事件循环

回过头来，我认为fastjgame最优秀的点在于注解处理器(apt)
，注解处理器减少大量的样板代码，可使得我们的业务代码极为干净，而且无运行时性能开销。  
PS：统计了一下fastjgame项目，震惊自己居然写了那么多类（507个类，57596行代码）...

### 开源

Q：我为什么开源呢？  
A：用一句话表达：我这些年实在太痛苦，我经历过的痛苦希望他人可以避免。  
为什么说痛苦呢？因为游戏行业是一个代码极其闭源的行业。 如果你对游戏开发感兴趣，你会发现几乎没有好的书籍和代码参考学习。  
游戏行业最多的书籍是什么？是引擎相关的书籍，这些书教你怎么使用Unity或Unreal，却没有一个教你如何实现一套好的技能系统，AI系统和角色架构的。
即便是有人出版过一些游戏AI或技能设计的书籍，也都是泛泛而谈，或者方案太差劲。  
（就我看过的书籍而言，我只推荐一本书：*游戏人工智能编程案例精粹*）

我很喜欢玩游戏，DNF的骨灰级玩家，wjybxx就是我的游戏角色 *玩家一不小心*
的缩写。我玩过DNF、剑灵这样优秀的游戏，我从业的时候就很希望能做出这样优秀的游戏，因此我对游戏的技能（战斗）、AI实现十分感兴趣，却无路可学。  
我经历了一些项目，也看过一些开源项目，没有看见让我满意的设计，我直觉上感觉它们不好（甚至糟糕），但不知道正确的设计应该是怎样的（回过头想，这其实是能力不足的体现）。

在工作的前几年，由于还有大量的基础知识要学习（语言、设计模式、多线程、分布式...），这一定程度上分散了我的精力，减轻了痛苦；
但当这些通用技术学到一定程度的时候，还是要面对我感兴趣的这些业务，然而我还是没有头绪，因此更加痛苦（明明学了大量知识，却感觉没有帮助）。  
在最近的这两年里，我思考了太多太多的问题，这两年写的笔记就接近1000篇。现在，我可以清楚地指出所经历的项目的一些设计错误，比如：玩家场景内外数据未分离，属性未分层。

另外一点，我认为要做出好玩的游戏，小团队很重要，但由于技术闭源的原因，大多数团队和公司的技术都是很差的。所以，市场上就表现为：有创意的没技术，有技术的没创意...
所以，我希望开源能对这些小团队有帮助，希望国内多出点好游戏。

### 成都的环境

成都是一个没有内核的网红城市，成都的繁荣是表面的繁荣。

我这两年在成都，个人认为成都的环境很糟糕，成都极缺乏高水平的人才，而大家普遍喜欢休闲的日子，没什么技术氛围。  
现在有些大厂来成都建分公司，为了招人，大幅拉高了薪资水平，看简历稍有经验的，就要20K+，25K+，结果面试发现菜的一比。。。
基础不扎实，多线程不会，游戏关键业务也不会，就没有强项啊，都是哪儿来的自信，梁静茹给的吗？
有些还是所谓的主程，写的还是多线程架构，觉得自己的服务器写得很好，结果连多线程的基础知识都不扎实，这哪是写多线程，是在写bug...

薪资高了，中小公司的生存很是艰难，大公司的野蛮扩张，最终需要小公司承受结果。如果始终难以引进人才，整体的衰败是迟早的事。  

---
Q：为什么叫BigCat(大猫)，而不是Tiger或Lion或其它？  
A：期望项目各方面都能做得比较优秀，因此不使用具体某类动物的名字。

(Markdown语法不是很熟悉，排版什么的后期有空再优化~)
