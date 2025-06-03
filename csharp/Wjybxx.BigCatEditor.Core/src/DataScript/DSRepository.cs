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
using Wjybxx.BigCatEditor.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 
/// 注意：与DS的仓库不同，我们的数据脚本顶层有Inst类型，而Inst的名字可以和其它元素重复。
/// </summary>
public class DSRepository
{
    /// <summary>
    /// 文件简单名到文件的映射
    /// </summary>
    private readonly LinkedDictionary<string, DSFile> fileMap = new();
    /// <summary>
    /// 顶层元素名到元素的映射
    /// key为[fileSimpleName, elementName]
    ///
    /// 跨文件访问时，只可访问其它文件的顶层类型
    /// </summary>
    private readonly LinkedDictionary<StringPair, DSNamedType> topTypeMap = new();
    /// <summary>
    /// 所有的实例映射
    /// key为[fileSimpleName, elementName]
    ///
    /// 所有的实例都属于顶层元素，且也按文件存储，即只能访问import的文件中的实例。
    /// </summary>
    private readonly LinkedDictionary<StringPair, DSInst> instanceMap = new();
    /// <summary>
    /// 顶层元素名到元素的映射，用于解决查询效率问题
    /// key为elementName
    /// </summary>
    private readonly LinkedDictionary<IndexKey, DSElement> indexedTopElementMap = new();

    /// <summary>
    /// 内建类型字典
    /// </summary>
    private readonly LinkedDictionary<string, DSNamedType> builtinTypeMap = new();
    /// <summary>
    /// 类型解析缓存(含内建类型)
    /// </summary>
    private readonly Dictionary<ClassName, DSNamedType> resolveCache = new();

    public DSRepository() {
        AddBuiltinTypes();
        foreach (var namedTypeElement in builtinTypeMap.Values) {
            resolveCache.Add(namedTypeElement.TypeName, namedTypeElement);
        }
    }

