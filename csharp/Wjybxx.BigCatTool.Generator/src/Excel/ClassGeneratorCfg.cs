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
/// <summary>
///
/// </summary>
[DsonSerializable]
public class ClassGeneratorCfg
{
#nullable disable
    /// <summary>
    /// 生成代码的文件夹
    /// </summary>
    public string outPath;
    /// <summary>
    /// 所有的解码代理类的命名空间
    /// </summary>
    public string codecProxyNs;
    /// <summary>
    /// 所有的配置
    /// </summary>
    public List<ClassCfg> items = new List<ClassCfg>();

    /// <summary>
    /// 单个类型的配置
    /// </summary>
    [DsonSerializable]
    public class ClassCfg
    {
        /// <summary>
        /// 类型名
        /// </summary>
        public string name;
        /// <summary>
        /// 该类型关联的编解码代理类的全限定名
        /// 
        /// 示例：<code>ItemCodecProxy</code>
        /// </summary>
        public string codecProxy;
        /// <summary>
        /// 解码代理
        /// key为字段名，value为Proxy类中的方法名；AfterDecode为解码后的钩子
        /// 
        /// <code>type: ReadType</code>
        /// <code>AfterDecode: AfterDecode</code>
        /// </summary>
        public Dictionary<string, string> fieldProxies = new Dictionary<string, string>();
        /// <summary>
        /// 扩展字段
        ///
        /// 扩展字段都是缓存字段，用户需要在<code>AfterDecode</code>钩子方法中维护它们。
        /// (因此这些字段都是包含pubic-setter的)
        /// </summary>
        public List<FieldCfg> extensionFields = new List<FieldCfg>();
    }

    /// <summary>
    /// 扩展字段
    /// </summary>
    [DsonSerializable]
    public class FieldCfg
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string name;
        /// <summary>
        /// 字段类型 -- 必须是Class可以访问到的类型符号
        /// </summary>
        public string type;
        /// <summary>
        /// 字段注释
        /// </summary>
        public string comment;
    }
}
}