---
name: datascript-syntax
description: This skill should be used when the user works with DataScript (.ds) files in the Wjybxx.BigCatTool project — writing/editing .ds schemas, asking about DataScript syntax, keywords, annotations, or the DSFileParser. Covers option/import/class/struct/enum/service/inst declarations, generics, built-in types, field numbering, and the annotation (//@Type{}) system used for code generation.
version: 1.0.0
---

# DataScript (.ds) Syntax

DataScript 是 `Wjybxx.BigCatTool` 项目中一套类 Protobuf 的数据定义语言(DSL),用于定义数据结构、枚举、RPC 服务和实例,并生成 Java / C# 代码。

## 关键源码位置

- 解析器:`Wjybxx.BigCatTool.Core/src/DataScript/DSFileParser.cs`
- 关键字/内置类型:`Wjybxx.BigCatTool.Core/src/DataScript/DSKeywords.cs`
- 内置注解定义:`Wjybxx.BigCatTool.Core/src/DataScript/DSAnnotations.cs`
- 注解解析:`Wjybxx.BigCatTool.Core/src/Core/Annotation.cs`
- 元素类型枚举:`Wjybxx.BigCatTool.Core/src/DataScript/DSElementKind.cs`
- 完整示例:`Wjybxx.BigCatTool.Tests/res/data_script.ds`

## 解析要点(务必记住)

- 逐行解析(`DSFileParser`),严格依赖行结构。
- 字段/方法/枚举值必须以 `= number;` 编号结尾;解析器对**字段编号**和**方法编号**做重复校验。
- `option` 值和 `inst` 值都是 **Dson** 格式;`inst` 的 `{}`/`[]` 需要严格缩进。
- 注解**不支持换行**(单行内闭合),否则 token 解析失败。
- 命名空间为**文件简单名**;内部类通过 `Outer.Inner` 访问。

## 1. 文件级选项 `option`(值为 Dson)

```ds
option csharp_namespace = "Wjybxx.BigCatTool.Tests.Generated";
option java_package = "cn.wjybxx.xxx";
option data_class = true;                    // 全文件默认生成 equals/hashCode
option codec_alias_prefix = "${fileName}";   // codec 别名前缀;${fileName} 自动填文件名
option macro_types = [...];                   // 声明宏注解类型(归属文件级)
```

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
- 内建结构:`DateTime Timestamp ObjectPtr Pair`
- 容器:`List<T>` `HashSet<T>` `Map<K,V>`
- 装箱:`Object` `Nullable<T>`(`T?` 等价于 `Nullable<T>`)

## 6. 实例 `inst`(仅顶层,值为 Dson)

```ds
inst Vector3 { x: 0, y: 1, z: 0 }
inst Vector4 from Vector3 { w: 0 }           // 从模板继承初始化(数组不能有 from)
inst vector3_array [ {x:1,y:0,z:0}, ... ]    // 数组实例
inst v1 { x: 0, y: 0, z: 0 }                 // 支持不换行
```

## 7. 注解系统 `//@Type{...}` / `//@Type[...]`

注解写成注释,值为 Dson(object 或 array),附着于其下方元素。行尾注释也可作为注解。

```ds
// @Options{ ssti: true, encodeFeatures: NumberHex }
int32 strLink = 10;

// @Options{ alias: [Vector3, V3], encodeFeatures: ObjectFlow }
// @Editor{ displayType: Vector3 }
struct Vector3 { ... }
```

常用内置注解(详见 `DSAnnotations.cs`):
- `@Options` — 类型/字段可选项。类型:`isFlags` `isIndexes` `baseType` `dataClass` `nonGenerate` `alias` `style` `encodeFeatures` `decodeFeatures` `projection`;字段:`nonSerialized` `nonEqual` `ssti` `encodeFeatures`。特征值支持字符串(竖线分隔)或数组,忽略大小写。
- `@Editor` — 编辑器属性:`displayType` `displayName` `tooltip` `min` `max` `initNull` `dsonType` 等。
- `@Namespace{ java: "...", cs: "..." }` — 覆盖命名空间(第三方类型)。
- `@region` / `@endregion` — 区域(强制归属所属容器)。
- 编辑器专用:`@PortField` `@PortNameRemap` `@PopField` `@BranchField` `@PloyField` `@MaskField` `@Candidates`。
- 扩展自定义注解 Key 时建议加前缀,避免与内置 Key 冲突。
