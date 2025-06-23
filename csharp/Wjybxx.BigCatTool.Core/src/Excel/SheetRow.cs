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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatTool.Excel
{
/// <summary>
/// 表格行
/// </summary>
public sealed class SheetRow : IValueProvider
{
    /// <summary>
    /// 行索引
    /// </summary>
    private int rowIndex;
    /// <summary>
    /// 该行的所有值 -- 可以只包含有效单元格，但建议包含所有表头覆盖的单元格
    /// </summary>
    private readonly LinkedDictionary<string, string?> name2ValueMap = new();

    public SheetRow(int rowIndex) {
        this.rowIndex = rowIndex;
    }

    public SheetRow(int rowIndex, IDictionary<string, string?> name2ValueMap) {
        this.rowIndex = rowIndex;
        this.name2ValueMap.PutAll(name2ValueMap);
    }

    /// <summary>
    /// 从1开始的行号
    /// </summary>
    public int LineNumber => rowIndex + 1;

    /// <summary>
    /// 获取单元格的值
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetValue(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        name2ValueMap.TryGetValue(name, out string value);
        return value;
    }

    /// <summary>
    /// 设置单元格的值
    /// (如果value为null则表示删除元素)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(string name, string? value) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (value == null) {
            name2ValueMap.Remove(name);
        } else {
            name2ValueMap[name] = value;
        }
    }

    /// <summary>
    /// 清空单元格
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() {
        name2ValueMap.Clear();
    }

    /// <summary>
    /// 是否为空行
    /// 只有所有的cell都是null时为true
    /// </summary>
    public bool IsEmpty => name2ValueMap.IsEmpty;

    /// <summary>
    /// 行索引
    /// </summary>
    public int RowIndex {
        get => rowIndex;
        set => rowIndex = value;
    }

    /// <summary>
    /// 获取内部字典
    /// </summary>
    public LinkedDictionary<string, string> Name2ValueMap => name2ValueMap;

    public override string ToString() {
        return $"{nameof(LineNumber)}: {LineNumber}, {nameof(Name2ValueMap)}: {CollectionUtil.ToString(Name2ValueMap)}";
    }
}
}