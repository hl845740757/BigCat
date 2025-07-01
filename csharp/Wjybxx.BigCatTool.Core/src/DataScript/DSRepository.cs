#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using TypeName = Wjybxx.Commons.Poet.TypeName;
using DsonTypeName = Wjybxx.Dson.Codec.TypeName;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 
/// 注意：与DS的仓库不同，我们的数据脚本顶层有Inst类型，而Inst的名字可以和其它元素重复。
/// </summary>
public sealed class DSRepository
{
    /// <summary>
    /// 虚拟全局文件
    /// </summary>
    private readonly DSFile globalFile = new DSFile(DSKeywords.GLOBAL, isVirtualFile: true);
    /// <summary>
    /// 真实文件映射(运行时不使用)
    /// key为文件简单名
    /// </summary>
    private readonly LinkedDictionary<string, DSFile> fileMap = new();
    /// <summary>
    /// 逻辑文件映射(包含内建结构所属的虚拟文件)
    /// key为文件简单名
    /// </summary>
    private readonly LinkedDictionary<string, DSFile> logicFileMap = new();
    /// <summary>
    /// 扩展工具类
    /// </summary>
    private readonly Dictionary<string, DSTypeHandler> handlerMap = new();

    /// <summary>
    /// 元素名到元素的映射，用于解决查询效率问题
    /// key为fullNAme
    /// </summary>
    private readonly LinkedDictionary<IndexKey, DSElement> indexedElementMap = new();
    /// <summary>
    /// 泛型解析缓存，用于避免泛型类重复构造
    /// </summary>
    private readonly Dictionary<ClassName, DSNamedType> genericTypeCache = new();

    /// <summary>
    /// 序列化类型别名到类型的映射（配置）
    /// </summary>
    private readonly Dictionary<string, DSNamedType> dsonAlias2TypeDic = new();
    /// <summary>
    /// 类型名到类型的解析缓存（多对1）
    /// </summary>
    private readonly Dictionary<string, DSNamedType> dsonClsName2TypeDic = new Dictionary<string, DSNamedType>();

    public DSRepository() {
        InitBuiltinTypes();
        AddFile(globalFile);
    }

