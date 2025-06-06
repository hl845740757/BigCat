#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.Excel
{
/// <summary>
/// 表单仓库
/// </summary>
public sealed class SheetRepository
{
    /// <summary>
    /// 所有的表单
    /// 表单名是不可以重复的
    /// </summary>
    private readonly LinkedDictionary<string, Sheet> _sheetMap = new();

    public SheetRepository() {
    }

    /// <summary>
    /// 特殊逻辑时可直接操作
    /// </summary>
    public LinkedDictionary<string, Sheet> SheetMap => _sheetMap;

    /// <summary>
    /// 获取排序后的表单
    /// </summary>
    /// <returns></returns>
    public List<Sheet> GetSortedSheets() {
        List<Sheet> sheets = new List<Sheet>(_sheetMap.Values);
        sheets.Sort((a, b) => string.Compare(a.sheetName, b.sheetName, StringComparison.Ordinal));
        return sheets;
    }

    /// <summary>
    /// 添加一个表单
    /// </summary>
    /// <param name="sheet"></param>
    /// <exception cref="ArgumentException"></exception>
    public void AddSheet(Sheet sheet) {
        if (!_sheetMap.TryAdd(sheet.sheetName, sheet)) {
            throw new ArgumentException($"sheet {sheet.sheetName} already exists");
        }
    }

    /// <summary>
    /// 获取表单
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public Sheet? GetSheet(string sheetName) {
        _sheetMap.TryGetValue(sheetName, out Sheet sheet);
        return sheet;
    }

    /// <summary>
    /// 删除表单
    /// </summary>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public Sheet? RemoveSheet(string sheetName) {
        _sheetMap.Remove(sheetName, out Sheet sheet);
        return sheet;
    }
}
}