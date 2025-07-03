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

namespace Wjybxx.BigCatTool.Generator.Excel
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
        /// <summary>
        /// 生成的class类名
        /// </summary>
        public string clsName;
        /// <summary>
        /// 表单名
        /// </summary>
        public string sheetName;
        /// <summary>
        /// 常量名所在的列
        /// </summary>
        public string nameCol;
        /// <summary>
        /// 常量值所在的列
        /// </summary>
        public string valueCol;
        /// <summary>
        /// 注释所在的列
        /// </summary>
        public string commentCol;
    }
}
}