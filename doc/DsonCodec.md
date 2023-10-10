# Dson序列化

该模块提供了基于Dson的序列化简单实现。

## Dson与Json、Bson、Protobuf的比较

### Dson与Json和Bson

Json的数据格式是简单清晰的，但不是自解释的，即不能表达它是由什么数据序列化而来。
Bson在Json的基础之上进行了一定的改进，设计了值类型，生成的Json也是特殊的，在反序列化时能较Json更精确一些；
但这仍然不够，Bson的文档和Json的对象一样，不是自解释的（缺少自描述信息），因此在反序列化时无法精确解析，只能解析为Document。

对于Document，要实现精确的解析，我们可以在Document里存储一些特殊的数据以表达其类型 -- 轻度污染；
对于Array，则没有办法，因为在Array里插入额外数据是危险的，数组元素个数的改变是危险的 -- 重度污染。  
简单说，Json和Bson在设计之初并没有很好的考虑反序列化的问题，因此不适合做复杂情况下的序列化组件。

Dson为Array和Object设计了一个对象头，用于保存其类型信息，由于它是单独存储的，因此不会对数据造成污染。

阅读源码，大家会发现Dson的代码和MongoDB的Bson很像，这是因为我对MongoDB的Bson较为熟悉--前几年研究过Bson和Protobuf的编解码，
于是这两天写Dson的时候参考了Bson的代码，不过我们在许多地方的设计仍然是不同的，我相信你用Dson的Reader和Writer会更舒服。

### Dson与Protobuf

Q：为什么不使用Protobuf序列化？  
A：Protobuf是很好的序列化工具，但它也存在一些问题：

1. 必须定义Proto文件。
2. 不能序列化自定义类，必须定义Message，然后通过Builder进行构建。
3. 不支持继承多态 -- 我们只能编码为bytes或展开为标签类
4. 兼容性问题 -- Protobuf过于兼容了。

#### 定义Proto文件

其实，对于一个跨语言的序列化工具来说，通过DSL文件描述数据结构是必须的，因此这个问题是个小问题；
不过在我们不需要跨语言的时候，维护proto文件就有点让人不爽。

#### 自定义类问题

对于不能序列化自定义类这点，在java端是容易解决的，因为有注解。我们可以通过注解将一个类声明为需要按照Protobuf格式序列化，
然后静态或动态代码生成编解码代码；Protobuf的序列化格式是比较简单的，因此生成代码并不算复杂。
(其实有现成的框架——protostuff)

而对于不能通过工具解决的语言或项目，维护自定义类到Message之间的映射是痛苦的，这需要付出较多的维护成本。  
（当年既要写转Message的代码，还要写MongoDB的Codec的日子真的痛苦...）

#### 继承和多态问题

Protobuf不支持多态（继承），是因为其需要定义schema，而schema要求一切都是明确的，明确的数据可以让编码结果更小。
我们在通过protobuf传输多态对象时，通常使用万能的bytes，将类型信息和对象一起放入bytes，或将其展开为标签类。

如果我们传输的数据通常是简单的，那么使用Protobuf并不会带来太大的影响；但在实际的业务开发中，出现继承的频率是很高的，
这导致我们定义了许多的标签类，让人维护得很是难受。

PS：我见过一些项目，由于序列化的缺陷，导致业务数据结构设计受到掣肘 -- 类似的是，由于SQL数据表的限制，业务数据结构按照表结构设计，
我认为这是不好的，因为依赖关系是反的，这使得你的业务代码很难迁移。

#### 兼容性问题

protobuf的数据非常兼容，以至于发生一些不期望的事情，这与protobuf的编码格式有关，pb的字段编码结果中只包含filedNumber和wireType，
即字段编号和编码格式，而**不包含字段的类型信息**，解码时完全按照接收方的schema进行解码，就可能胡乱解码，产生奇怪的兼容或异常。

不过，PB也正是能省则省才能够达到这么小的包体，在客户端与服务器通信时仍然是首选，在客户端服务器同步维护前，我们通常避免修改字段的类型。  
不过，也正是因为PB的这些问题，PB是不适合做持久化存储的 -- 个人认为用PB持久化（入库），等于给自己挖坑。