    private void AddBuiltinTypes() {
        // 原子类型
        builtinTypeMap[DSKeywords.TYPE_INT32] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_INT32);
        builtinTypeMap[DSKeywords.TYPE_INT64] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_INT64);
        builtinTypeMap[DSKeywords.TYPE_FLOAT] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_FLOAT);
        builtinTypeMap[DSKeywords.TYPE_DOUBLE] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_DOUBLE);
        builtinTypeMap[DSKeywords.TYPE_BOOL] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_BOOL);
        builtinTypeMap[DSKeywords.TYPE_STRING] = DSNamedType.NewClassType(DSKeywords.TYPE_NAME_STRING);
        builtinTypeMap[DSKeywords.TYPE_BYTES] = DSNamedType.NewClassType(DSKeywords.TYPE_NAME_BYTES);
        // 基础容器
        builtinTypeMap[DSKeywords.TYPE_LIST] = DSNamedType.NewClassType(DSKeywords.TYPE_NAME_LIST);
        builtinTypeMap[DSKeywords.TYPE_MAP] = DSNamedType.NewClassType(DSKeywords.TYPE_NAME_MAP);
        builtinTypeMap[DSKeywords.TYPE_PAIR] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_PAIR);
        // 装箱类型
        builtinTypeMap[DSKeywords.TYPE_OBJECT] = DSNamedType.NewClassType(DSKeywords.TYPE_NAME_OBJECT);
        builtinTypeMap[DSKeywords.TYPE_NULLABLE] = DSNamedType.NewStructType(DSKeywords.TYPE_NAME_NULLABLE);
    }

    /// <summary>
    /// 添加文件
    /// </summary>
    /// <param name="pbFile"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DSRepository AddFile(DSFile pbFile) {
        string simpleName = pbFile.SimpleName;
        // 检查重复
        if (fileMap.ContainsKey(simpleName)) {
            throw new ArgumentException("duplicate fileName " + simpleName);
        }
        fileMap[simpleName] = pbFile;
        // 添加索引，类型和实例分开
        foreach (DSElement element in pbFile.EnclosedElements) {
            var key = new StringPair(simpleName, element.SimpleName);
            bool isInst = element.Kind == DSElementKind.Inst;
            if (isInst) {
                instanceMap.Add(key, (DSInst)element);
            } else {
                topTypeMap.Add(key, (DSNamedType)element);
            }
            IndexKey indexKey = new IndexKey(isInst, element.SimpleName);
            indexedTopElementMap.TryAdd(indexKey, element);
        }
        // 添加已解析缓存
        foreach (DSNamedType namedTypeElement in DSUtil.GetAllEnclosedTypes(pbFile)) {
            resolveCache.Add(namedTypeElement.TypeName, namedTypeElement);
        }
        return this;
    }

    /// <summary>
    /// 获取所有的文件 -- 不可修改
    /// </summary>
    /// <returns></returns>
    public ICollection<DSFile> GetFiles() {
        return fileMap.Values;
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
        fileMap.TryGetValue(simpleName, out DSFile pbFile);
        return pbFile;
    }

    /// <summary>
    /// 获取顶层类型
    /// (只查询顶层类)
    /// </summary>
    /// <param name="fileSimpleName">文件简单名</param>
    /// <param name="elementName">顶层元素名</param>
    /// <returns></returns>
    public DSNamedType? GetType(string fileSimpleName, string elementName) {
        var key = new StringPair(fileSimpleName, elementName);
        topTypeMap.TryGetValue(key, out DSNamedType element);
        return element;
    }

    /// <summary>
    /// 根据name查询类型，会返回第一个匹配的类型
    /// (只查询顶层类)
    /// </summary>
    /// <param name="elementName"></param>
    /// <returns></returns>
    public DSNamedType? FindType(string elementName) {
        IndexKey key = new IndexKey(isInst: false, elementName);
        indexedTopElementMap.TryGetValue(key, out DSElement element);
        return element as DSNamedType;
    }

    /// <summary>
    /// 获取顶层实例
    /// </summary>
    /// <param name="fileSimpleName"></param>
    /// <param name="elementName"></param>
    /// <returns></returns>
    public DSInst? GetInst(string fileSimpleName, string elementName) {
        var key = new StringPair(fileSimpleName, elementName);
        instanceMap.TryGetValue(key, out DSInst element);
        return element;
    }

    /// <summary>
    /// 根据name查询实例，会返回第一个匹配的实例
    /// </summary>
    /// <param name="elementName"></param>
    /// <returns></returns>
    public DSInst? FindInst(string elementName) {
        IndexKey key = new IndexKey(isInst: true, elementName);
        indexedTopElementMap.TryGetValue(key, out DSElement element);
        return element as DSInst;
    }

    /// <summary>
    /// 查询是否是内建类型
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public bool IsBuiltinType(string typeSymbol) {
        return builtinTypeMap.ContainsKey(typeSymbol);
    }

    /// <summary>
    /// 获取内建类型
    /// </summary>
    public DSNamedType GetBuiltinType(string typeSymbol) {
        if (builtinTypeMap.TryGetValue(typeSymbol, out DSNamedType result)) {
            return result;
        }
        throw new ArgumentException("invalid typeSymbol: " + typeSymbol);
    }

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
        if (resolveCache.TryGetValue(className, out DSNamedType result)) {
            return result;
        }
        result = new DSNamedType(namedType, className, typeArguments);
        resolveCache.Add(className, result);

        // 克隆字段
        foreach (DSField field in namedType.GetFields(false)) {
            DSTypeElement fieldType = ResolveTypeSymbol(result, field.TypeSymbol);
            result.AddEnclosedElement(new DSField(field, fieldType));
        }
        // 递归构造超类
        if (namedType.BaseTypeSymbol != null) {
            result.BaseType = (DSNamedType)ResolveTypeSymbol(result, namedType.BaseTypeSymbol);
        }
        return result;
    }

    private static ClassName MakeGenericClassName(ClassName origin, List<DSTypeElement> typeArguments) {
        List<TypeName> typeArgumentNames = new List<TypeName>(typeArguments.Count);
        foreach (DSTypeElement typeArgument in typeArguments) {
            typeArgumentNames.Add(typeArgument.TypeName);
        }
        return origin.WithTypeArguments(typeArgumentNames.ToArray());
    }

    #region build

    /// <summary>
    /// 构建最终数据
    ///
    /// 1.解析文件之间依赖
    /// 2.解析类型引用：字段类型，超类类型
    /// 3.解析实例之间的引用：模板引用
    /// </summary>
    public void Build() {
        // 解析import
        HashSet<string> tempSet = new HashSet<string>(16);
        foreach (DSFile file in fileMap.Values) {
            tempSet.Clear();
            ResolvePublicImports(file, tempSet, 0);
            file.ResolvedImports.AddAll(tempSet);
        }
        // 解析字段和超类类型
        foreach (DSFile file in fileMap.Values) {
            try {
                ResolveTypeSymbols(file);
            }
            catch (Exception e) {
                throw new Exception("file: " + file.FileName, e);
            }
        }
        // 解析实例引用： inst $name from t1, t2 ...
        foreach (DSFile file in fileMap.Values) {
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
        foreach (DSNamedType typeElement in DSUtil.GetAllEnclosedTypes(file)) {
            if (typeElement.BaseTypeSymbol != null && typeElement.BaseType == null) {
                typeElement.BaseType = (DSNamedType)ResolveTypeSymbol(typeElement, typeElement.BaseTypeSymbol);
                continue;
            }
            foreach (DSField field in typeElement.GetFields(false)) {
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

    /// <summary>
    /// 查找
    /// </summary>
    /// <param name="scopeEntry"></param>
    /// <param name="instName"></param>
    /// <returns></returns>
    private DSInst? FindInst(DSFile scopeEntry, string instName) {
        // 先从当前文件查询，再从导入的文件查询
        DSInst inst = GetInst(scopeEntry.SimpleName, instName);
        if (inst != null) {
            return inst;
        }
        foreach (string resolvedImport in scopeEntry.ResolvedImports) {
            inst = GetInst(resolvedImport, instName);
            if (inst != null) {
                return inst;
            }
        }
        return null;
    }

    #endregion

    #region resolve-type

    /// <summary>
    /// 如果typeSymbol是泛型类型，则会构造目标泛型
    ///
    /// 这里不能直接建立typeSymbol到结果的缓存，因为scopeEntry可能不同...
    /// </summary>
    /// <param name="scopeEntry">作用域入口，包含必要的泛型参数</param>
    /// <param name="typeSymbol">引用的类型符号</param>
    /// <returns></returns>
    public DSTypeElement ResolveTypeSymbol(DSNamedType scopeEntry, string typeSymbol) {
        typeSymbol = ObjectUtil.DeleteWhitespace(typeSymbol);
        return ResolveTypeSymbol(scopeEntry, DSTypeSymbol.Parse(typeSymbol));
    }

    private DSTypeElement ResolveTypeSymbol(DSNamedType scopeEntry, DSTypeSymbol typeSymbol) {
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
    /// 查找类型
    /// 从内部类、外部类、内建类型以及导入的文件查询
    ///
    /// 1.如果是解析字段的typeSymbol，scopeEntry为字段的声明类
    /// 2.如果是解析超类的typeSymbol，scopeEntry为子类
    /// </summary>
    /// <param name="scopeEntry">作用域的入口，还用于解析泛型参数</param>
    /// <param name="typeSymbol">引用的类型符号，非泛型类型，也不是非空类型</param>
    /// <returns>可能是泛型参数</returns>
    private DSTypeElement? FindType(DSNamedType scopeEntry, string typeSymbol) {
        Debug.Assert(!typeSymbol.Contains('?'));
        // 查询内建类型 -- 基础类型使用频率最高
        if (builtinTypeMap.TryGetValue(typeSymbol, out DSNamedType r)) {
            return r;
        }
        // 查找泛型变量 -- 需要通过泛型原型查询；symbol总是基于泛型定义类编写的
        for (int idx = 0; idx < scopeEntry.OriginDefine.TypeParameters.Count; idx++) {
            var typeParameter = scopeEntry.OriginDefine.TypeParameters[idx];
            if (typeParameter.SimpleName == typeSymbol) {
                return scopeEntry.IsGenericTypeDefinition ? typeParameter : scopeEntry.TypeArguments[idx];
            }
        }
        // 在当前文件内部查询
        List<DSNamedType> accessibleTypes = new List<DSNamedType>();
        CollectAccessibleTypes(scopeEntry, accessibleTypes);
        foreach (DSNamedType typeElement in accessibleTypes) {
            if (typeElement.SimpleName == typeSymbol) {
                return typeElement;
            }
        }
        // 从导入的文件中查询顶层类
        DSFile enclosingFile = scopeEntry.GetEnclosingFile();
        if (enclosingFile == null) {
            return null;
        }
        foreach (string resolvedImport in enclosingFile.ResolvedImports) {
            DSNamedType? typeElement = GetType(resolvedImport, typeSymbol);
            if (typeElement != null) return typeElement;
        }
        // 查找失败
        return null;
    }

    /// <summary>
    /// 收集一个类型可访问的所有类型（当前文件内）
    /// </summary>
    /// <param name="scopeEntry">作用域的入口</param>
    /// <param name="outList"></param>
    private void CollectAccessibleTypes(DSNamedType scopeEntry, List<DSNamedType> outList) {
        // 只有原始定义类才可以访问Elements
        scopeEntry = scopeEntry.OriginDefine;

        // 所有的内部类
        DSUtil.GetAllEnclosedTypes(scopeEntry, outList);
        // 当前类的平级类（同文件夹）
        foreach (DSElement peerElement in scopeEntry.EnclosingElement.EnclosedElements) {
            if (peerElement.IsTypeElement && !ReferenceEquals(peerElement, scopeEntry)) {
                outList.Add((DSNamedType)peerElement);
            }
        }
        // 直系祖先节点（不访问祖先的兄弟节点）
        var enclosingElement = scopeEntry.EnclosingElement;
        while (enclosingElement != null && enclosingElement.IsTypeElement) {
            outList.Add((DSNamedType)enclosingElement);
            enclosingElement = enclosingElement.EnclosingElement;
        }
    }

    #endregion

    private readonly struct IndexKey : IEquatable<IndexKey>
    {
        public readonly bool isInst;
        public readonly string simpleName;

        public IndexKey(bool isInst, string simpleName) {
            this.isInst = isInst;
            this.simpleName = simpleName;
        }

        public bool Equals(IndexKey other) {
            return isInst == other.isInst && simpleName == other.simpleName;
        }

        public override bool Equals(object? obj) {
            return obj is IndexKey other && Equals(other);
        }

        public override int GetHashCode() {
            return (isInst.GetHashCode() * 397) ^ simpleName.GetHashCode();
        }

        public static bool operator ==(IndexKey left, IndexKey right) {
            return left.Equals(right);
        }

        public static bool operator !=(IndexKey left, IndexKey right) {
            return !left.Equals(right);
        }

        public override string ToString() {
            return $"{nameof(isInst)}: {isInst}, {nameof(simpleName)}: {simpleName}";
        }
    }
}
}