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
using System.Linq;
using System.Text;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// PB文件
/// </summary>
public class PBFile : PBElement
{
#nullable disable
    /** 文件名 */
    private string fileName;
    /** 语法级别 */
    private string syntax = "proto3";
    /** 导入的文件 -- value可以为null，表示未声明修饰符 */
    private readonly LinkedDictionary<string, string?> imports = new(4);
    /** 解析后的所有依赖 -- 包括传递而来的依赖，无法保证解析顺序 */
    private readonly HashSet<string> resolvedImports = new(4);
#nullable enable

    public override PBElementKind Kind => PBElementKind.File;

    /// <summary>`
    /// 添加一个文件引用
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="modifier">修饰符</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public PBFile AddImport(string fileName, string? modifier) {
        if (!fileName.EndsWith(".proto")) {
            throw new ArgumentException(); // protobuf语法规范
        }
        this.imports.Add(fileName, modifier);
        this.resolvedImports.Add(fileName);
        return this;
    }

    /// <summary>
    /// 获取所有的顶层服务
    /// </summary>
    /// <returns></returns>
    public List<PBService> GetServices() {
        return EnclosedElements.Where(e => e.Kind == PBElementKind.Service)
            .Cast<PBService>()
            .ToList();
    }

    /// <summary>
    /// 获取所有的顶层消息
    /// </summary>
    /// <returns></returns>
    public List<PBMessage> GetMessages() {
        return EnclosedElements.Where(e => e.Kind == PBElementKind.Message)
            .Cast<PBMessage>()
            .ToList();
    }

    /// <summary>
    /// 获取所有的顶层枚举
    /// </summary>
    /// <returns></returns>
    public List<PBEnum> GetEnums() {
        return EnclosedElements.Where(e => e.Kind == PBElementKind.Enum)
            .Cast<PBEnum>()
            .ToList();
    }

    #region Props

    public string FileName {
        get => fileName;
        set => fileName = value ?? throw new ArgumentNullException(nameof(value));
    }
    public string? Syntax {
        get => syntax;
        set => syntax = value;
    }

    public LinkedDictionary<string, string?> Imports => imports;
    public HashSet<string> ResolvedImports => resolvedImports;

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", fileName=").Append(fileName);
    }
}
}