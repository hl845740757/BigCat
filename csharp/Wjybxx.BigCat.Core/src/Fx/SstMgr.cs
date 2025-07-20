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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.IO;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 共享字符串表管理器
/// （客户端用）
/// </summary>
public static class SstMgr
{
    /// <summary>
    /// 单个文本的缓存(正常也就几十个字符，多的时候200左右，200个中文最多600字节)
    /// </summary>
    private const int BUFFER_LENGTH = 2048;
    /// <summary>
    /// 字段id到字符串的映射
    /// (注意：不是共享字符串的id到字符串的索引)
    /// (非并发集合是安全的，因为运行期间不会增删，不会导致结构性变化)
    /// </summary>
    private static readonly LinkedDictionary<int, Item> locationId2ItemMap = new();

    /// <summary>
    /// 获取字段关联的文本
    /// </summary>
    /// <param name="locationId">数据坐标</param>
    /// <returns></returns>
    [StableName]
    public static string GetString(int locationId) {
        if (locationId == 0) return string.Empty;
        if (!locationId2ItemMap.TryGetValue(locationId, out Item item)) {
            return locationId.ToString();
        }
        if (item.IsLoaded) {
            return item.Value;
        }
        // 多线程访问Steam时需要加锁，buffer也需要避免共享
        // 多线程时这里小概率会重复加载，不处理
        string value;
        Stream stream = item.Stream;
        lock (stream) {
            byte[] buffer = IArrayPool<byte>.Shared.Acquire(BUFFER_LENGTH);
            try {
                stream.Seek(item.offset, SeekOrigin.Begin);
                _ = stream.Read(buffer, 0, 4 + 1 + 2);
                int len = ByteBufferUtil.GetInt16LE(buffer, 4 + 1);
                _ = stream.Read(buffer, 0, len);
                value = Encoding.UTF8.GetString(buffer, 0, len);
            }
            finally {
                IArrayPool<byte>.Shared.Release(buffer);
            }
        }
        item = item.WithValue(value);
        locationId2ItemMap[locationId] = item;

        // 需要拷贝到其它Item -- 已提前相邻
        int key = locationId;
        while (locationId2ItemMap.PrevKey(key, out int prevKey, out Item prevItem)) {
            if (prevItem.ssti != item.ssti) {
                break;
            }
            locationId2ItemMap[prevKey] = item;
            key = prevKey;
        }
        key = locationId;
        while (locationId2ItemMap.NextKey(key, out int nextKey, out Item nextItem)) {
            if (nextItem.ssti != item.ssti) {
                break;
            }
            locationId2ItemMap[nextKey] = item;
            key = nextKey;
        }
        return value;
    }

    /// <summary>
    /// 该接口主要用于简化生成的代码
    /// </summary>
    /// <param name="idList"></param>
    /// <returns></returns>
    [StableName]
    public static ImmutableList<string> GetStringList(IList<int> idList) {
        if (idList.Count == 0) return ImmutableList<string>.Empty;
        string[] array = new string[idList.Count];
        for (int i = 0; i < idList.Count; i++) {
            int locationId = idList[i];
            array[i] = GetString(locationId);
        }
        return ImmutableList<string>.CreateRange(array);
    }

    /// <summary>
    /// 直接加载所有字符串
    /// (正常不应该使用，主要用于测试内存压力；只应该在无其它线程访问的时候调用)
    /// </summary>
    public static void LoadAll() {
        foreach (var pair in locationId2ItemMap) {
            Item item = pair.Value;
            if (item.IsLoaded) continue;
            GetString(pair.Key);
        }
    }

