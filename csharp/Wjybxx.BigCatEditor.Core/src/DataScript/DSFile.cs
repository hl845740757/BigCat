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
using System.IO;
using System.Linq;
using System.Text;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 文件
/// </summary>
public class DSFile : DSElement
{
    /** 文件名 */
    private readonly string fileName;
    /** 导入的文件 -- value可以为null，表示未声明修饰符 */
    private readonly LinkedDictionary<string, string?> imports = new(4);
    /** 解析后的所有依赖 -- 包括传递而来的依赖，无法保证解析顺序 */
    private readonly HashSet<string> resolvedImports = new(4);

    public DSFile(string fileName)
        : base(Path.GetFileNameWithoutExtension(fileName)) {
        this.fileName = fileName;
    }

    /// <summary>`
    /// 添加一个文件引用
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="modifier">修饰符</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public void AddImport(string fileName, string? modifier) {
        if (!fileName.EndsWith(".ds")) { // import需要以`.ds`结尾
            throw new ArgumentException();
        }
        this.imports.Add(fileName, modifier);
        this.resolvedImports.Add(fileName);
    }

    /// <summary>
    /// 获取所有的顶层类型
    /// </summary>
    /// <returns></returns>
    public List<DSNamedType> GetTypes() {
        return EnclosedElements.Where(e => e.Kind == DSElementKind.Class)
            .Cast<DSNamedType>()
            .ToList();
    }

    /// <summary>
    /// 获取所有的顶层类
    /// </summary>
    /// <returns></returns>
    public List<DSNamedType> GetClasses() {
        return EnclosedElements.Where(e => e.Kind == DSElementKind.Class)
            .Cast<DSNamedType>()
            .ToList();
    }

    /// <summary>
    /// 获取所有的顶层结构体
    /// </summary>
    /// <returns></returns>
    public List<DSNamedType> GetStructs() {
        return EnclosedElements.Where(e => e.Kind == DSElementKind.Strut)
            .Cast<DSNamedType>()
            .ToList();
    }

    /// <summary>
    /// 获取所有的顶层枚举
    /// </summary>
    /// <returns></returns>
    public List<DSNamedType> GetEnums() {
        return EnclosedElements.Where(e => e.Kind == DSElementKind.Enum)
            .Cast<DSNamedType>()
            .ToList();
    }

    /// <summary>
    /// 获取所有的顶层实例
    /// </summary>
    /// <returns></returns>
    public List<DSInst> GetInsts() {
        return EnclosedElements.Where(e => e.Kind == DSElementKind.Inst)
            .Cast<DSInst>()
            .ToList();
    }
#nullable disable

    #region props

    public override DSElementKind Kind => DSElementKind.File;
    public string FileName => fileName;
    public LinkedDictionary<string, string?> Imports => imports;
    public HashSet<string> ResolvedImports => resolvedImports;

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", fileName=").Append(fileName);
    }
}
}