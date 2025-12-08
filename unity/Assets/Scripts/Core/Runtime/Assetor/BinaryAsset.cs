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
using System.IO;
using System.Text;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 二进制文件资产
///
/// 注：不可修改返回的字节数组，否则可能导致异常。
/// </summary>
public sealed class BinaryAsset
{
    public readonly string assetPath;
    public readonly byte[] bytes;
    private string _fileName;
    private string _text;

    public BinaryAsset(string assetPath, byte[] bytes) {
        this.assetPath = assetPath;
        this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    /// <summary>
    /// 数据长度
    /// </summary>
    public int dataLength => bytes.Length;

    /// <summary>
    /// 文件名（含扩展名）
    /// </summary>
    public string fileName => _fileName ??= Path.GetFileName(assetPath);

    /// <summary>
    /// 文本内容
    ///
    /// 注：默认为UTF8格式；如果不期望UTF8格式解码，可以在首次访问前手动初始化。
    /// </summary>
    public string text {
        get => _text ??= Encoding.UTF8.GetString(bytes);
        set {
            if (_text != null) {
                throw new InvalidOperationException("text initialized");
            }
            _text = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
}