    /// <summary>
    /// 初始化共享字符串表，必须在游戏初始化流程时调用
    /// 由于版本更新机制，SST文件可能不在同一个物理目录，所以由用户收集所有文件后传入该方法。
    /// </summary>e
    /// <param name="files">sst目录下的文件</param>
    public static void Init(List<string> files) {
        // 读取sst.db文件
        Dictionary<int, Item> sstStringMap = new Dictionary<int, Item>(1000);
        foreach (string file in files) {
            if (file.EndsWith(".index")) continue;
            ReadSstMetaInfo(sstStringMap, file);
        }
        // 读取索引文件 -- 对索引文件进行排序，让相同ssti的字段集中在一起
        string indexFile = files.First(e => e.EndsWith(".index"));
        KeyValuePair<int, int>[] sortedIndexMap = ReadIndexMap(indexFile).ToArray();
        Array.Sort(sortedIndexMap, (a, b) => {
            int r = a.Value.CompareTo(b.Value);
            return r != 0 ? r : a.Key.CompareTo(b.Key);
        });
        // 
        locationId2ItemMap.Clear();
        locationId2ItemMap.EnsureCapacity(sortedIndexMap.Length);
        foreach (var pair in sortedIndexMap) {
            int locationId = pair.Key;
            int ssti = pair.Value;
            if (sstStringMap.TryGetValue(ssti, out Item item)) {
                locationId2ItemMap.Add(locationId, item);
            }
        }
    }

    private static Dictionary<int, int> ReadIndexMap(string filePath) {
        Dictionary<int, int> indexMap = new Dictionary<int, int>();
        byte[] buffer = new byte[8];
        using (FileStream fileStream = File.OpenRead(filePath)) {
            while (fileStream.Position < fileStream.Length) {
                _ = fileStream.Read(buffer, 0, buffer.Length);
                int locationId = ByteBufferUtil.GetInt32LE(buffer, 0);
                int ssti = ByteBufferUtil.GetInt32LE(buffer, 4);
                indexMap.Add(locationId, ssti);
            }
        }
        return indexMap;
    }

    private static void ReadSstMetaInfo(Dictionary<int, Item> sstStringMap, string filePath) {
        FileStream fileStream = File.OpenRead(filePath);
        byte[] buffer = new byte[BUFFER_LENGTH];
        while (fileStream.Position < fileStream.Length) {
            int offset = (int)fileStream.Position;
            // [id, preload, len, data]
            _ = fileStream.Read(buffer, 0, 4 + 1 + 2);
            int ssti = ByteBufferUtil.GetInt32LE(buffer, 0);
            bool preload = ByteBufferUtil.GetByte(buffer, 4) == 1;
            int len = ByteBufferUtil.GetInt16LE(buffer, 4 + 1);
            if (len > buffer.Length) {
                throw new AssertionError();
            }
            object streamOrValue;
            if (preload) {
                _ = fileStream.Read(buffer, 0, len);
                streamOrValue = Encoding.UTF8.GetString(buffer, 0, len);
                offset = -1;
            } else {
                fileStream.Seek(len, SeekOrigin.Current);
                streamOrValue = fileStream;
            }
            sstStringMap.Add(ssti, new Item(ssti, offset, streamOrValue));
        }
    }

    /// <summary>
    /// 注意：由于是Struct，加载后需要覆盖所有ssti相同的Item
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct Item
    {
#nullable disable
        /// <summary>
        /// 字符串索引
        /// </summary>
        [FieldOffset(0)]
        public readonly int ssti;
        /// <summary>
        /// 文件中的偏移 -- len + data；
        ///
        /// -1表示已加载，value为string
        /// 非负表示尚未加载，value为stream
        /// </summary>
        [FieldOffset(4)]
        public readonly int offset;
        /// <summary>
        /// 关联的stream或最终字符串值
        /// </summary>
        [FieldOffset(8)]
        public readonly object streamOrValue;

        public Item(int ssti, int offset, object streamOrValue) {
            this.ssti = ssti;
            this.offset = offset;
            this.streamOrValue = streamOrValue;
        }

        public Item WithValue(string value) {
            return new Item(ssti, -1, value);
        }

        public bool IsLoaded {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => offset < 0;
        }

        public string Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (string)streamOrValue;
        }
        public Stream Stream {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Stream)streamOrValue;
        }
    }
}
}