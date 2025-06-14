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

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 单元格数据坐标
///
/// 注意：不要调整hashcode算法
/// </summary>
public readonly struct Location : IEquatable<Location>
{
    public readonly string sheetName; // 表
    public readonly string dataId; // 行
    public readonly string fieldName; // 列
    public readonly int index; // 数据下标

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sheetName">表单名</param>
    /// <param name="dataId">数据id，第一列的value</param>
    /// <param name="fieldName">原列名，字段名</param>
    /// <param name="index">数据下标</param>
    public Location(string sheetName, string dataId, string fieldName, int index = 0) {
        this.sheetName = sheetName;
        this.dataId = dataId;
        this.fieldName = fieldName;
        this.index = index;
    }

    /// <summary>
    /// 获取单元格的坐标
    /// </summary>
    public Location CellLocation => new Location(sheetName, dataId, fieldName, index: 0);

    public bool Equals(Location other) {
        return sheetName == other.sheetName && dataId == other.dataId && fieldName == other.fieldName && index == other.index;
    }

    public override bool Equals(object? obj) {
        return obj is Location other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = sheetName.GetHashCode();
        hashCode = (hashCode * 397) ^ dataId.GetHashCode();
        hashCode = (hashCode * 397) ^ fieldName.GetHashCode();
        hashCode = (hashCode * 397) ^ index;
        return hashCode;
    }

    public override string ToString() {
        return $"{nameof(sheetName)}: {sheetName}, {nameof(dataId)}: {dataId}, {nameof(fieldName)}: {fieldName}, {nameof(index)}: {index}";
    }
}
}