# Dson Csharp

[Dson文本语法](https://github.com/hl845740757/commons/blob/dev/docs/Dson.md)

## 模块说明

1. Dson.Core: Core模块提供了Dson的文本和二进制流编解码实现。
2. Dson.Codec: Codec模块提供了基于Dson的对象序列化实现(ORM)。
3. Dson.Apt: Apt模块处理Codec模块约定的注解，为目标类生成编解码类(Codec)。

## 简单使用指南

这里只介绍Dson-Core模块的功能，有关对象序列化的说明请跳转`Dson.Codec`模块。

### Dsons工具类

为方便用户使用，我提供了**Dsons**工具类，提供了大量的读写Dson的快捷API。

注意：FromDson默认只读取第一个对象。

```
     DsonObject<String> dsonObject = Dsons.FromDson(dsonString).AsObject();
     String dsonString1 = Dsons.ToDson(dsonObject, ObjectStyle.Indent);
     Console.WriteLine(dsonString1);
```

### 解析引用(DsonRepository)

Dsons中的方法默认不解析引用，库提供了简单解析引用的工具类*DsonRepository*。

方式1：FromDson的时候解析引用。

```
    DsonRepository repository = DsonRepository.FromDson(reader, true);
```

方式2：需要的时候解析引用。该方式支持手动构建repository。

```
    DsonRepository repository = DsonRepository.FromDson(reader);
    repository.ResolveReference();
```

## C#库特性

解析规则不分语言，因此reader的实现应该保持一致，~~也就不存在特别的特性~~。
但书写格式各个库的实现可能并不相同，因此writer可能有所差异；不过Csharp的代码也由我编写，因此具备与Java相同的特性。

### Reader

#### 投影(Projection)

我在V1.4版本为Java和C#增加了投影功能；一开始仅仅是想截取Dson文本中的某块，避免全量解析，后来想起了MongoDB的投影功能，
投影是比获取分块更完备的功能，花了2天工夫实现了一版，不过没做语法简化。

### Writer

#### 全局设置

1. 支持行长度限制，自动换行
2. 支持关闭纯文本模式
3. 支持ASCII不可见字符转unicode字符输出
4. 支持无行首打印，即打印为类json模式

一般不建议开启unicode字符输出，我设计它的目的仅仅是考虑到可能有跨语言移植的需求。
支持关闭纯文本模式，是为了与ASCII不可见字符打印为unicode字符兼容。

#### NumberStyle

Number提供了9种默认格式输出：

1. SIMPLE 简单模式 —— 普通整数和小数格式，科学计数法
2. TYPED 简单类型模式 —— 在简单模式的基础上打印类型
3. UNSIGNED 将整数打印为普通无符号数 -- 不支持浮点数
4. TYPED_UNSIGNED 将整数打印为普通无符号数，同时打印类型 -- 不支持浮点数
5. SignedHex 有符号16进制，负数会输出负号 -- 不支持浮点数
6. UnsignedHex 无符号16进制，负数将打印所有位 -- 不支持浮点数
7. SignedBinary 有符号2进制，负数会输出负号 -- 不支持浮点数
8. UnsignedBinary 无符号2进制，负数将打印所有位 -- 不支持浮点数
9. FixedBinary 固定位数2进制，int32打印为32位，int64打印为64位

注意：

1. 浮点数不支持二进制格式
2. 浮点数是NaN、Infinite或科学计数法格式时，简单模式下也会打印类型
3. 浮点数的16进制需要小心使用，需先了解规范
4. 因为数字的默认解析类型是double，因此int64的值大于double的精确区间时，将自动添加类型
5. 允许用户添加自己的Style

#### StringStyle

字符串支持以下格式：

1. AUTO 自动判别
    1. 当内容较短且无特殊字符，且不是特殊值时不加引号
    2. 当内容长度中等时，打印为双引号字符串
    3. 当内容较长时，打印为文本模式
2. AUTO_QUOTE —— 与AUTO模式相似，但不启用文本模式
3. QUOTE 双引号模式
4. UNQUOTE 无引号模式 —— 常用于输出一些简单格式字符串
5. TEXT 文本模式 —— 常用于输出长字符串
6. SIMPLE_TEXT 简单文本模式 —— 输出常量字符串，保持原始格式
7. STRING_LINE 单行字符串 —— 输出不含换行符，但含其它特殊字符的短字符串

#### ObjectStyle

对象(object/array/header)支持两种格式：

1. INDENT 缩进模式(换行)
2. FLOW 流模式

属性较少的对象适合使用Flow模式简化书写和减少行数，同时数据量较大的Array也很适合Flow模式，
可以很好的减少行数。

PS：其实Writer的目标就是尽可能和我们的书写格式一致。

#### 文本左对齐

在新的版本中，我增加行首缩进和纯文本左对齐功能，运行`DsonTextReaderTest2`测试用力，我们得到以下文本。

```
   {@{clsName: MyClassInfo, guid: 10001, flags: 0}
     name: wjybxx,
     age: 28,
     pos: {@{Vector3}
       x: 0,
       y: 0,
       z: 0
     },
     address: [
       beijing,
       chengdu
     ],
     intro: 
     @"""
     @-   我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，你可以通过gi
     @- thub联系到我。
     @|   thanks
     """,
     url: "https://www.github.com/hl845740757",
     time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, 
   offset: +08:00}
   }
```