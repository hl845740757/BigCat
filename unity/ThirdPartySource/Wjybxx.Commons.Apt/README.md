# APT

APT包是[javapoet](https://github.com/square/javapoet)仓库的移植版。
我在java端使用javapoet生成各类辅助类已有5年左右，这是个非常好用的轮子的，但C#端没有合适的等价物，于是自己移植了一版。

用言语描述APT包不够直观，我们直接看代码生成器和生成的代码（可运行测试用例）（代码生成器的最佳示例可见Dson和BTree库）。

`GeneratorTest`测试类（生成器）代码如下：

```csharp
    private static TypeSpec BuildClassType() {
        TypeName dictionaryTypeName = TypeName.Get(typeof(LinkedDictionary<string, object>));
        AttributeSpec processorAttribute = AttributeSpec.NewBuilder(ClassName.Get(typeof(GeneratedAttribute)))
            .Constructor(CodeBlock.Of("$S", "GeneratorTest")) // 字符串$S
            .Build();

        AttributeSpec attributeSpec = AttributeSpec.NewBuilder(ClassName.Get(typeof(MyCodeAttribute)))
            .AddMember("Name", CodeBlock.Of("$S", "wjybxx"))
            .AddMember("Age", CodeBlock.Of("29"))
            .Build();

        TypeSpec classType = TypeSpec.NewClassBuilder("ClassBean")
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorAttribute)
            .AddAttribute(attributeSpec)
            // 字段
            .AddField(TypeName.INT, "age", Modifiers.Private)
            .AddField(TypeName.STRING, "name", Modifiers.Private)
            .AddSpec(FieldSpec.NewBuilder(dictionaryTypeName, "blackboard", Modifiers.Public | Modifiers.Readonly)
                .Initializer("new $T()", dictionaryTypeName)
                .Build())
            // 构造函数
            .AddSpec(MethodSpec.NewConstructorBuilder()
                .AddModifiers(Modifiers.Public)
                .ConstructorInvoker(CodeBlock.Of("this($L, $S)", 29, "wjybxx"))
                .Build())
            .AddSpec(MethodSpec.NewConstructorBuilder()
                .AddModifiers(Modifiers.Public)
                .AddParameter(TypeName.INT, "age")
                .AddParameter(TypeName.STRING, "name")
                .Code(CodeBlock.NewBuilder()
                    .AddStatement("this.age = age")
                    .AddStatement("this.name = name")
                    .Build())
                .Build())
            // 属性
            .AddSpec(PropertySpec.NewBuilder(TypeName.INT, "Age", Modifiers.Public)
                .Getter(CodeBlock.Of("age").WithExpressionStyle(true))
                .Setter(CodeBlock.Of("age = value").WithExpressionStyle(true))
                .Build())
            .AddSpec(PropertySpec.NewBuilder(TypeName.BOOL, "IsOnline", Modifiers.Private)
                .Initializer("$L", false)
                .Build()
            )
            // 普通方法
            .AddSpec(MethodSpec.NewMethodBuilder("Sum")
                .AddDocument("求int的和")
                .AddModifiers(Modifiers.Public)
                .Returns(TypeName.INT)
                .AddParameter(TypeName.INT, "a")
                .AddParameter(TypeName.INT, "b")
                .Code(CodeBlock.NewBuilder()
                    .AddStatement("return a + b")
                    .Build())
                .Build())
            .AddSpec(MethodSpec.NewMethodBuilder("SumNullable")
                .AddDocument("求空int的和")
                .AddModifiers(Modifiers.Public | Modifiers.Extern)
                .Returns(TypeName.INT.MakeNullableType())
                .AddParameter(TypeName.INT.MakeNullableType(), "a")
                .AddParameter(TypeName.INT, "b")
                .Build())
            .AddSpec(MethodSpec.NewMethodBuilder("SumRef")
                .AddDocument("求ref int的和")
                .AddModifiers(Modifiers.Public | Modifiers.Extern)
                .Returns(TypeName.INT)
                .AddParameter(TypeName.INT.MakeByRefType(), "a")
                .AddParameter(TypeName.INT.MakeByRefType(ByRefTypeName.Kind.In), "b")
                .Build())
            .Build();
        return classType;
    }
```

下面是测试`GeneratorTest`类生成的代码：

```csharp
    using Wjybxx.Commons.Attributes;
    using Commons.Tests.Apt;
    using Wjybxx.Commons.Collections;
    
    namespace Wjybxx.Commons.Apt;
    
    [Generated("GeneratorTest")]
    [MyCode(Name = "wjybxx", Age = 29)]
    public class ClassBean 
    {
      private int age;
      private string name;
      public readonly LinkedDictionary<string, object> blackboard = new LinkedDictionary<string, object>();
    
      public ClassBean()
       : this(29, "wjybxx") {
      }
    
      public ClassBean(int age, string name) {
        this.age = age;
        this.name = name;
      }
    
      public int Age {
        get => age;
        set => age = value;
      }
    
      private bool IsOnline { get; set; } = false;
    
      /// <summary>
      /// 求int的和
      /// </summary>
      public int Sum(int a, int b) {
        return a + b;
      }
    
      /// <summary>
      /// 求空int的和
      /// </summary>
      public extern int? SumNullable(int? a, int b);
    
      /// <summary>
      /// 求ref int的和
      /// </summary>
      public extern int SumRef(ref int a, in int b);
    }
```