PS：DSON在序列化时仍然使用了Protobuf的组件，以压缩数字。

## Dson序列化

Dson支持两套序列化格式，一套以number表示字段id和classId，一套以string表示字段id和classId。  
其中，number版称之为二进制版本，注重的是编解码效率和包体大小；string版称之为文档版本，注重的是可读性和可恢复性。  
其中，二进制版本设计用于服务器之间通信，文档版本用于持久化和导入导出配置文件。

二进制版和文档版的编解码格式几乎是相同的，唯一的差别就是二进制版本通过数字表达value关联的字段和class，
而文档版本通过字符串表达value关联的字段和class。

### Dson的特性

Dson有许多强大的特性，你如果只是简单使用Dson，那和普通的序列化组件差不多，可能还没那么方便，因为要做点准备工作；
如果与Dson深度集成，Dson将提供许多强大的功能。

提示：

1. 下面的代码片段来自test目录下的 *CodecBeanExample* 类。
2. numbers和names字段都是*编译时常量*，编译时将直接内联，因此不会有编解码时的访问开销。
3. 测试目录下codec包中包含了许多有用的测试用例，建议阅读。

#### 默认值可选写入

对于基础类型 int32,int64,float,double,bool，可以通过 *Options.appendDef* 控制是否写入写入默认值；
对于引用类型，可以通过 *Options.appendNull* 控制是否写入null值。

如果数据结构中有大量的可选属性（默认值），那么不写入默认只和null可以很好的压缩数据包。

#### 指定数字字段的编码格式

Dson集成了Protobuf的组件，支持数字的*varint、unit、sint、fixed*4种编码格式，你可以简单的通过*FieldImpl*注解声明
字段的编码格式，而且*修改编码格式不会导致兼容性问题*，eg：

```
    @FieldImpl(wireType = WireType.UINT)
    public int age;
    
    // 生成的编码代码
    writer.writeInt(CodecBeanExampleSchema.numbers_age, instance.age, WireType.UINT);
    writer.writeString(CodecBeanExampleSchema.numbers_name, instance.name);
```

示例中的int类型的age字段，在编码时将使用uint格式编码。

#### 指定多态字段的实现

以Map的解码为例，一般序列化框架只能反序列化为LinkedHashMap，限制了业务对数据结构的引用；但Dson支持你指定字段的实现类，eg：

```
    @FieldImpl(EnumMap.class)
    public Map<Sex, String> sex2NameMap3;
```

上面的这个Map字段在解码时就会解码为EnumMap。具体类型的集合和Map，通常不需要指定实现类，但也是可以指定的，eg：

```
    public Int2IntOpenHashMap currencyMap1;
    
    @FieldImpl(Int2IntOpenHashMap.class)
    public Int2IntMap currencyMap2;
```

上面的这两个Map字段都会解码为 Int2IntOpenHashMap，编解码代码都是生成的静态代码，看看生成的代码你就很容易明白这是如何实现的。

#### 字段级别的读写代理

上面都是FieldImpl的简单用法，FieldImpl的最强大之处就在于字段级别的读写代理。  
Dson的理念是：**能托管的逻辑就让生成的代码负责，用户只处理特殊编解码的部分**。  
一个很好的编码指导是：我们写的代码越少，代码就越干净了，维护成本就越低，项目代码质量就越有保证。

与一般的序列化工具不同，Dson支持生成的代码调用用户的自定义代码，从而实现在编解码过程中用户只处理特殊字段逻辑。  
举个栗子，假设一个Class有100个字段，有一个字段需要特殊解码，那么用户就可以只处理这一个字段的编解码，其余的仍然由生成的代码负责，
生成的代码在编解码该特殊字段的时候就会调用用户手写的代码。看段代码：

```
    @FieldImpl(writeProxy = "writeCustom", readProxy = "readCustom")
    public Object custom;

    //
    public void writeCustom(BinaryObjectWriter writer) {
        writer.writeObject(custom, TypeArgInfo.OBJECT);
    }

    public void readCustom(BinaryObjectReader reader) {
        this.custom = reader.readObject(TypeArgInfo.OBJECT);
    }
```

