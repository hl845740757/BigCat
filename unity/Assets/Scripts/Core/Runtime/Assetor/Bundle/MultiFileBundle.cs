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
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.IO;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 简单多文件捆绑包
///
///<![CDATA[
///     len(assetPath) + utf8(assetPath) + len(data) + data
///       2 Byte             N Bytes        4 Byte   + N Bytes
/// ]]>
/// 注：
/// 1.简单拼接文件的二进制构成，通过len/name/len/data的方式切割，len字段使用小端编码。
/// 2.Bundle销毁时会自动关闭输入流 —— bundle不应该和其它对象共享输入流。
/// </summary>
public class MultiFileBundle : IAssetBundle
{
    private readonly AssetBundleInfo _bundleInfo;
    private readonly Stream _stream;
    private readonly List<FileItem> _fileItemList = new List<FileItem>();
    private readonly LinkedDictionary<string, FileItem> _fileItemDic = new();
    private Action<MultiFileBundle> _unloadCallback;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bundleInfo"></param>
    /// <param name="stream">文件输入流</param>
    /// <param name="sharedBuffer">解析缓冲区</param>
    public MultiFileBundle(AssetBundleInfo bundleInfo, Stream stream, byte[] sharedBuffer = null) {
        _bundleInfo = bundleInfo;
        _stream = stream;
        _fileItemList.EnsureCapacity(bundleInfo.mainAssets.Count);
        _fileItemDic.EnsureCapacity(bundleInfo.mainAssets.Count * 2);
        //
        byte[] buffer = sharedBuffer ?? new byte[256];
        while (stream.Position < stream.Length) {
            _ = stream.Read(buffer, 0, 2);
            int pathLen = ByteBufferUtil.GetInt16LE(buffer, 0);
            _ = stream.Read(buffer, 0, pathLen);
            string assetPath = Encoding.UTF8.GetString(buffer, 0, pathLen);
            int compression = stream.ReadByte();
            //
            _ = stream.Read(buffer, 0, 4);
            int dataLen = ByteBufferUtil.GetInt32LE(buffer, 0);
            int offset = (int)stream.Position;
            //
            FileItem item = new FileItem(assetPath, compression, offset, dataLen, this);
            _fileItemList.Add(item);
            _fileItemDic.Add(item.assetPath, item); // 全路径索引，禁止重复
            _fileItemDic[item.fileName] = item; // 文件名索引，允许重复
            //
            stream.Seek(dataLen, SeekOrigin.Current);
        }
        Debug.Assert(stream.Position == stream.Length);
    }

    public AssetBundleInfo BundleInfo => _bundleInfo;
    public Stream Stream => _stream;
    /// <summary>
    /// 卸载回调(用于解除对BundleManager的依赖)
    /// </summary>
    public Action<MultiFileBundle> UnloadCallback {
        get => _unloadCallback;
        set => _unloadCallback = value;
    }

    public void UnloadBundle(bool unloadAllLoadedObjects) {
        _fileItemList.Clear();
        _fileItemDic.Clear();
        _stream.Dispose();
        _unloadCallback?.Invoke(this);
    }

    public ResourceTask LoadAssetAsync(string assetPath, Type assetType) {
        return null;
    }

    public ResourceTask LoadAssetWithSubAssetsAsync(string assetPath, Type assetType) {
        return null;
    }

    public ResourceTask LoadAllAssetsAsync(Type assetType) {
        return null;
    }

    public BinaryAsset LoadBinaryAsset(string assetPath) {
        _fileItemDic.TryGetValue(assetPath, out FileItem item);
        return item;
    }

    public IReadOnlyList<BinaryAsset> LoadAllBinaryAssets() {
        return _fileItemList;
    }

    private class FileItem : BinaryAsset
    {
        private readonly MultiFileBundle bundle;
        private readonly int _compression; // 压缩方式
        private readonly int _offset; // 数据部分偏移
        private readonly int _length; // 数据长度

        public FileItem(string assetPath, int compression,
                        int offset, int length,
                        MultiFileBundle bundle)
            : base(assetPath) {
            this.bundle = bundle;
            this._compression = compression;
            this._offset = offset;
            this._length = length;
        }

        public override int compression => _compression;
        public override int dataLength => _length;

        public override void GetData(byte[] buffer, int offset) {
            ByteBufferUtil.CheckBuffer(buffer, offset, _length);
            bundle.Stream.Position = this._offset;
            int cnt = bundle.Stream.Read(buffer, offset, _length);
            Debug.Assert(cnt == _length);
        }
    }
}
}