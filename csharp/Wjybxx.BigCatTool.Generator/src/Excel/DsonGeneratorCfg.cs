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

using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCatTool.Generator.Excel
{
[DsonSerializable]
public class DsonGeneratorCfg
{
#nullable disable
    /// <summary>
    /// dson文件的输出目录
    /// </summary>
    public string outPath;
    /// <summary>
    /// 是否生成dson文本文件
    /// </summary>
    public bool enableText = false;
    /// <summary>
    /// 是否生成dson二进制文件
    /// </summary>
    public bool enableBinary = true;
    /// <summary>
    /// dson二进制文件的后缀
    /// </summary>
    public string fileExtension = ".dson2";
    /// <summary>
    /// 生成二进制文件时的buffer大小
    /// </summary>
    public int bufferLen = 1024 * 1024;
}
}