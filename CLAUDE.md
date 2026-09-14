# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 仓库概览

BigCat 是游戏工具 + MMO 框架项目（作者 wjybxx）。仓库分三块：

| 目录 | 状态 | 说明 |
|---|---|---|
| `csharp/` | **活跃** | 运行时框架 + 工具链（.NET 8 SDK，`BigCat.sln`） |
| `unity/` | **活跃** | Unity 客户端（Unity 2021.3.56f2，URP） |
| `java/` | 遗留参考 | Maven / Java 21；作者已转向 C#，不再提供工具的 Java 实现。`java/README.md` 已过时（其中的 `tools` 项目已删除） |
| `docs/` | 遗留但有价值 | 里面的**规则**仍然有效，见下文「必须遵守的约定」 |

**大量核心代码不在本仓库**，而在外部 [Commons](https://github.com/hl845740757/commons) 仓库（Concurrent/Future/EventLoop、Dson 序列化、BTree 任务树、Inject、Disruptor、Poet）。C# 侧通过 NuGet 消费，Unity 侧通过 `Packages/manifest.json` 的 git 包（`#upkg` 分支）消费。改动经常需要同步升级 Commons 依赖版本。

## 构建与测试

### C# (`csharp/`)

```bash
cd csharp
dotnet build BigCat.sln
dotnet test Wjybxx.BigCatTool.Tests -f net8.0 --filter FullyQualifiedName~SheetReaderTest
dotnet test Wjybxx.BigCat.Tests      -f net8.0 --filter FullyQualifiedName~NodeTest
```

- 测试框架是 **NUnit 3**。所有项目多目标 `net6.0;net8.0`，**跑单测务必带 `-f net8.0`**，否则两个 TFM 各跑一遍。
- 仓库内**没有** `NuGet.config` / `Directory.Build.props` / `.editorconfig`。`Wjybxx.Commons.*` 包依赖本地 NuGet 源：需先在 Commons 仓库本地打包并把该目录加为 NuGet 源，否则还原失败（见 `csharp/README.md`）。
- `csharp/lib/ExcelDataReader.dll` 是本地魔改版（修了浮点精度丢失，上游拒绝合并），通过 `HintPath` 引用，不要换成 NuGet 版。

### Unity (`unity/`)

无命令行构建脚本，全部走编辑器：

- **打 AssetBundle**：`Window/BigCat/PackageBuilder` 打开图编辑器；或 **`Window/BigCat/EditorBuild` (Ctrl+E)** 直接执行 `Assets/Editor/DataScripts/PackageBuilder.dson` 中名为 `EditorPacker` 的节点。
- **测试**：`Window → General → Test Runner`，EditMode。`Assets/Scripts/Tests` 是标准 UTF+NUnit 程序集（`defineConstraints: UNITY_INCLUDE_TESTS`）。
- 其它编辑器入口：`Window/BigCat/DataEditor`（通用 Dson 对象图编辑器）、`Window/BigCat/AnimationClipEditor`（帧动画编辑器）、`Window/BigCat/EditorMenus/RefreshSpriteGroup|RefreshAudioGroup`。
- `unity/Assets/GameRes/`、`unity/Assets/Resources/` **不入库**（.gitignore），新克隆的仓库运行时资源是缺失的。`Assets/Editor/DataScripts/*.ds` 和 `*.dson` **入库**。

### Java (`java/`) — 有严格的两阶段构建顺序

```bash
cd java/apts      && mvn clean install   # 必须先装注解处理器到本地仓库
cd java/framework && mvn clean install   # 或 mvn test
```

`apts` 不能和 `framework` 一起在 IDE 里编译（需 unlink），否则代码生成失败。"generated XXX not found" 基本都是 apts 没装或缺 getter。

## 代码生成：两级链路（最重要的心智模型）

```
.xlsx / .proto / .ds   ──[Tool 生成器，离线]──►  提交进仓库的 .cs / .dson / sst.db
                                                      │
                            提交的 .cs ──[Roslyn APT，编译期]──►  内存中的 *Codec / *Proxy
```

### 第一级：离线工具（`Wjybxx.BigCatTool.*`）

- `BigCatTool.Core` = 解析器与模型：`DataScript/`（`.ds` DSL，`DSFileParser` + `DSRepository`）、`Protobuf/`（手写 `.proto` 解析器，抽取 protoc 丢掉的 `service` 与注解）、`Excel/`（Sheet 模型）。
- `BigCatTool.Generator` = 发射器：`DataScriptGenerator`（xlsx→.ds）、`DsonGenerator`（xlsx→.dson/.dson2）、`ClassGenerator`（.ds→C# 配置类）、`ConstGenerator`、`SSTGenerator`（共享字符串表 → `sst.db`/`lsst.index`/`location.db`）、`PBFileGenerator`/`PBFileCompiler`/`ServiceGenerator`。
- **没有 CLI 入口**（全仓库无 `static Main`）。两条驱动方式：
  1. **NUnit 测试**：`Wjybxx.BigCatTool.Tests/src/SheetReaderTest.cs` 是 excel 管线的端到端范本；配置在 `res/SheetGeneratorCfg.dson`（多对象 Dson，按 `clsName`/`localId` 索引），模板 `res/SheetCfg.tt`。
  2. **Unity 编辑器**：`Assets/Scripts/Core/Editor/DataScript/*` 直接调用 `BigCatTool` 类型解析 `Assets/Editor/DataScripts/*.ds`。（excel 导出链路目前**只有测试在调用**，尚无 Unity 入口。）

### 第二级：Roslyn 源生成器（`Wjybxx.BigCat.Apt` + Commons/Dson 的 Apt）

**关键坑**：生成器**不是**通过 PackageReference/ProjectReference 引入的，而是硬绑预编译 DLL：

- `csharp/Analyzers/*.dll`（`<Analyzer Include="../Analyzers/..." />`，入库）
- `unity/Assets/Analyzers/*.dll`（`.meta` 标 `RoslynAnalyzer` 标签，入库）

**改了 `Wjybxx.BigCat.Apt` 源码不会生效**，必须重新编译它，然后把 DLL 拷进上面两个目录。唯一例外是 `Wjybxx.BigCatTool.Tests.csproj`，它用规范的 `OutputItemType="Analyzer"` 形式。

各项目都设了 `EmitCompilerGeneratedFiles` + `CompilerGeneratedFilesOutputPath=Generated`，随后 `Compile Remove="Generated\**"` —— **`Generated/` 只是给人看的只读镜像，不要编辑**（且被 gitignore）。

生成器职责：`Wjybxx.BigCat.Apt/src/RpcServiceProcessor.cs` 识别 `[RpcService]`/`[RpcMethod]`，产出 `<Service>Proxy`（客户端打包成 `RpcMethodSpec<T>`）+ 同类型内的 `export(IRpcMethodRegistry, T)`（服务端）。`Wjybxx.Dson.Apt`（外部 DLL）识别 `[DsonSerializable]`/`[DsonCodecLinkerGroup]` 产出 `XxxCodec`。

## C# 运行时架构（`csharp/Wjybxx.BigCat.Core/src`）

同时也是 Unity 本地包（`src/package.json` + `src/Wjybxx.BigCat.Core.asmdef`），所以 `src` 下只有两层。

- **`Fx/`** — 线程/进程框架 + Rpc。`Node` = 一个"服"，建立在 `DisruptorEventLoop<WorkerEvent>` 上的 IO 线程，管理子 `Worker` 并作为其网络门面；`Worker` = 一个业务线程，Module/Service 宿主，自带 `IInjector`；`IMainModule`/`DefaultMainModule` 是驱动每个 Worker 帧循环的 `IEventLoopAgent`。装配走 `NodeBuilder`/`WorkerBuilder`（`NodeBuilder.RpcPackages` 决定扫哪些程序集建 `IRpcMethodRegistry`）。Rpc 运行时全在此：`IRpcClient`/`S2SRpcClient`/`RpcMethodRegistry`/`RpcContext`/`S2SSessionMgr`。`SstMgr`/`SstString` 读工具侧产出的共享字符串表。
- **`Gameplay/`** — Scene + GameUnit 组件框架。`SceneMgr` 是世界模拟的唯一驱动者，持有 `GTime`/`TimerMgr`/`CoroutineMgr` 与场景栈，暴露帧阶段 `BeginOfFrame/EarlyUpdate/FixedUpdate/Update/LateUpdate`（见 `GameLoopPhase`）。`Scene` 有数据组件+行为组件（编辑器配置、反序列化创建、≤128 种、初始化后不可增删）；`GameUnit` 只有数据组件（面向过程；池化按 GameUnit 而非按组件）。
- **`Co/`** — 自定义协程。定时器功能**故意合并进 `CoroutineMgr`** 以避免顺序问题；`TimerMgr` 每个 `GameLoopPhase` 各有 unscaled/scaled 队列。
- **`Unity/`** — `UnityEventLoop`/`UnityWorker`，让 EventLoop 跑在 Unity 主线程。
- **`Util/`** — `Blackboard`、`DataKey`、`GBitSet`/`EnumSet64`、`UnionValue`、`TimeValue` 等。

**技能/战斗框架尚未实现**（README 里列为规划项，C# 树中不存在）。

## Unity 客户端架构（`unity/Assets`）

### 程序集依赖图（6 个 asmdef）

```
外部包 (Commons.*, Dson.*, BTree.*, Disruptor, BigCat.Core, BigCatTool.Core)
        ▲
Wjybxx.BigCat.UnityCore  (Scripts/Core/Runtime，唯一引用 Unity.InputSystem 的程序集)
   ▲          ▲                    ▲
BigCat.UI     │            BigCatEditor.Core (Editor only, +BigCatTool.Core)
   ▲          │
BigCatEditor.UI(空占位)     Tests (Editor only, 只到 UnityCore)
   │
BigCat.Launcher  ──► UI + UnityCore（图的顶端）
```

### 启动与主循环 —— `Assets/Scripts/Launcher/GameLauncher.cs`

整个工程**只有 GameLauncher 一个真正的 MonoBehaviour 驱动源**；所有 Manager 都是普通 C# 类，不使用 Unity 自己的 `Update`，由 GameLauncher 显式扇出。挂在 `Assets/GameRes/Scenes/MainScene.unity` 的 `GameEntry` 上。

`Awake()` 顺序：`InputManager` → 用 `DefaultNodeBuilder` 建 Disruptor `Node`（worker 0 = `UnityWorkerBuilder(Thread.CurrentThread)` 作 Rpc 客户端，worker 1 = 后台 `DefaultWorkerBuilder` 作 Rpc 服务端 —— **这个拆分是同步 Rpc 能成立的原因**）→ `worker.Internal_Start()` 后才 `node.Start().Join()`（顺序反了会死锁）→ `SceneMgr` → `WindowMgr`（找场景根 `"UIRoot"`）→ 协程启动 `ResourceManager`。

帧驱动（顺序有意义）：
- `FixedUpdate()`：靠 `lastFrame < Time.frameCount` 保证「帧首」块每渲染帧只跑一次 —— `inputMgr.BeginOfFrame`（**输入必须在所有消费者之前**）→ `sceneMgr`/`windowMgr` 的 `BeginOfFrame`、`EarlyUpdate`；随后每个物理步跑 `sceneMgr.FixedUpdate`。
- `Update()`：`worker.Internal_Update()`（抽干 Disruptor 队列）→ `ResourceManager.Update` → `sceneMgr.Update` → `windowMgr.Update`。
- `LateUpdate()`：`LateUpdate` → `EndOfFrame` → 最后 `inputMgr.EndOfFrame()`。

`Assets/Scripts/Launcher/AppContext.cs`（基于 BTree `StateMachineTask` 的顶层流程状态机）**目前未接线**。`EditorBootTask.cs` 是编辑器模式的资源启动任务，真机版尚缺。

### UI 框架（`Assets/Scripts/UI/Runtime`）

`Window` 与 `WindowMgr` **都不是 MonoBehaviour**（有意为之，避免签名冲突），由 `WindowMgr` 统一 tick。

- 作者侧只碰 MonoBehaviour 配置类：预制体根挂 `Canvas` + **`WindowCfg`**（`desktopId`/`sortLayer`/`sortOrder`/`features`/`tags`/`maxIdleTime`/`dataAddress`/`rootNode`；`MAX_DESKTOP=5, MAX_LAYER=9, MAX_SORT_ORDER=49`），子节点挂 **`UINode`** 子类（**约定命名 `*View`**）+ 内联 `UINodeCfg`（`elements` 按 **GameObject 名字** 查找），可选 `Controller`（MVC 控制器）与 `WComponentCfg`（Window 级逻辑组件 / `IWindowAgentHolder`）。
- `Desktop` 是"分屏"而非 z 层；`SortWindows()` 按 `(sortLayer, sortOrder, openOrder)` 排序并按块分配 `Canvas.sortingOrder`，全遮挡的窗口 `SetActive(false)`。
- `Window` 是**资源所有权单元**：关闭即释放其 `AssetHandle`。关闭的窗口进入按 `destroyTime` 排序的优先队列，在 `EndOfFrame` 惰性销毁 —— 所以**销毁前可以被重新打开**。关父窗口会连带关子窗口（除非子窗标了 `Nohup`）。
- 数据绑定靠字符串 `dataAddress`（如 `"{logic}.bagModel.itemList.{uiIndex}"`），由 `Core/Runtime/MVC/DataModelResolver` 反射解析。逻辑层只依赖 `WindowCmdMgr` 接口（`WindowMgr` **故意不实现它**），以便无头运行。
- `Controller`/`WComponent` 里**不要写 `Update` 方法，也不要用 Unity 协程** —— 框架用反射探测你重写了哪些帧阶段（`UIInternal.GetOverrideInfo`）。
- UI **没有专属代码生成**；`Assets/Scripts/UI/Editor` 是空占位程序集。
- 唯一的 View 范例：`Assets/Scripts/Launcher/UI/LoginView.cs`。

### 资源系统（`Assets/Scripts/Core/Runtime/Assetor`）

门面 `ResourceManager`（static `Inst`，`[Inject]` 构造）套在 `IPackageManager` + `IBundleManager` 列表之上，`AssetQuery` 做路径→`AssetFileInfo` 查询。**所有加载都是行为树任务**：`ResourceTask : Decorator<Blackboard>`，`TaskScheduler : BranchTask<Blackboard>` 分时（`MaxTimeSlice=100ms`）；`Provider` 家族（`AssetProvider`/`BinaryAssetProvider`/`SceneAssetProvider`/`BundleProvider`/`InstanceProvider`）构成加载图，空闲超时卸载。编辑器路径走 `EditorAssetBundle`/`EditorBundleManager`/`EditorPackageManager`（`AssetDatabase`，不打真包）。

### 编辑器工具（`Assets/Scripts/Core/Editor`）

**打包管线本身就是一棵用图编辑器编排的 `[DsonSerializable]` 行为树**：`PackageBuilder : Sequence` = `CollectorPackage`（收集，含 `Collector`/`IAssetClassifier`/`IIgnoreService`/`PathCache`/`DependencyCache`）+ `BuildPipelineTask`（`preBuildTasks`/`buildTasks`/`postBuildTasks`，如 `BuildDependencyTask`/`BuildIndexesTask`/`EditorBuildTask`），共享状态走 `Blackboard`。

它的编排基座是 `DataScript/` 下的通用 Dson 对象图编辑器（`DataEditor : EditorWindow` + `DataGraph` + `UnityEditor.Experimental.GraphView`）：**schema 来自 `.ds` 文件**（由外部 `BigCatTool.Core` 的 `DSFileParser` 解析），图持久化为**扁平 Dson**（`Dsons.FromFlatDson`，`@ptr N` 交叉引用）。`BuildWindow : DataEditor` 只是它的一个特化。

`SpriteAnimation/AnimationClipEditor` 从精灵生成运行时的 `SpriteAnimationClip`/`SpriteModel` ScriptableObject（帧 pivot/size、`attackBoxes`/`hurtBoxes`）。`UIElements/` 是两个编辑器共用的 ~19 个 UIToolkit 字段控件。

## 必须遵守的约定

### 依赖约束（`csharp/README.md`）

1. C# 程序集尽量不依赖 Unity 程序集，最多依赖 Unity 基础模块。
2. **运行时代码绝不依赖 Editor 代码**；编辑器的职责就是导出运行时需要的配置。
3. `Wjybxx.BigCatTool.*` 尽量不依赖运行时程序集 —— 当前严格遵守：`BigCatTool.*` 与 `BigCat.Core` 之间**零项目引用**，只共享 NuGet 包。

### C# 代码风格

- **`LangVersion 9` 是硬上限，注释写明"为兼容unity"** —— 不能用 record、file-scoped namespace、`required`。
- 全项目 `ImplicitUsings=disable`、`Nullable=annotations`（局部用 `#nullable disable/restore`）、`GenerateDocumentationFile=true`、`NeutralLanguage=zh-Hans`。
- 每个 `.cs` 以 `#region LICENSE` 包裹的 Apache-2.0 头开始（模板见根 `HEADER`）。
- 4 空格缩进、K&R 大括号（`public void Foo() {`）、namespace 大括号体不缩进、`#region` 分节、**中文 XML 文档注释**。

### `.ds`（DataScript）

有专门的 skill：`.claude/skills/datascript-syntax/SKILL.md`（已对齐当前源码，v1.1.0）。要点：

- **文件级选项用 `inst @file { key: value, }`（Dson object 语法）**。旧的 `option key = value;` 已废弃 —— `DSKeywords.OPTION` 现在是 private，解析器顶层 dispatch 没有 `option` 分支，写了会被**静默忽略**（不报错也不生效）。
- **`@region`/`@endregion` 必须先在 `inst @file` 的 `macro_types` 里声明**，否则不会被识别为文件级宏注解，而会附着到下一个元素上。
- 生成 C# 代码时 `csharp_namespace` 必填，缺失抛 `csharpNamespace is absent`；纯类型镜像文件（`common.ds`/`assetor.ds`）不设它，改用逐类型的 `@Namespace` + `nonGenerate`。
- 新写法看 `unity/Assets/Editor/DataScripts/*.ds`；`Wjybxx.BigCatTool.Tests/res/data_script.ds` 是旧写法，别照抄。
- 注解键的权威清单在 `DSAnnotations.cs`（`@Options`/`@Editor`/`@Namespace` 及一整套节点编辑器注解 `@PortField`/`@PopField`/`@BranchField`/`@PloyField`/`@MaskField`/`@Candidates`/`@PortNameRemap`）。

解析器是逐行手写的，硬约束：字段/方法/枚举值必须以 `= number;` 结尾（同类型内查重，但继承层次中编号会重复）；注解必须单行闭合；`inst` 的值是 Dson。

### `.proto`（`docs/Protobuf.md`）—— 解析器手写，以下是硬约束

1. 字段或 rpc 方法定义**必须单行**，不能折行。
2. `service`/`message`/`enum` 的**右括号必须独占一行**。
3. `service` 内**不允许嵌套 message**。
4. **文件名、顶层 message 名、service 名必须全局唯一。**
5. rpc 方法**只能引用顶层 message**。
6. **只支持 `//` 行注释**；注释与被注释元素之间**不能有空行**。
7. Rpc 方法**最多一个参数、最多一个返回值，且必须是 Message 类型**；允许重载（按 id 分发）—— 这会让 IDE 的 proto 插件报错，属正常。
8. 元注解 `//@Rpc {id: 1, async: true, ctx: true, manual: true}`（值为 Dson）；service 级是默认值，method 级覆盖。service `id` ∈ [-32767, 32767]，method `id` ∈ [0, 9999]。
9. 调试：protoc 编译的是预处理后的临时文件（`service` 块被注释掉），报错时要看临时文件。

### Rpc 设计（`docs/Rpc.md`）

- 客户端与服务端接口**故意不对称**：不生成客户端接口，只生成 `XxxProxy` 打包类。
- 服务端返回 `Future<T>` 时，**Proxy 返回类型是 `T`**（泛型解包）—— 客户端不应感知服务端同步还是异步。
- 执行方式由**调用方**在 `IRpcClient` 上选：`send()` 通知 / `call()` 异步 / `syncCall()` 阻塞。目标节点始终显式传 `NodeId`，框架不选路。
- 分发靠 `serviceId` + `methodId`，**各限 2 字节** —— 这是为什么放弃了兼容性检查。
- **手动返回契约**：`RpcContext<V>` 必须是**第一个**参数，方法返回类型必须是 `void`，并标 `manualReturn = true`。

### 文件热更新（`docs/FileReload.md`，C# 侧待实现）

六阶段：解析依赖 → 读取（无依赖文件**并行**，有依赖文件**串行**）→ 沙箱 Assign+Link → 校验 → 发布 → 通知。**只有并行读取阶段是多线程的，其余都在主线程。**

- 循环依赖非法会抛异常；打破环要用 `FileDataLinker`（延迟链接），代价是至少一个外键字段不能 `final`。
- 沙箱**并非完全隔离**（重读全部文件太贵），原子性靠**失败回滚**：失败时对 live 环境重跑 Link 修复。
- 因此 **Linker / Validator / Listener 都必须幂等且互不依赖**；Listener 每轮更新只被通知一次，且**监听器之间无顺序保证**。
- `FileReader` 应在读取时就校验本表数据并尽早抛错；跨表校验放 `FileDataValidator`。

### 表格设计（`docs/表格设计.md`）

- 每个 sheet **前 10 行保留给自由注释**（行数可配但所有 sheet 必须一致）。
- 普通表：第 11 行 `options`（`cs`/`sc`/`s`/`c` = 哪一侧需要）、12 行 `type`、13 行 `name`（**lowerCamelCase 且表内唯一**）、14 行 `comment`，数据从 15 行开始。**第 1 列值必须唯一**；逻辑上二维索引的表（如 skillId+skillLevel）必须额外加一个可推导的主键列。
- 参数表是转置版：第 11 行就是表头 `options | name | type | value | comment`；导出器靠第 11 行判断表类型。
- 数组最多二维（`type[]`/`type[][]`），json/dson 写法；**数组内的字符串必须加引号**。格式化占位符用**带下标的 `{n}`**（便于换序与 i18n）。**绝不让策划配位掩码** —— 配索引数组，导出时转 bit。
- Sheet 名为 `SheetXxx`/`sheetXxx` 或以 `_` 开头则**被忽略**；只允许字母数字下划线。编辑器专用配置必须放独立文件，不能写在源表里。
- 已废弃：运行时的 `Sheet` 抽象。`Sheet` 现在只用于工具，运行时直接消费 **Dson**。

## 已知的陈旧信息（别被误导）

- `csharp/temp/README.md` 是一份人/AI 写的架构分析文档（含一份不错的模块索引和疑似 bug 列表），但其中的包版本号已过时（**csproj 才是权威**），且它引用的 5 个兄弟 md 文件并不存在。
- `java/README.md` 仍在描述已删除的 `tools` 项目。
- `docs/Rpc.md` 里指向 `doc/Protobuf.md` 的链接路径已失效（目录是 `docs/`）。
- `Assets/csc.rsp.meta` 存在但 `csc.rsp` 本身被 gitignore，是开发者本地文件。
- `Assets/Editor/FileSyncMgr.cs`（`csharp/<Project>/src` → `Assets/ThirdParty/`）的 `_projectNames` 为空且 MenuItem 已注释掉。
- 领域命名变迁：`damageBoxes` → `hitBoxes` → `attackBoxes`。
