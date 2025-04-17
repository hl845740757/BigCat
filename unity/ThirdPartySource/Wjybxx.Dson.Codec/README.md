# Dson序列化

Dson有许多强大的特性，你如果只是简单使用Dson，那和普通的序列化组件差不多，可能还没那么方便，因为还要做点准备工作；
如果与Dson深度集成，Dson将提供许多强大的功能。

ps: Readme文档暂时复制了Java的内容，Csharp的序列化将包含Java的所有功能，
传送门[Java-Code](https://github.com/hl845740757/Dson/blob/dev/java/DsonCodec.md)。
(建议多阅读源码文档和测试用例，这里的文档不一定及时更新)

## 特性一览

1. **支持泛型**
2. 默认值可选写入
3. 指定数字字段的编码格式(apt)
4. 支持多态解析，指定指定默认解码类型(apt)
5. **字段级别的读写代理(核心功能)**(apt)
6. 序列化钩子方法(apt)
7. 单例支持(apt)
8. 为外部库类生成Codec(apt)
9. 外部静态代理(apt)

不支持的特性：

1. 默认不支持对象图序列化
2. 不支持委托类型。

## 有限泛型支持

Csharp是真实泛型，为方便使用，Dson库对泛型支持了完整支持 —— 使用上有点配置工作量。  
支持泛型的优点：

1. 类型自解释，精准编解码
2. 跨语言通信支持 -- 更多是共享配置文件。

注意：虽然C#库提供了完整的泛型支持，在涉及公共文件时，应当限制泛型的使用 —— 避免泛型参数为泛型，
否则影响跨语言时的兼容性。

## 默认值可选写入

对于基础类型 int32,int64,float,double,bool，可以通过`Options.appendDef`控制是否写入写入默认值；
对于引用类型，可以通过`Options.appendNull`控制是否写入null值。

如果数据结构中有大量的可选属性（默认值），那么不写入默认只和null可以很好的压缩数据包。

## 指定数字字段的编码格式

Dson集成了Protobuf的组件，支持数字的`varint`、`unit`、`sint`、`fixed`4种编码格式，你可以简单的通过`DsonProperty`注解声明
字段的编码格式，而且**修改编码格式不会导致兼容性问题**，eg：

```
    @DsonProperty(wireType = WireType.Uint)
    public int age;
    
    // 生成的编码代码
    writer.WriteInt(names_age, inst.age, WireType.Uint);
    writer.WriteString(names_name, inst.name);
```

示例中的int类型的age字段，在编码时将使用uint格式编码。

## 指定多态字段的实现

以字典的解码为例，一般序列化框架只能反序列化为`Dictionary<K,V>`，限制了业务对数据结构的引用；但Dson支持你指定字段的实现类，eg：

```
    @DsonProperty(impl = typeof(LinkedDictionary<,>))
    public IDictionary<Sex, String> sex2NameMap3;
```

上面的这个字典字段在解码时就会解码为`LinkedDictionary`。

## 字段级别的读写代理(核心)

上面都是DsonProperty的简单用法，DsonProperty的最强大之处就在于字段级别的读写代理。  
Dson的理念是：**能托管的逻辑就让生成的代码负责，用户只处理特殊编解码的部分**。  
一个很好的编码指导是：**我们写的代码越少，代码就越干净了，维护成本就越低，项目代码质量就越有保证**。

与一般的序列化工具不同，Dson支持生成的代码调用用户的自定义代码，从而实现在编解码过程中用户只处理特殊字段逻辑。  
举个栗子，假设一个Class有100个字段，有一个字段需要特殊解码，那么用户就可以只处理这一个字段的编解码，其余的仍然由生成的代码负责，
生成的代码在编解码该特殊字段的时候就会调用用户手写的代码。看段代码：

ps: 字段读写代理几乎可实现`DsonProperty`提供的其它所有功能。

```
    @DsonProperty(writeProxy = "WriteCustom", readProxy = "readCustom")
    public Object custom;

    // 定义了钩子方法后，生成的Codec代码会自动调用
    public void WriteCustom(DsonObjectWriter writer, String name) {
        writer.WriteObject(custom, TypeArgInfo.OBJECT);
    }

    public void ReadCustom(DsonObjectReader reader, String name) {
        this.custom = reader.ReadObject(TypeArgInfo.OBJECT);
    }
```

我们在类中有一个Object类型的custom字段，并且通过DsonProperty声明了读写代理方法的名字，
生成的代码就会在编解码custom的时候调用用户的方法，下面是生成的代码节选：

```
    // 解码方法
    inst.currencyMap1 = reader.ReadObject(names_currencyMap1, types_currencyMap1);
    inst.currencyMap2 = reader.ReadObject(names_currencyMap2, types_currencyMap2);
    inst.ReadCustom(reader, names_custom);
    // 编码方法
    writer.WriteObject(names_currencyMap1, inst.currencyMap1, types_currencyMap1);
    writer.WriteObject(names_currencyMap2, inst.currencyMap2, types_currencyMap2);
    inst.WriteCustom(writer, names_custom);
```

## 序列化钩子方法

Dson提供了`WriteObject`、`ReadObject`、`Constructor`、`AfterDecode`、`BeforeEncode`5种默认的钩子调用支持。

1. 如果用户定义了包含指定writer的writeObject方法，在编码时将自动调用该方法。
2. 如果用户定义了包含指定reader的readObject方法，在解码时将自动调用
3. 如果用户定义了包含指定reader的构造方法，在解码时将自动调用 - 通常用于读取final字段。
4. 如果用户定义了包含options的AfterDecode方法，在解码的末尾将自动调用 - 通常用于处理缓存字段。
5. 如果用户定义了包含options的BeforeEncode方法，在编码之前将自动钓鱼 - 通常用于处理缓存字段。

注意，这里仍然遵从前面的编码指导，你只需要处理特殊的字段，其它字段交给生成的代码处理即可。

```
    // 序列化前钩子
    public void BeforeEncode(ConverterOptions options) {
    }
    // 自定义写入字段 - 紧随BeforeEncode调用
    public void WriteObject(DsonObjectWriter writer) {
    }
    // 读自定义写入字段
    public void ReadObject(DsonObjectReader reader) {
    }
    // 反序列化钩子
    public void AfterDecode(ConverterOptions options) {
        if (age < 1) throw new IllegalStateException();
    }
   
    // 字段读写钩子
    public void WriteCustom(DsonObjectWriter writer, String name) {
    }
    public void ReadCustom(DsonObjectReader reader, String name) {
    }
```

## 单例支持

Dson在`ClassImpl`注解中提供了`singleton`属性，当用户指定`singleton`属性时，生成的Codec将简单调用给定方法返回共享实例。

```java

@ClassImpl(singleton = "getInstance")
@DsonSerializable
public class SingletonTest {
    private static final SingletonTest INST = new SingletonTest("wjybxx", 29);

    public static SingletonTest getInstance() {
        return INST;
    }
}
```

## 为外部类生成Codec类

APT除了支持为项目中的类生成Codec外，还支持为外部库的类生成Codec，通过`CodecLinkerGroup`和`CodecLinker`两个注解实现。

```java

@CodecLinkerGroup(outputPackage = "cn.wjybxx.btree.fsm")
private static class FsmLinker {
    @CodecLinker(classImpl = @ClassImpl)
    private ChangeStateTask<?> changeStateTask;
    @CodecLinker(classImpl = @ClassImpl)
    private StateMachineTask<?> stateMachineTask;
}
```

ps: 该注解的最佳实例可见[BTree-Codec](https://github.com/hl845740757/BTree/tree/dev/java/btree-codec)

## 外部静态代理

如果我们要序列化的是一个外部库的类，且期望能够参与到目标类型序列化过程中，我们就可以通过`CodecLinkerBean`
实现外部静态代理。  
`CodecLinkerBean`支持除构造函数以外的所有钩子，包括字段的读写代理。

```
@CodecLinkerBean(value = ThirdPartyBean2.class)
public class CodecLinkerBeanTest {

    @DsonProperty(wireType = WireType.UINT)
    private ThirdPartyBean2 age;

    @DsonProperty(stringStyle = StringStyle.AUTO_QUOTE)
    private ThirdPartyBean2 name;

    // 这些钩子方法，生成的代码会自动调用
    public static void BeforeEncode(ThirdPartyBean2 inst, ConverterOptions options) {
    }
    public static void WriteObject(ThirdPartyBean2 inst, DsonObjectWriter writer) {
    }
    public static void ReadObject(ThirdPartyBean2 inst, DsonObjectReader reader) {
    }
    public static void AfterDecode(ThirdPartyBean2 inst, ConverterOptions options) {
    }
}
```

ps：

1. `CodecLinkerBean`同样会为目标Bean生成Codec类。
2. 你可以将`CodecLinkerBean`看做目标Bean的外部配置类。
3. 注解的使用实例可参考[BigCat](https://github.com/hl845740757/BigCat)项目。

---