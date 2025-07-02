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

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 代码生成器配置
/// </summary>
[DsonSerializable]
public class CodeGeneratorCfg
{
#nullable disable
    /// <summary>
    /// 生成代码的文件夹
    /// </summary>
    public string outPath;
    /// <summary>
    /// 是否将同一个ds文件中的类型生成到同一个cs文件
    /// (如果文件内全是数据类，大量的equals和hashcode方法可能导致文件过大)
    /// </summary>
    public bool combine;
    /// <summary>
    /// 服务需要实现的接口，我们的业务可能需要Rpc服务实现一些公共接口
    /// (类型的全限定名)
    /// </summary>
    public List<string> serviceBaseTypes = new List<string>();

    /// <summary>
    /// 所有的解码代理类的命名空间
    /// </summary>
    public string codecProxyNs;
    /// <summary>
    /// 所有的配置
    /// (这个数据类通常很小，暂不建立缓存)
    /// </summary>
    public List<ClassCodecCfg> codecCfgs = new List<ClassCodecCfg>();

    /// <summary>
    /// 单个类型的配置
    /// </summary>
    [DsonSerializable]
    public class ClassCodecCfg
    {
        /// <summary>
        /// 类型名
        ///
        /// 如果是内部类，需要使用A.B.C格式声明
        /// </summary>
        public string name;
        /// <summary>
        /// 该类型关联的编解码代理类
        /// 
        /// 注意：如果原始类型是泛型类，代理类也需要是泛型类。
        /// 示例：<code>ItemCodecProxy</code>
        /// </summary>
        public string proxy;
        /// <summary>
        /// 字段读写代理
        ///
        /// (这里数据量也很少，暂不建立缓存)
        /// <code></code>
        /// </summary>
        public List<FieldCodecCfg> fieldProxies = new();
        /// <summary>
        /// 对象解码钩子 -- 详细可阅读<see cref="DsonSerializableAttribute"/>
        ///
        /// 目前支持：
        /// 1. BeforeEncode 解码前的钩子
        /// 2. AfterDecode 解码后的钩子
        /// </summary>
        public Dictionary<string, string> hooks = new();
    }

    [DsonSerializable]
    public class FieldCodecCfg
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string name;
        /// <summary>
        /// 读代理
        /// </summary>
        public string? readProxy;
        /// <summary>
        /// 写代理
        /// </summary>
        public string? writeProxy;
    }
}
}