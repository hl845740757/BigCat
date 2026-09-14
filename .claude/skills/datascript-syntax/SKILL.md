---
name: datascript-syntax
description: This skill should be used when the user works with DataScript (.ds) files in the Wjybxx.BigCatTool project — writing/editing .ds schemas, asking about DataScript syntax, keywords, annotations, or the DSFileParser. Covers the `inst @file` file-option block, import/class/struct/enum/service/inst declarations, generics, built-in types, field numbering, and the annotation (//@Type{}) system used for code generation and the Unity node editor.
version: 1.1.0
---

# DataScript (.ds) Syntax

DataScript 是 `Wjybxx.BigCatTool` 项目中一套类 Protobuf 的数据定义语言(DSL),用于定义数据结构、枚举、RPC 服务和实例,并生成 Java / C# 代码,同时为 Unity 的 `DataEditor` 节点编辑器提供 schema。

## 关键源码位置

- 解析器:`Wjybxx.BigCatTool.Core/src/DataScript/DSFileParser.cs`
- 关键字/内置类型:`Wjybxx.BigCatTool.Core/src/DataScript/DSKeywords.cs`
- 内置注解定义(注解键的权威清单):`Wjybxx.BigCatTool.Core/src/DataScript/DSAnnotations.cs`
- 注解解析:`Wjybxx.BigCatTool.Core/src/Core/Annotation.cs`
- 符号表/依赖解析:`Wjybxx.BigCatTool.Core/src/DataScript/DSRepository.cs`
- 代码生成:`Wjybxx.BigCatTool.Core/src/DataScript/CodeGenerator.cs` + `CodeGeneratorHelper.cs`
- 元素/上下文类型枚举:`DSElementKind.cs`、`DSContextType.cs`
- **当前写法的示例**:`unity/Assets/Editor/DataScripts/*.ds`(`common.ds` `assetor.ds` `btree.ds` `animator.ds` `test.ds`)
- 旧示例(仅测试用,写法较老):`Wjybxx.BigCatTool.Tests/res/data_script.ds`

## 解析要点(务必记住)

- 逐行解析(`DSFileParser`),严格依赖行结构。
- 字段/方法/枚举值必须以 `= number;` 编号结尾。解析器对**同一类型内**的字段编号和方法编号做重复校验并抛 `IOException`;注意**继承层次中编号会重复**,字段不能靠 number 查找(见 `DSNamedType.cs`)。
- `inst` 的值是 **Dson** 格式,`{}`/`[]` 需要严格缩进。
- 注解**不支持换行**(必须单行内闭合),否则 token 解析失败。
- DS 层的命名空间为**文件简单名**;内部类通过 `Outer.Inner` 访问。生成代码的命名空间另由文件选项 `csharp_namespace` / `java_package` 决定。
- 顶层的 `inst` 名字**允许和类型名重复**(见 `DSRepository.cs` 注释)。

## 1. 文件级选项 —— `inst @file { ... }`

> **重要**:旧的 `option key = value;` 语法**已废弃**。`DSKeywords.OPTION` 现在是 `private`,解析器的顶层 dispatch 里**没有 `option` 分支**,写了会落到 `default` 被**静默忽略**(不报错,但不生效)。

文件选项通过名为 `@file` 的特殊实例声明(`DSFileParser.InitFileOptions`)。**值是 Dson object,用 `key: value,`,不是 `key = value;`**;字符串要加双引号:

```ds
// 文件选项
inst @file {
  macro_types: [region, endregion],
  csharp_namespace: "Wjybxx.BigCatTool.Tests.Generated",
  java_package: "cn.wjybxx.xxx",
  data_class: true,
  codec_alias_prefix: "${fileName}",
}
```

支持的键(`DSKeywords.cs` 的 `options-file` 区):

| 键 | 类型 | 说明 |
|---|---|---|
| `macro_types` | 数组 | **声明哪些注解是宏注解**。见下方 §7.1,不声明 `@region` 就不生效 |
| `csharp_namespace` | 字符串 | C# 命名空间。**生成 C# 代码时必需**,缺失会抛 `csharpNamespace is absent` |
| `java_package` | 字符串 | Java 包名 |
| `data_class` | bool | 全文件默认生成 equals/hashCode |
| `codec_alias_prefix` | 字符串 | codec 别名前缀;`"${fileName}"` 自动填文件名 |

`@` 开头是特殊实例的约定前缀(`DSKeywords.INST` 注释)。纯类型镜像文件(如 `common.ds`、`assetor.ds`)通常不设 `csharp_namespace`,而是靠 `@Namespace` 逐类型指定 + `nonGenerate`。

## 2. 导入 `import`

```ds
import "other.ds";           // private:不传递依赖
import public "common.ds";   // public:依赖传递
```

## 3. 四种命名类型

**class**(引用类型,支持继承):
```ds
class SimpleBean {
    readonly int32 age = 1;   // readonly 修饰只读字段
    string name = 2;
    int32? opt = 3;           // Nullable<int32>
    List<int32> list = 6;
    Map<int32, string> dic = 8;
}
class SimpleChildBean : SimpleBean { ... }   // 继承
```

**struct**(值类型,禁止继承)、**enum**(枚举,禁止继承)、**service**(RPC 服务):
```ds
struct Vector3 { readonly float x = 1; readonly float y = 2; readonly float z = 3; }

enum Color { White = 0; Red = 1; }   // 值支持 16 进制

service FirstService {
    func Echo(Vector3 v3) : (Vector3) = 1;              // 参数可命名/可无参/可无返回
    func Echo(List<Vector3> list) : (List<Vector3>) = 2;
}
```
- 内部类允许嵌套定义;禁止在 class 内嵌 service;方法签名 `func Name(ArgType argName) : (ResultType) = number;`。

## 4. 泛型

```ds
class GenericBean<T, U>
    where T: struct        // 约束:struct(值类型) / class(引用类型) / new(默认构造)
    where U: class {
    T? key = 1;
    U value = 2;
}
class GenericChildBean<T, U> : GenericBean<T, U> where T: struct where U: class {}
```

## 5. 内置类型(DSKeywords)

- 原子:`int32 int64 float double bool string bytes`
- 内建结构:`DateTime Timestamp ObjectPtr Pair<K,V>`
- 容器:`List<T>` `HashSet<T>` `Map<K,V>`
- 装箱:`Object` `Nullable<T>`(`T?` 等价于 `Nullable<T>`)

内置类型挂在伪命名空间 `global` 下(`DSKeywords.GLOBAL`)。

## 6. 实例 `inst`(仅顶层,值为 Dson)

```ds
inst Vector3 { x: 0, y: 1, z: 0 }
inst Vector4 from Vector3 { w: 0 }           // 从模板继承初始化
inst Foo from t1, t2 { ... }                  // from 支持多个模板,逗号分隔
inst vector3_array [ {x:1,y:0,z:0}, ... ]    // 数组实例:数组不支持 from
inst v1 { x: 0, y: 0, z: 0 }                 // 支持不换行
```

## 7. 注解系统 `//@Type{...}` / `//@Type[...]`

注解写成注释,值为 Dson(object 或 array),附着于其下方元素;行尾注释也可作为注解。`//` 与 `@` 之间允许空格。注解类型名须匹配 `^[a-zA-Z][a-zA-Z0-9_\.]*$`。**注解可以无值**(如单独一行 `// @endregion`),此时值为 `DsonNull.NULL`。

```ds
// @Options{ ssti: true, encodeFeatures: NumberHex }
int32 strLink = 10;

// @Namespace{cs: "UnityEngine"}
// @Options{ alias: [Vector3, V3], encodeFeatures: "Double4AsVector | Double4Len3" }
// @Editor{ displayType: Vector3, dsonType: Double4 }
struct Vector3 { ... }
```

特征值(`encodeFeatures`/`decodeFeatures`)支持**字符串(竖线分隔)或数组**两种形式,忽略大小写。

### 7.1 宏注解 `@region` / `@endregion` —— 必须先声明

宏注解**强制归属为文件级**注解,而不是下方元素的注解。但解析器只把 `macro_types` 里列出的类型当作宏(`DSFileParser.Parse` 检查 `macroTypes.Contains(annotation.type)`),所以**必须在 `inst @file` 中声明**:

```ds
inst @file {
  macro_types: [region, endregion],
}
```

未声明时 `@region` 会被当成普通注解附着到下一个元素上,行为完全不同。值格式由用户自行约定(数组或 object):

```ds
// @region[cmd, p1, p2]
// @region{cmd: cmd, k1: v1, k2: v2}
```

### 7.2 `@Options` —— 类型/字段可选项

**用于类型**:`isFlags`(枚举值为掩码) `isIndexes`(枚举值可作数组下标/存入 BitSet) `baseType`(生成代码的特殊超类,主要给枚举用) `dataClass`(生成 equals/hashCode) `nonGenerate`(生成代码时跳过,即外部库类型的镜像) `modifiers`(待定) `style` `alias`(序列化别名,单值可不写成数组) `encodeFeatures` `decodeFeatures` `projection`(类型投影:把自定义结构投影到其它结构以覆盖编辑器属性;投影类型生成代码时自动跳过)。

**用于字段**:`nonSerialized` `nonEqual`(不参与 equals,因此也不参与 hash) `ssti`(标识 int 或 `List<int>` 字段的值是共享字符串表索引) `encodeFeatures` `decodeFeatures`。

DS 脚本中的类型**默认都是可序列化的**。用于 service/method 时语义由用户自行约定。

### 7.3 `@Namespace` —— 覆盖命名空间

```ds
// @Namespace{ java: "cn.xxx", cs: "UnityEngine" }
```
用于第三方程序集类型;建议为每个第三方程序集单独建一个 ds 文件。命名空间含点号故须加双引号;显式指定单个类型时要用全路径。

### 7.4 `@Editor` —— 编辑器属性(键比想象的多)

```ds
// @Editor{ displayType: Vector3, displayName: 坐标, tooltip: "提示", dsonType: Double4 }
```

- 展示:`displayType`(枚举名见代码) `displayName`(默认取类型名/字段名) `tooltip`
- 数值/初始化:`min` `max` `initNull`(延迟初始化;Port 字段自动为 null) `pathType`(初始化 ObjectPath 字段,枚举值见 `ObjectPathType`)
- 控件行为:`isDelayed`(延迟响应输入) `isMultiline`(多行文本) `isInteger`(整数型 AABB) `isFolder`(文件夹路径) `isSheet`(List/Map 字段渲染为表单)
- 布局:`minWidth` `maxWidth` `maxHeight` `labelMargin` `labelMargins`(原子结构内嵌字段的边距,数组,允许 null)
- 投影/节点:`dsonType`(把自定义结构导出为 Dson 内建结构,如 `ObjectPtr`/`Pointer`/`Double4`) `nodeFeatures`(node 特征值,如是否启用 Port 端口)

### 7.5 编辑器专用注解(GraphView 节点编辑器)

- `@PortField{ side: Right, distinct: true, expanded: true }` —— 端口字段。`side` 取 `Left`/`Right`/`Bottom`(默认 `Right`);`distinct` 禁止 List/Map 端口连到同一对象;`expanded` 默认展开(同侧只能有一个)。**List/Map 内元素也要做端口时,必须把 List 字段自身标为 PortField。**
- `@PortNameRemap{ path: displayName }` —— 端口名重映射,`path` 用 `"a.b.c"` 格式。仅适用于静态路径字段,不适用于 List/Map 内元素;用于解决复用数据结构导致的端口名重复。
- `@PopField{ value: 1, displayName: AABB }` 或 `@PopField{ @{clsName} }` —— 下拉字段,支持多个自动合并。语法 2 表示把 int 字段映射到 enum 类型(枚举放在对象头)。通常与 `@BranchField` 配套实现标签类,也用于 IntMask 字段。
- `@BranchField{ ctrl: type, value: 1, displayName: radius, tooltip: "半径" }` —— 分支(标签类)字段,支持多个自动合并。`ctrl` 是控制字段名(通常是 PopField 或枚举字段),`value` 支持 int32 与 string。示例含义:当 `type == 1` 时该字段有效并展示为 `radius`。
- `@PloyField[ Vector2, Vector3 ]` —— 多态字段(数组型,支持多个自动合并)。字段为集合/Map 时表示**元素**支持的多态类型。
- `@MaskField[ Left, Right, Bottom ]` 或 `@MaskField[ @{clsName} ]` —— 掩码字段。数组元素为每个 bit 的名字(无特殊字符可不加引号);语法 2 通过 `isIndexes` 型枚举初始化 MaskName。字段为集合/Map 时表示元素的 Mask 配置。
- `@Candidates[ Vector2, Vector3 ]` —— 候选值(数组型)。目前**仅支持 string 字段**。

> `@NodeStyle` 在 `DSAnnotations.cs` 中是 `private`,尚未启用。

### 7.6 扩展自定义注解

扩展注解 Key 时建议加特殊命名前缀,避免与内置 Key 冲突。超长内容的两种应对:拆成多个注解,或用 id 指向一个 `inst`。
