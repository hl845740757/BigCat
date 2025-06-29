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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// DS脚本生成器配置
/// </summary>
[DsonSerializable]
public class DataScriptGeneratorCfg
{
    /// <summary>
    /// 生成的ds文件的输出路径
    ///
    /// 如果以'.ds'结尾，表示生成为单文件，否则表格每个表格一个ds文件。
    /// </summary>
    public string outPath;
    /// <summary>
    /// 模板文件路径
    /// </summary>
    public string templateFile;
    /// <summary>
    /// 每个类的详细配置
    /// </summary>
    /// <returns></returns>
    public List<ClassCfg> items = new();

    /// <summary>
    /// 
    /// </summary>
    [DsonSerializable]
    public class ClassCfg
    {
        /// <summary>
        /// 类型名
        /// <code>Item => ItemCfg</code>
        /// </summary>
        public string name;
        /// <summary>
        /// 增加的额外字段
        /// </summary>
        public List<FieldCfg> extraFields = new List<FieldCfg>();
    }

    /// <summary>
    /// 扩展字段配置
    ///
    /// 1.扩展字段不支持readonly
    /// 2.扩展字段不支持序列化
    /// 3.扩展字段不参与equals和hashcode
    /// </summary>
    [DsonSerializable]
    public class FieldCfg
    {
        /// <summary>
        /// 字段类型
        /// </summary>
        public string type;
        /// <summary>
        /// 字段名
        /// </summary>
        public string name;
        /// <summary>
        /// 简单注释
        /// </summary>
        public string comment;
    }
}
}