我们在类中有一个Object类型的custom字段，并且通过FieldImpl声明了读写代理方法的名字，
生成的代码就会在编解码custom的时候调用用户的方法，下面是生成的代码节选：

```
    // 解码方法
    instance.currencyMap1 = reader.readObject(CodecBeanExampleSchema.numbers_currencyMap1, CodecBeanExampleSchema.currencyMap1);
    instance.currencyMap2 = reader.readObject(CodecBeanExampleSchema.numbers_currencyMap2, CodecBeanExampleSchema.currencyMap2);
    instance.readCustom(reader);
    // 编码方法
    writer.writeObject(CodecBeanExampleSchema.numbers_currencyMap1, instance.currencyMap1, CodecBeanExampleSchema.currencyMap1);
    writer.writeObject(CodecBeanExampleSchema.numbers_currencyMap2, instance.currencyMap2, CodecBeanExampleSchema.currencyMap2);
    instance.writeCustom(writer);
```

#### AfterDecode等钩子方法

Dson提供了 *writeObject、readObject、afterDecode、constructor* 4种默认的钩子调用支持。

1. 如果用户定义了包含指定writer的writeObject方法，在编码时将自动调用该方法。
2. 如果用户定义了包含指定reader的readObject方法，在解码时将自动调用
3. 如果用户定义了包含指定reader的构造方法，在解码时将自动调用 - 通常用于读取final字段。
4. 如果用户定义了无参的afterDecode方法，在解码的末尾将自动调用 - 通常用于处理缓存字段。

注意，这里仍然遵从前面的编码指导，你只需要处理特殊的字段，其它字段交给生成的代码处理即可。  
仍然是上面的类，我们在其中定义了一个afterDecode方法，生成的代码会自动调用该方法，我们可以在该方法中检查数据的状态和初始化缓存字段。

```
    public void afterDecode() {
        if (age < 1) throw new IllegalStateException();
    }
```

## 数据格式转换

### Dson如何实现到其它数据格式的转换

一般的序列化组件，只有简单int32、int64、string这种基本的值类型，通过这些值确实能构建任意的对象，但它们不能直接表达业务。
比如：一个类中的long字段是日期时间或时间戳，就无法在序列化结果中得到体现，因此也就无法直接转换为其它数据格式。
当然，你可以手写序列化代码，将这个long序列化为一个特定的数据结构，从而让序列化数据可以转换为其它数据格式。

Dson除了基本的值类型外，还提供了ExtInt32（带标签的Int32）、ExtInt64（带标签的Int64）、ExtString（带标签的String）--
且Binary也是带标签的。
以上面的long值为例，用户可以通过注解将其标记为ExtInt64类型，并声明其子类型为datetime，生成的序列化代码就会将其序列化为ExtInt64,

简单说，Dson为用户提供大量的可选标签，尽量让序列化后的数据**记录字段的业务目的，而不仅仅是字段类型**
，因此Dson的数据就更容易转换为其它数据格式。

```
    @FieldImpl(dsonType = DsonType.EXT_STRING, subType = DsonExtStringType.REGULAR_EXPRESSION)
    public String reg;
    
    // 生成的编码代码
    writer.writeExtString(CodecBeanExampleSchema.numbers_reg, DsonExtStringType.REGULAR_EXPRESSION, instance.reg);
    // 生成的解码代码
    instance.reg = reader.readString(CodecBeanExampleSchema.names_reg);
```

仍然是CodecBeanExample中的代码，我们将一个String标记为了正则表达式，序列化时就会序列化为带标签的字符串；
解码通常不需要特殊处理，因为我们的字段是字符串类型，可以读取ExtString类型。

### Dson为什么要做数据格式转换

在之前的序列化组件中，我也只设计了基本的数据类型，但我发现我写了Dson的Codec以后，假设想写入到MongoDB，多数情况下确实可以通过Dson转换，
但有一些特殊含义的字段，在转换时会出现异常，就比如上面的datetime，我真的不想写那么多的codec，于是就想让Dson来完成这件事。