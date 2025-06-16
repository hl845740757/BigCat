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

using System.Collections.Generic;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
[DsonSerializable]
public class ConstGeneratorCfg
{
#nullable disable
    /// <summary>
    /// 生成代码的文件夹
    /// </summary>
    public string outPath;
    /// <summary>
    /// 生成代码的命名空间
    /// </summary>
    public string ns;
    /// <summary>
    /// 所有的配置
    /// </summary>
    public List<ConstCfg> items = new List<ConstCfg>();

    /// <summary>
    /// 常量生成器配置，适用枚举
    /// 
    /// 1.字段名建议驼峰或蛇形，不建议全大写。
    /// 2.为参数表生成常量时，只需配置类名和表单名。
    /// </summary>
    [DsonSerializable]
    public class ConstCfg
    {
        public string clsName;
        public string sheetName;
        public string nameCol;
        public string valueCol;
        public string commentCol;

        /// <summary>
        /// 生成的代码调用
        /// </summary>
        public ConstCfg() {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clsName">生成的枚举名</param>
        /// <param name="sheetName">表单名-顶级表单名</param>
        /// <param name="nameCol">枚举名列</param>
        /// <param name="valueCol">枚举值列</param>
        /// <param name="commentCol">注释列</param>
        public ConstCfg(string clsName,
                        string sheetName, string nameCol, string valueCol, string commentCol) {
            this.clsName = clsName;
            this.sheetName = sheetName;
            this.nameCol = nameCol;
            this.valueCol = valueCol;
            this.commentCol = commentCol;
        }

        /// <summary>
        /// 用于为参数表生成常量
        /// </summary>
        /// <param name="clsName">生成的枚举名</param>
        /// <param name="sheetName">表单名-顶级表单名</param>
        public ConstCfg(string clsName, string sheetName) {
            this.clsName = clsName;
            this.sheetName = sheetName;
            this.nameCol = "";
            this.valueCol = "";
            this.commentCol = "";
        }
    }
}
}