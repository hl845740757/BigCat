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
using System.Buffers;
using System.IO;
using System.Text;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 二进制资产的接收对象
///
/// 注：用于将二进制资产<see cref="BinaryAsset"/>转换为Unity对象。
/// </summary>
public interface IBinaryAssetReceiver
{
    void Unpack(BinaryAsset binAsset);
}

/// <summary>
/// 二进制文件资产
///
/// 注：不可修改返回的字节数组，否则可能导致异常。
/// </summary>
public abstract class BinaryAsset
{
    private readonly string _assetPath;
    private byte[] _bytes;
    private string _fileName;
    private string _text;

    protected BinaryAsset(string assetPath) {
        this._assetPath = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
    }

    /// <summary>
    /// 资产路径
    /// </summary>
    public string assetPath => _assetPath;

    /// <summary>
    /// 文件名（含扩展名）
    /// </summary>
    public string fileName => _fileName ??= Path.GetFileName(_assetPath);

    /// <summary>
    /// 数据压缩方式
    /// 注：0表示未压缩，可以用于表示文件自身的压缩状态，也可以用于表示子资源的压缩状态。
    /// </summary>
    public abstract int compression { get; }

    /// <summary>
    /// 数据长度
    /// </summary>
    public abstract int dataLength { get; }

    /// <summary>
    /// 将数据拷贝到指定Buffer
    ///
    /// 注：用于避免额外的bytes缓存，比如读表期间。
    /// </summary>
    /// <param name="buffer">接收结果的buffer</param>
    /// <param name="offset">buffer偏移</param>
    public abstract void GetData(byte[] buffer, int offset);

    /// <summary>
    /// 关联的字节数据
    /// </summary>
    public virtual byte[] bytes {
        get {
            if (_bytes == null) {
                _bytes = new byte[dataLength];
                GetData(_bytes, 0);
            }
            return _bytes;
        }
        protected set => _bytes = value;
    }

    /// <summary>
    /// 文本内容
    /// </summary>
    public virtual string text {
        get {
            if (_text != null) return _text;
            if (_bytes == null && dataLength >= 4096) {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(dataLength);
                GetData(buffer, 0);
                _text = Encoding.UTF8.GetString(buffer, 0, dataLength);
                ArrayPool<byte>.Shared.Return(buffer);
            } else {
                _text = Encoding.UTF8.GetString(bytes);
            }
            return _text;
        }
        protected set => _text = value;
    }
}
}