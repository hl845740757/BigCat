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

using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 常量生成器配置，适用枚举
/// 
/// 1.直接通过反序列化解析
/// 2.对于常量表，只需配置类名和表单名
/// </summary>
[DsonSerializable]
public class ConstCfg
{
    public readonly string clsName;
    public readonly string sheetName;
    public readonly string nameCol;
    public readonly string valueCol;
    public readonly string commentCol;
    public readonly bool isFlags;

    /// <summary>
    /// 该方法由生成的代码调用
    /// </summary>
    /// <param name="reader"></param>
    public ConstCfg(IDsonObjectReader reader) {
        clsName = reader.ReadString("clsName");
        sheetName = reader.ReadString("sheetName");
        nameCol = reader.ReadString("nameCol") ?? "";
        valueCol = reader.ReadString("valueCol") ?? "";
        commentCol = reader.ReadString("commentCol") ?? "";
        isFlags = reader.ReadBool("isFlags");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clsName">生成的枚举名</param>
    /// <param name="sheetName">表单名-顶级表单名</param>
    /// <param name="nameCol">枚举名列</param>
    /// <param name="valueCol">枚举值列</param>
    /// <param name="commentCol">注释列</param>
    /// <param name="isFlags">是否是flags类型</param>
    public ConstCfg(string clsName,
                    string sheetName, string nameCol, string valueCol, string commentCol,
                    bool isFlags = false) {
        this.clsName = clsName;
        this.sheetName = sheetName;
        this.nameCol = nameCol;
        this.valueCol = valueCol;
        this.commentCol = commentCol;
        this.isFlags = isFlags;
    }

    /// <summary>
    /// 用于为参数表生成常量
    /// </summary>
    /// <param name="clsName">生成的枚举名</param>
    /// <param name="sheetName">表单名-顶级表单名</param>
    public ConstCfg(string clsName, string sheetName) {
        this.clsName = clsName;
        this.sheetName = sheetName;
        nameCol = "";
        valueCol = "";
        commentCol = "";
        isFlags = false;
    }
}
}