    /// <summary>
    /// 用户可在build前修改内建类型数据
    /// </summary>
    private void InitBuiltinTypes() {
        // 原子类型
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_INT32).AddDsonAliases("i", "int32"));
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_INT64).AddDsonAliases("L", "int64"));
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_FLOAT).AddDsonAliases("f", "float"));
        ;
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_DOUBLE).AddDsonAliases("d", "double"));
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_BOOL).AddDsonAliases("b", "bool"));
        AddBuiltinType(DSNamedType.NewClassType(DSKeywords.TYPE_NAME_STRING).AddDsonAliases("s", "string"));
        AddBuiltinType(DSNamedType.NewClassType(DSKeywords.TYPE_NAME_BYTES).AddDsonAliases("bin", "binary"));
        // 内建结构
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_DATETIME).AddDsonAliases("DateTime")); // 不是Dson内建结构
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_TIMESTAMP).AddDsonAliases("ts", "Timestamp")); // ts是Dson内建结构
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_PAIR).AddDsonAliases("Pair", "KeyValuePair"));
        // 基础容器
        AddBuiltinType(DSNamedType.NewClassType(DSKeywords.TYPE_NAME_LIST).AddDsonAliases("List"));
        AddBuiltinType(DSNamedType.NewClassType(DSKeywords.TYPE_NAME_HASH_SET).AddDsonAliases("HashSet", "Set"));
        AddBuiltinType(DSNamedType.NewClassType(DSKeywords.TYPE_NAME_MAP).AddDsonAliases("Map", "Dictionary"));
        // 装箱类型
        AddBuiltinType(DSNamedType.NewClassType(DSKeywords.TYPE_NAME_OBJECT).AddDsonAliases("Object", "object"));
        AddBuiltinType(DSNamedType.NewStructType(DSKeywords.TYPE_NAME_NULLABLE, new List<DSTypeParameter>(1)
        {
            new DSTypeParameter("T", TypeParameterConstraints.ValueTypeConstraint)
        }).AddDsonAliases("Nullable"));
    }

    #region props

    /// <summary>
    /// 虚拟全局文件
    /// </summary>
    public DSFile GlobalFile => globalFile;
    /// <summary>
    /// 真实文件字典
    /// </summary>
    public LinkedDictionary<string, DSFile> FileMap => fileMap;
    /// <summary>
    /// 逻辑文件字典(包含虚拟文件)
    /// </summary>
    public LinkedDictionary<string, DSFile> LogicFileMap => logicFileMap;
    /// <summary>
    /// 所有的Handler--可按应用修改
    /// </summary>
    public Dictionary<string, DSTypeHandler> HandlerMap => handlerMap;

    #endregion

    #region file

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="dsFile"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public void AddFile(DSFile dsFile) {
        if (!logicFileMap.TryAdd(dsFile.SimpleName, dsFile)) {
            throw new ArgumentException("duplicate fileName " + dsFile.SimpleName);
        }
        if (!dsFile.IsVirtualFile) {
            fileMap.Add(dsFile.SimpleName, dsFile);
        }
    }

    /// <summary>
    /// 获取排序后的所有的文件 -- 根据文件名排序，有助于逻辑的稳定性
    /// </summary>
    /// <returns></returns>
    public List<DSFile> GetSortedFiles() {
        List<DSFile> result = new(fileMap.Values);
        result.Sort((a, b) => string.Compare(a.SimpleName, b.SimpleName, StringComparison.Ordinal));
        return result;
    }

    /// <summary>
    /// 获取指定文件
    /// </summary>
    /// <param name="simpleName">文件简单名，不包含proto后缀</param>
    /// <returns></returns>
    public DSFile? GetFile(string simpleName) {
        fileMap.TryGetValue(simpleName, out DSFile dsFile);
        return dsFile;
    }

    #endregion

    #region query

    /// <summary>
    /// 根据name查询类型
    /// 1.该接口必须从顶层类查询，支持指定文件名。
    /// 2.如果不指定文件名，则会尝试匹配所有文件。
    /// 
    /// <code>FileSimpleName.A.B.C</code>
    /// <code>A.B.C</code>
    /// </summary>
    /// <param name="typeName">类型名</param>
    /// <returns></returns>
    public DSNamedType? GetType(string typeName) {
        IndexKey indexKey = new IndexKey(isInst: false, typeName);
        if (indexedElementMap.TryGetValue(indexKey, out DSElement element)) {
            return element as DSNamedType;
        }
        // 不存在精确匹配项，要么文件名错误，要么不包含文件名
        foreach (DSFile dsFile in logicFileMap.Values) {
            DSNamedType namedType = dsFile.GetType(typeName);
            if (namedType != null) return namedType;
        }
        return null;
    }

    /// <summary>
    /// 根据name查询实例
    /// 1.该接口必须从顶层类查询，支持指定文件名。
    /// 2.如果不指定文件名，则会尝试匹配所有文件。
    /// 
    /// <code>FileSimpleName.InstName</code>
    /// <code>InstName</code>
    /// </summary>
    /// <param name="instName"></param>
    /// <returns></returns>
    public DSInst? GetInst(string instName) {
        IndexKey indexKey = new IndexKey(isInst: true, instName);
        if (indexedElementMap.TryGetValue(indexKey, out DSElement element)) {
            return element as DSInst;
        }
        // 不存在精确匹配项，要么文件名错误，要么不包含文件名
        foreach (DSFile dsFile in logicFileMap.Values) {
            DSInst inst = dsFile.GetInst(instName);
            if (inst != null) return inst;
        }
        return null;
    }

    /// <summary>
    /// 添加内建类型
    /// (需要在build之前执行)
    /// </summary>
    /// <param name="builtinType"></param>
    public void AddBuiltinType(DSNamedType builtinType) {
        globalFile.AddEnclosedElement(builtinType);
    }

    /// <summary>
    /// 获取内建类型
    /// </summary>
    public DSNamedType GetBuiltinType(string typeName) {
        // 该方法可能在Build之前调用以修正内建结构数据
        if (globalFile.TypeMap.IsEmpty) {
            foreach (DSElement enclosedElement in globalFile.EnclosedElements) {
                if (enclosedElement.Kind.IsNamedType() && enclosedElement.SimpleName == typeName) {
                    return (DSNamedType)enclosedElement;
                }
            }
            throw new ArgumentException("invalid typeName: " + typeName);
        }
        DSNamedType result = globalFile.GetType(typeName);
        if (result == null) {
            throw new ArgumentException("invalid typeName: " + typeName);
        }
        return result;
    }

    /// <summary>
    /// 添加类型关联的handler
    /// 
    /// </summary>
    /// <param name="fullName">FileName.A.B</param>
    /// <param name="handler"></param>
    public void AddTypeHandler(string fullName, DSTypeHandler handler) {
        handlerMap.Add(fullName, handler);
    }

    /// <summary>
    /// 获取类型关联的handler
    /// </summary>
    /// <param name="fullName">FileName.A.B</param>
    /// <returns></returns>
    public DSTypeHandler? GetTypeHandler(string fullName) {
        handlerMap.TryGetValue(fullName, out DSTypeHandler handler);
        return handler;
    }

    #endregion

    #region 泛型

    /// <summary>
    /// 构造一个可空类型
    /// </summary>
    /// <param name="typeArgument"></param>
    /// <returns></returns>
    public DSNamedType MakeNullableType(DSTypeElement typeArgument) {
        DSNamedType nullableType = GetBuiltinType(DSKeywords.TYPE_NULLABLE);
        return MakeGenericType(nullableType, new List<DSTypeElement>() { typeArgument });
    }

    /// <summary>
    /// 构造泛型类
    /// 注意：该方法只会初始化该类和超类的字段，不会初始化内部类和外部类。
    ///
    /// Q：为什么定义在这里？
    /// A：因为符号解析需要由repository处理。
    /// </summary>
    /// <param name="namedType">要处理的泛型</param>
    /// <param name="typeArguments">泛型实参</param>
    /// <returns></returns>
    public DSNamedType MakeGenericType(DSNamedType namedType, List<DSTypeElement> typeArguments) {
        if (!namedType.IsGenericTypeDefinition) throw new InvalidOperationException();
        if (typeArguments.Count != namedType.TypeParameters.Count) {
            throw new ArgumentException("typeArguments.Length != _typeParameters.Count");
        }
        // 避免每个符号都创建一个Type
        ClassName className = MakeGenericClassName(namedType.TypeName, typeArguments);
        if (genericTypeCache.TryGetValue(className, out DSNamedType result)) {
            Debug.Assert(!result.IsGenericTypeDefinition, "result is genericTypeDefinition");
            return result;
        }
        result = new DSNamedType(namedType, className, typeArguments);
        genericTypeCache.Add(className, result);

        // 克隆字段
        foreach (DSField field in namedType.GetFields(false)) {
            DSTypeElement fieldType = ResolveTypeSymbol(result, field.TypeSymbol);
            result.AddEnclosedElement(new DSField(field, fieldType));
        }
        // 递归构造超类
        if (namedType.BaseTypeSymbol != null) {
            result.BaseType = (DSNamedType)ResolveTypeSymbol(result, namedType.BaseTypeSymbol);
        }
        // 缓存CodecName
        result.DsonTypeName = NameOfType(result);
        return result;
    }

    private static ClassName MakeGenericClassName(ClassName origin, List<DSTypeElement> typeArguments) {
        List<TypeName> typeArgumentNames = new List<TypeName>(typeArguments.Count);
        foreach (DSTypeElement typeArgument in typeArguments) {
            typeArgumentNames.Add(typeArgument.TypeName);
        }
        return origin.WithTypeArguments(typeArgumentNames.ToArray());
    }

    #endregion

    #region resolve-type

    /// <summary>
    /// 如果typeSymbol是泛型类型，则会构造目标泛型
    /// </summary>
    public DSTypeElement ResolveTypeSymbol(DSElement? scopeEntry, string typeSymbol) {
        return ResolveTypeSymbol(scopeEntry, DSTypeSymbol.Parse(typeSymbol));
    }

    /// <summary>
    /// 如果typeSymbol是泛型类型，则会构造目标泛型
    ///
    /// </summary>
    /// <param name="scopeEntry">作用域入口，即从哪里访问目标类型；可以是文件或类型；如果为null，则表示使用全局作用域</param>
    /// <param name="typeSymbol">引用的类型符号</param>
    /// <returns></returns>
    private DSTypeElement ResolveTypeSymbol(DSElement? scopeEntry, DSTypeSymbol typeSymbol) {
        // 这里不能建立查询缓存，因为scopeEntry不同，结果可能不同...
        DSTypeElement typeElement = FindType(scopeEntry, typeSymbol.name);
        if (typeElement == null) {
            throw new InvalidOperationException("cant resolve typeSymbol: " + typeSymbol.symbol);
        }
        // 找到的可能是泛型变量
        if (typeElement is DSTypeParameter typeParameter) {
            // '?'作用于值类型时需要转为Nullable类型
            return (typeParameter.HasValueTypeConstraint && typeSymbol.isNullable)
                ? MakeNullableType(typeParameter)
                : typeParameter;
        }
        // 非泛型
        if (typeSymbol.typeArguments == null) {
            // '?'作用于值类型时需要转为Nullable类型
            return (typeSymbol.isNullable && typeElement.IsValueType)
                ? MakeNullableType(typeElement)
                : typeElement;
        }
        // 构建泛型 -- 还好我们没数组，不然还要处理数组的问题
        List<DSTypeElement> typeArguments = new List<DSTypeElement>(typeSymbol.typeArguments.Count);
        foreach (DSTypeSymbol typeArgumentSymbol in typeSymbol.typeArguments) {
            DSTypeElement typeArgument = ResolveTypeSymbol(scopeEntry, typeArgumentSymbol);
            typeArguments.Add(typeArgument);
        }
        DSNamedType namedType = (DSNamedType)typeElement;
        return MakeGenericType(namedType, typeArguments);
    }

    /// <summary>
    /// 查找类型(原始类型)
    /// 从内部类、外部类、内建类型以及导入的文件查询
    /// 
    /// 1.如果是解析字段的typeSymbol，scopeEntry为字段的声明类
    /// 2.如果是解析超类的typeSymbol，scopeEntry为子类
    /// </summary>
    /// <param name="scopeEntry">作用域的入口，还用于解析泛型参数；null表示全局作用域</param>
    /// <param name="typeName">类型名，可能是A.B.C</param>
    /// <returns>可能是泛型参数</returns>
    private DSTypeElement? FindType(DSElement? scopeEntry, string typeName) {
        Debug.Assert(!typeName.Contains('?'));
        // 从全局作用域查询
        if (scopeEntry == null) {
            return GetType(typeName);
        }
        // 查询内建类型 -- 内建类型不限作用域；基础类型使用频率也最高
        DSNamedType? r = globalFile.GetType(typeName);
        if (r != null) {
            return r;
        }
        DSFile enclosingFile;
        if (scopeEntry is DSNamedType namedType) {
            enclosingFile = namedType.GetEnclosingFile();
            // 查找泛型变量 -- 需要通过泛型原型查询；symbol总是基于泛型定义类编写的
            ImmutableList<DSTypeParameter> typeParameters = namedType.OriginDefine.TypeParameters;
            for (int idx = 0; idx < typeParameters.Count; idx++) {
                var typeParameter = typeParameters[idx];
                if (typeParameter.SimpleName == typeName) {
                    return namedType.IsGenericTypeDefinition ? typeParameter : namedType.TypeArguments[idx];
                }
            }
            // 在当前文件内部查询
            // 当typeName是A.B.C的时候，不确定A是内部类还是父兄类，先根据A定位
            int spIndex = typeName.IndexOf('.');
            string firstName = spIndex < 0 ? typeName : typeName.Substring2(0, spIndex);
            r = FindFirstType(namedType, firstName);
            if (r != null) {
                return spIndex < 0 ? r : FindEnclosedType(r, typeName.Substring2(spIndex + 1));
            }
        } else {
            enclosingFile = (DSFile)scopeEntry;
            r = enclosingFile.GetType(typeName);
            if (r != null) {
                return r;
            }
        }
        // 从导入的文件中查询 -- 提前缓存了import，因此不是递归调用FindType
        foreach (string resolvedImport in enclosingFile.ResolvedImports) {
            if (!logicFileMap.TryGetValue(resolvedImport, out DSFile importFile)) {
                continue;
            }
            DSNamedType? typeElement = importFile.GetType(typeName);
            if (typeElement != null) return typeElement;
        }
        // 查找失败
        return null;
    }

    private static DSNamedType? FindFirstType(DSNamedType scopeEntry, string firstName) {
        scopeEntry = scopeEntry.OriginDefine;
        if (scopeEntry.SimpleName == firstName) {
            return scopeEntry;
        }
        // 先查询内部类--禁止直接访问内部类的内部类，必须是A.B.C相对路径格式访问
        foreach (DSElement enclosedElement in scopeEntry.EnclosedElements) {
            if (!enclosedElement.Kind.IsNamedType()) continue;
            if (enclosedElement.SimpleName == firstName) {
                return (DSNamedType)enclosedElement;
            }
        }
        // 平级节点、父节点、叔父节点--递归向上，广度优先遍历
        var enclosingElement = scopeEntry.EnclosingElement;
        while (enclosingElement != null) {
            foreach (DSElement peerElement in enclosingElement.EnclosedElements) {
                if (ReferenceEquals(peerElement, scopeEntry)) continue;
                if (!peerElement.Kind.IsNamedType()) continue;
                if (peerElement.SimpleName == firstName) {
                    return (DSNamedType?)peerElement;
                }
            }
            enclosingElement = enclosingElement.EnclosingElement;
        }
        return null;
    }

    private static DSNamedType? FindEnclosedType(DSElement root, string accessName) {
        int idx = accessName.IndexOf('.');
        if (idx < 0) {
            string firstName = accessName;
            foreach (DSElement enclosedElement in root.EnclosedElements) {
                if (enclosedElement.Kind.IsNamedType() && enclosedElement.SimpleName == firstName) {
                    return (DSNamedType)enclosedElement;
                }
            }
            return null;
        } else {
            string firstName = accessName.Substring2(0, idx);
            foreach (DSElement enclosedElement in root.EnclosedElements) {
                if (enclosedElement.Kind.IsNamedType() && enclosedElement.SimpleName == firstName) {
                    return FindEnclosedType(enclosedElement, accessName.Substring2(idx + 1));
                }
            }
            return null;
        }
    }

    #endregion

    #region Resolve-DsonTypeName

    /// <summary>
    /// 根据类型的序列化名字计算其真实类型
    ///
    /// 1.这里我们不验证作用域，因为Name通常来源于最终数据文件，而非DS文件。
    /// 2.这部分实现可参考<see cref="DynamicTypeMetaRegistry"/>
    /// </summary>
    /// <param name="clsName">对象的序列化名字</param>
    /// <returns></returns>
    public DSNamedType? ResolveDsonTypeName(string clsName) {
        if (dsonAlias2TypeDic.TryGetValue(clsName, out DSNamedType result)
            || dsonClsName2TypeDic.TryGetValue(clsName, out result)) {
            return result;
        }
        DsonTypeName typeName = DsonTypeName.Parse(clsName);
        DSNamedType namedType = TypeOfName(typeName);
        // 这里不测试是否包含空白字符，因为这里不是运行时，额外的缓存没有太大的影响
        dsonClsName2TypeDic.Add(clsName, namedType);
        return namedType;
    }

    /// <summary>
    /// 类型的编解码名字 -- 实时构造，外部缓存
    /// </summary>
    private static DsonTypeName NameOfType(DSTypeElement type) {
        // 泛型参数
        if (type is DSTypeParameter typeParameter) {
            return new DsonTypeName(typeParameter.SimpleName);
        }
        DSNamedType namedType = (DSNamedType)type;
        if (namedType.IsGenericTypeDefinition) {
            // 泛型原型
            string mainClsName = namedType.DsonAliases[0];
            ImmutableList<DSTypeParameter> genericTypeArgs = namedType.TypeParameters;
            List<DsonTypeName> typeArgClassNames = new List<DsonTypeName>(genericTypeArgs.Count);
            foreach (var genericTypeArg in genericTypeArgs) {
                typeArgClassNames.Add(new DsonTypeName(genericTypeArg.SimpleName));
            }
            return new DsonTypeName(mainClsName, typeArgClassNames);
        }
        if (namedType.IsGenericType) {
            // 已构造泛型
            string mainClsName = namedType.OriginDefine.DsonAliases[0];
            ImmutableList<DSTypeElement> genericTypeArgs = namedType.TypeArguments; // 真实泛型参数
            List<DsonTypeName> typeArgClassNames = new List<DsonTypeName>(genericTypeArgs.Count);
            foreach (var genericTypeArg in genericTypeArgs) {
                typeArgClassNames.Add(NameOfType(genericTypeArg));
            }
            return new DsonTypeName(mainClsName, typeArgClassNames);
        }
        // 非泛型类
        return new DsonTypeName(namedType.DsonAliases[0]);
    }

    /// <summary>
    /// 根据编解码名字计算其真实类型
    /// </summary>
    private DSNamedType TypeOfName(DsonTypeName typeName) {
        if (!dsonAlias2TypeDic.TryGetValue(typeName.name, out DSNamedType originDefine)) {
            throw new InvalidOperationException("invalid typeName: " + typeName);
        }
        if (!originDefine.IsGenericType) {
            return originDefine;
        }
        int typeArgsCount = typeName.typeArgs.Count;
        List<DSTypeElement> typeArgs = new List<DSTypeElement>(typeArgsCount);
        for (int index = 0; index < typeArgsCount; index++) {
            DsonTypeName typeNameArg = typeName.typeArgs[index]; // 可能是泛型变量
            if (typeNameArg.name == originDefine.TypeParameters[index].SimpleName) {
                typeArgs[index] = originDefine.TypeParameters[index];
            } else {
                typeArgs[index] = TypeOfName(typeNameArg);
            }
        }
        return MakeGenericType(originDefine, typeArgs);
    }

    #endregion

    #region build

    /// <summary>
    /// 构建最终数据
    ///
    /// 1.解析文件之间依赖
    /// 2.解析类型引用：字段类型，超类类型
    /// 3.解析实例之间的引用：模板引用
    /// </summary>
    public void Build() {
        // 初始化File缓存
        foreach (DSFile dsFile in logicFileMap.Values) {
            dsFile.BuildCache();
            foreach (DSNamedType namedType in dsFile.TypeMap.Values) {
                indexedElementMap.Add(new IndexKey(isInst: false, namedType.FullName), namedType);
                InitCodecInfo(namedType);
            }
            foreach (DSInst inst in dsFile.InstMap.Values) {
                string fullName = dsFile.SimpleName + "." + inst.SimpleName;
                indexedElementMap.Add(new IndexKey(isInst: true, fullName), inst);
            }
        }
        // 解析import
        HashSet<string> tempSet = new HashSet<string>(16);
        foreach (DSFile file in logicFileMap.Values) {
            tempSet.Clear();
            ResolvePublicImports(file, tempSet, 0);
            file.ResolvedImports.AddAll(tempSet);
        }
        // 解析字段和超类类型
        foreach (DSFile file in logicFileMap.Values) {
            try {
                ResolveTypeSymbols(file);
            }
            catch (Exception e) {
                throw new Exception("file: " + file.FileName, e);
            }
        }
        // 解析实例引用： inst $name from t1, t2 ...
        foreach (DSFile file in logicFileMap.Values) {
            try {
                foreach (DSInst inst in file.GetInsts()) {
                    BuildInst(inst, 0);
                }
            }
            catch (Exception e) {
                throw new Exception("file: " + file.FileName, e);
            }
        }
    }

    private void InitCodecInfo(DSNamedType namedType) {
        DsonObject<string> options = DSUtil.GetCodecOptions(namedType);
        namedType.DsonStyle = DSUtil.GetObjectStyle(options, namedType.DsonStyle);
        // 用户可能手动指定了别名
        if (namedType.DsonAliases.Count == 0) {
            if (options.TryGetValue(DSAnnotations.KEY_ALIAS, out DsonValue dsonValue) && dsonValue.AsArray().Count > 0) {
                foreach (DsonValue alias in dsonValue.AsArray()) {
                    namedType.DsonAliases.Add(alias.AsString());
                }
            } else {
                string alias = DSUtil.RemoveFirstName(namedType.FullName);
                namedType.DsonAliases.Add(alias);
            }
        }
        // 加入缓存 -- 冲突时抛异常
        foreach (string alias in namedType.DsonAliases) {
            dsonAlias2TypeDic.Add(alias, namedType);
        }
        // 此时都是泛型原型，无需延迟处理
        namedType.DsonTypeName = NameOfType(namedType);
    }

    /// <summary>
    /// 解析文件的继承得到的公有依赖
    /// </summary>
    private void ResolvePublicImports(DSFile entryFile, HashSet<string> result, int deep) {
        if (deep > 32) {
            throw new InvalidOperationException("something is error, deep: " + deep);
        }
        foreach (string importFileName in entryFile.Imports.Keys) {
            string fileSimpleName = Path.GetFileNameWithoutExtension(importFileName);
            DSFile curFile = GetFile(fileSimpleName);
            if (curFile == null) {
                throw new InvalidOperationException($"{entryFile.FileName} cant resolve import: {importFileName}");
            }
            foreach (var pair in curFile.Imports) {
                if (pair.Value == DSKeywords.PUBLIC) {
                    result.Add(pair.Key);
                    ResolvePublicImports(curFile, result, deep + 1);
                }
            }
        }
    }

    /// <summary>
    /// 解析文件内的所有类型引用
    /// (超类和字段)
    /// </summary>
    /// <param name="file"></param>
    private void ResolveTypeSymbols(DSFile file) {
        List<DSField> fieldList = new List<DSField>(20);
        foreach (DSNamedType typeElement in DSUtil.GetAllEnclosedTypes(file)) {
            if (typeElement.BaseTypeSymbol != null && typeElement.BaseType == null) {
                typeElement.BaseType = (DSNamedType)ResolveTypeSymbol(typeElement, typeElement.BaseTypeSymbol);
            }
            foreach (DSField field in typeElement.GetFields(false, fieldList.ClearAndReturn())) {
                if (field.TypeSymbol != null && field.Type == null) {
                    field.Type = ResolveTypeSymbol(typeElement, field.TypeSymbol);
                }
            }
        }
    }

    /// <summary>
    /// 构建单个实例
    /// </summary>
    private void BuildInst(DSInst inst, int deep) {
        if (deep > 32) throw new Exception();
        if (inst.DsonValue != null) {
            return;
        }
        if (inst.Templates.Count == 0) {
            inst.DsonValue = Dsons.FromDson(inst.Value);
            return;
        }
        // 从模板开始初始化
        DsonObject<string> dsonObject = new DsonObject<string>();
        foreach (string template in inst.Templates) {
            DSFile file = (DSFile)inst.EnclosingElement;
            DSInst? instTemplate = FindInst(file, template);
            if (instTemplate == null) {
                throw new InvalidOperationException("cant resolve inst template: " + template);
            }
            if (instTemplate.DsonValue == null) {
                BuildInst(instTemplate, deep + 1);
            }
            // 需要避免内存共享
            DsonObject<string> copiedObject = Dsons.MutableDeepCopy<string>(instTemplate.DsonValue!).AsObject();
            foreach (var pair in copiedObject) {
                dsonObject[pair.Key] = pair.Value;
            }
        }
        // 再初始化自身数据
        foreach (var pair in Dsons.FromDson(inst.Value).AsObject()) {
            dsonObject[pair.Key] = pair.Value;
        }
        inst.DsonValue = dsonObject;
    }

    private DSInst? FindInst(DSFile? scopeEntry, string instName) {
        // 未指定作用域，从所有文件查询
        DSInst inst;
        if (scopeEntry == null) {
            foreach (DSFile dsFile in logicFileMap.Values) {
                inst = dsFile.GetInst(instName);
                if (inst != null) return inst;
            }
            return null;
        }
        // 先从当前文件查询
        inst = scopeEntry.GetInst(instName);
        if (inst != null) return inst;

        // 再从导入的文件查询
        foreach (string resolvedImport in scopeEntry.ResolvedImports) {
            if (!logicFileMap.TryGetValue(resolvedImport, out DSFile importFile)) continue;
            inst = importFile.GetInst(instName);
            if (inst != null) {
                return inst;
            }
        }
        return null;
    }

    #endregion

    #region internal

    private readonly struct IndexKey : IEquatable<IndexKey>
    {
        public readonly bool isInst;
        public readonly string fullName;

        public IndexKey(bool isInst, string fullName) {
            this.isInst = isInst;
            this.fullName = fullName;
        }

        public bool Equals(IndexKey other) {
            return isInst == other.isInst && fullName == other.fullName;
        }

        public override bool Equals(object? obj) {
            return obj is IndexKey other && Equals(other);
        }

        public override int GetHashCode() {
            return (isInst.GetHashCode() * 397) ^ fullName.GetHashCode();
        }

        public static bool operator ==(IndexKey left, IndexKey right) {
            return left.Equals(right);
        }

        public static bool operator !=(IndexKey left, IndexKey right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return $"{nameof(isInst)}: {isInst}, {nameof(fullName)}: {fullName}";
        }
    }

    #endregion
}
}