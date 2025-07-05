#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.IO;
using Wjybxx.Dson;
using static Wjybxx.BigCatTool.Generator.Excel.ExcelConstants;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 共享字符串表生成器
/// (Shared String Table Generator)
///
/// 相关属性：
/// <see cref="ExcelConstants.KEY_I18N"/>
/// <see cref="ExcelConstants.KEY_INTERN"/>
/// 
/// 生成内容：
/// 1.根据字段坐标，分配稳定的唯一id
/// 2.将所有需要管理的字符串收集起来，相同字符串指向同一个字符串，并分配稳定的唯一id
/// 3.生成字段id=>字符串id的索引
/// 
/// 使用说明：
/// 1.location数据库文件默认永不清理，因为文件增长速率不大，特别情况时可手动清理。
/// 2.默认情况下不自动清理sst数据库文件中不被使用的数据，只有rebuild模式下才会清理。
/// 3.Rebuild会导致所有分区文件产生变化，导致用户需要下载所有分区 -- rebuild不保留原始id，因为没有意义，变化1个和所有都变化结果是一样的。
/// 4.调整分区数时必须使用rebuild模式。
/// 5.该脚本需要在<see cref="DataScriptGenerator"/>和<see cref="DsonGenerator"/>之前执行。
/// </summary>
public class SstGenerator : ISheetProcessor
{
    /// <summary>
    /// 单元格数据库，存储的是单元格的逻辑id
    ///
    /// 1.二进制，该文件仅编辑器使用，运行时不使用
    /// 2.数据可能包含冗余，不能基于此认为表格数据需要池化
    /// </summary>
    public const string FILE_LOCATION_DB = "location.db";
    /// <summary>
    /// Location到SST的索引文件
    ///
    /// 1.二进制，运行时需要
    /// 2.int => int
    /// </summary>
    public const string FILE_LSST_INDEX = "lsst.index";
    /// <summary>
    /// 共享字符串数据库，按分区存储
    ///
    /// 
    /// 文件名规则：<code>sst.db.0 sst.db.1 ...</code>
    /// 1.二进制，运行时需要
    /// 2.文件内字符串id递增
    /// </summary>
    public const string FILE_SST_DB = "sst.db";

    private readonly SheetRepository _repository;
    private readonly string _workDir;
    private readonly bool _rebuild;

    private readonly LinkedDictionary<Location, int> locationMap = new(1000);
    private readonly LinkedDictionary<int, int> indexMap = new(1000);
    private readonly List<Partition> partitions;
    private readonly StringBuilder _sb = new StringBuilder(64);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">要处理的表格</param>
    /// <param name="workDir">sst文件所在目录</param>
    /// <param name="rebuild">是否是重新构建sst数据库文件</param>
    /// <param name="partitionCount">sst文件分区数量</param>
    public SstGenerator(SheetRepository repository, string workDir, bool rebuild = false, int partitionCount = 10) {
        _repository = repository;
        _workDir = workDir;
        _rebuild = rebuild;

        partitions = new List<Partition>(partitionCount);
        for (int i = 0; i < partitionCount; i++) {
            partitions.Add(new Partition(i));
        }
    }

    public void Execute() {
        if (_rebuild) {
            File.Delete(_workDir + "/" + FILE_LSST_INDEX);
            string[] filePaths = Directory.GetFiles(_workDir, FILE_SST_DB + ".*", SearchOption.TopDirectoryOnly);
            foreach (string filePath in filePaths) {
                File.Delete(filePath);
            }
        }
        if (!Directory.Exists(_workDir)) {
            Directory.CreateDirectory(_workDir);
        }
        LoadLocation();
        LoadPartitions();
        LoadIndex();

        List<Header> headers = new List<Header>(10);
        foreach (Sheet sheet in _repository.SheetMap.Values) {
            headers.Clear();
            foreach (Header header in sheet.headers.Values) {
                if (IsListOrMapElement(header.name)) {
                    continue;
                }
                if (!IsInternField(header)) {
                    continue;
                }
                headers.Add(header);
            }
            if (sheet.IsParamSheet) {
                ProcessParamSheet(sheet, headers);
            } else {
                ProcessNormalSheet(sheet, headers);
            }
        }

        SaveLocation();
        SavePartitions();
        SaveIndex();
    }

    private void ProcessNormalSheet(Sheet sheet, List<Header> headers) {
        List<string> rawValues = new List<string>(10);
        List<int> locationIdList = new List<int>(10);
        Dictionary<string, List<Header>> elementHeaderMap = new(10);
        foreach (SheetRow sheetRow in sheet.valueRows) {
            if (sheetRow.Name2ValueMap.IsEmpty) { // 空白行
                continue;
            }
            string dataId = sheetRow.Name2ValueMap.PeekFirst().Value;
            if (string.IsNullOrWhiteSpace(dataId)) { // 注释行
                continue;
            }
            foreach (Header header in headers) {
                if (IsStringType(header.type)) {
                    int locationId = AddLocation(new Location(sheet.sheetName, dataId, header.name));
                    string rawValue = sheetRow.GetValue(header.name) ?? "";
                    int ssti = AddInternString(rawValue);

                    // 修正Value -- value是locationId
                    sheetRow.SetValue(header.name, locationId.ToString());
                    AddIndex(locationId, ssti);
                } else {
                    if (!elementHeaderMap.TryGetValue(header.name, out List<Header> elementHeaders)) {
                        elementHeaders = CollectElementHeaders(sheet, header);
                        elementHeaderMap.Add(header.name, elementHeaders);
                    }
                    rawValues.Clear();
                    locationIdList.Clear();
                    MergeCellValue(sheetRow, header, elementHeaders, rawValues);
                    // 更新LocationDB -- 全部挂在当前单元格下
                    for (int index = 0; index < rawValues.Count; index++) {
                        string rawValue = rawValues[index];
                        int locationId = AddLocation(new Location(sheet.sheetName, dataId, header.name, index));
                        int ssti = AddInternString(rawValue);

                        AddIndex(locationId, ssti);
                        locationIdList.Add(locationId);
                    }
                    // 修正Value -- value是locationId
                    sheetRow.SetValue(header.name, ToString(locationIdList));
                }
            }
        }
        UpdateHeaders(sheet, headers);
    }

    /// <summary>
    /// List类型需要先合并单元格，否则后续的生成器会出现问题
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="headers"></param>
    private void ProcessParamSheet(Sheet sheet, List<Header> headers) {
        List<Header> elementHeaders = new List<Header>(10);
        List<string> rawValues = new List<string>(10);
        List<int> locationIdList = new List<int>(10);
        foreach (Header header in headers) {
            if (IsStringType(header.type)) {
                int locationId = AddLocation(new Location(sheet.sheetName, header.name, header.name));
                string rawValue = sheet.GetValue(header.name) ?? "";
                int ssti = AddInternString(rawValue);

                // 修正Value -- value是locationId
                sheet.SetValue(header.name, locationId.ToString());
                AddIndex(locationId, ssti);
            } else {
                {
                    elementHeaders.Clear();
                    CollectElementHeaders(sheet, header, elementHeaders);
                }
                rawValues.Clear();
                locationIdList.Clear();
                MergeCellValue(sheet, header, elementHeaders, rawValues);
                for (int index = 0; index < rawValues.Count; index++) {
                    string rawValue = rawValues[index];
                    int locationId = AddLocation(new Location(sheet.sheetName, header.name, header.name, index));
                    int ssti = AddInternString(rawValue);

                    // 这里不修正原始value，方便debug查看
                    AddIndex(locationId, ssti);
                    locationIdList.Add(locationId);
                }
                // 修正Value -- value是locationId
                sheet.SetValue(header.name, ToString(locationIdList));
            }
        }
        UpdateHeaders(sheet, headers);
    }

    private void UpdateHeaders(Sheet sheet, List<Header> headers) {
        foreach (Header header in headers) {
            if (IsStringType(header.type)) {
                sheet.headers[header.name] = header.WithType(DSKeywords.TYPE_INT32);
            } else {
                sheet.headers[header.name] = header.WithType(TYPE_LIST_INT32);
                // 需要丢弃Element，否则会影响后续流程
                List<Header> elementHeaders = CollectElementHeaders(sheet, header);
                foreach (Header elementHeader in elementHeaders) {
                    sheet.headers.Remove(elementHeader.name);
                }
            }
        }
    }

    private string ToString(List<int> list) {
        StringBuilder sb = _sb.Clear();
        sb.Append('[');
        for (int index = 0; index < list.Count; index++) {
            if (index > 0) {
                sb.Append(',');
            }
            int locationId = list[index];
            sb.Append(locationId);
        }
        sb.Append(']');
        return sb.ToString();
    }

    private void MergeCellValue(IValueProvider valueProvider, Header header, List<Header> elementHeaders, List<string> result) {
        if (elementHeaders.Count == 0) {
            // 未拆为配置
            string rawValue = valueProvider.GetValue(header.name);
            if (string.IsNullOrWhiteSpace(rawValue)) {
                return;
            }
            DsonArray<string> dsonArray = Dsons.FromDson(rawValue).AsArray();
            foreach (DsonValue dsonValue in dsonArray) {
                result.Add(dsonValue.AsString());
            }
            return;
        }
        if (IsTuple(header)) {
            // 合并所有列
            foreach (Header elemHeader in elementHeaders) {
                string rawValue = valueProvider.GetValue(elemHeader.name) ?? "";
                result.Add(rawValue);
            }
        } else {
            // 遇见空白列中断
            foreach (Header elemHeader in elementHeaders) {
                string rawValue = valueProvider.GetValue(elemHeader.name) ?? "";
                if (string.IsNullOrEmpty(rawValue)) {
                    break;
                }
                result.Add(rawValue);
            }
        }
    }

    private static bool IsTuple(Header header) {
        if (!header.options.Contains(KEY_IS_TUPLE)) {
            return false;
        }
        DsonObject<string> options = ParseOptions(header.options);
        return GetBool(options, KEY_IS_TUPLE);
    }

    private static bool IsInternField(Header header) {
        if (!IsStringType(header.type) && !IsListStringType(header.type)) {
            return false;
        }
        if (!header.options.Contains(KEY_I18N) && !header.options.Contains(KEY_INTERN)) {
            return false;
        }
        DsonObject<string> options = ParseOptions(header.options);
        return GetBool(options, KEY_I18N) || GetBool(options, KEY_INTERN);
    }

    #region update

    /// <summary>
    /// 单个文本的缓存(正常也就几十个字符，多的时候200左右，200个中文最多600字节)
    /// </summary>
    private const int BUFFER_LENGTH = 2048;
    /// <summary>
    /// 字符串小于多少个字节时预加载
    /// </summary>
    private const int PRELOAD_THRESHOLD = 32;
    /// <summary>
    /// 每个分区的最大文本数量
    /// 每张表应该不至于超过10W个文本...
    /// </summary>
    private const int PARTITION_FACTOR = 10_0000;

    private static int MakeGuid(int partition, int value) {
        if (value >= PARTITION_FACTOR) {
            throw new ArgumentException("overflow: " + value);
        }
        return partition * PARTITION_FACTOR + value;
    }

    /// <summary>
    /// 我们调整了算法，DB里保存CellId，这样可以让同一个单元格多条数据的id相邻，可读性更好。
    /// 游戏配置中需要池化的List{string}单元格很少，且单个单元格元素很少，因此暂为为每个单元格预留10个Id（实际9）。
    /// (字符串数据长度很大的时候，通常需要额外的表来配置，否则单个单元格很臃肿)
    /// </summary>
    /// <param name="cellId"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private static int MakeLocationId(int cellId, int index) {
        return cellId * 10 + (index + 1);
    }

    private int AddLocation(Location location) {
        if (location.index > 8) {
            throw new IndexOutOfRangeException(location.index.ToString());
        }
        Location cellLocation = location.CellLocation;
        if (locationMap.TryGetValue(cellLocation, out int cellId)) {
            return MakeLocationId(cellId, location.index);
        }
        cellId = locationMap.Count + 1;
        locationMap.Add(cellLocation, cellId);
        return MakeLocationId(cellId, location.index);
    }

    private int AddInternString(string value) {
        int idx = string.IsNullOrEmpty(value) ? 0 : Math.Abs(value.GetHashCode()) % partitions.Count;
        Partition partition = partitions[idx];
        if (partition.itemMap.TryGetValue(value, out Item item)) {
            return item.ssti;
        }
        int ssti = MakeGuid(partition.id, partition.itemMap.Count + 1); // 避免尾部全0
        bool preload = string.IsNullOrEmpty(value) || Encoding.UTF8.GetByteCount(value) <= PRELOAD_THRESHOLD;
        item = new Item(ssti, preload, value);
        partition.itemMap.Add(value, item);
        return ssti;
    }

    private void AddIndex(int locationId, int ssti) {
        indexMap[locationId] = ssti;
    }

    #endregion

    #region load-save

    private void LoadLocation() {
        string filePath = _workDir + "/" + FILE_LOCATION_DB;
        if (!File.Exists(filePath)) {
            return;
        }
        // 直接使用系统库的String编码
        FileStream fileStream = File.OpenRead(filePath);
        using (var reader = new BinaryReader(fileStream, Encoding.UTF8)) {
            while (fileStream.Position < fileStream.Length) {
                string sheetName = reader.ReadString();
                string dataId = reader.ReadString();
                string colName = reader.ReadString();
                // int index = reader.ReadInt32();
                int cellId = reader.ReadInt32();

                Location location = new Location(sheetName, dataId, colName);
                locationMap.Add(location, cellId);
            }
        }
    }

    private void SaveLocation() {
        FileStream fileStream = File.OpenWrite(_workDir + "/" + FILE_LOCATION_DB);
        using (var writer = new BinaryWriter(fileStream, Encoding.UTF8)) {
            foreach (var pair in locationMap) {
                Location location = pair.Key;
                writer.Write(location.sheetName);
                writer.Write(location.dataId);
                writer.Write(location.fieldName);
                // writer.Write(location.index);
                writer.Write(pair.Value);
            }
        }
    }

    private void LoadIndex() {
        string filePath = _workDir + "/" + FILE_LSST_INDEX;
        if (!File.Exists(filePath)) {
            return;
        }
        byte[] buffer = new byte[8];
        using (FileStream fileStream = File.OpenRead(filePath)) {
            while (fileStream.Position < fileStream.Length) {
                _ = fileStream.Read(buffer, 0, buffer.Length);
                int locationId = ByteBufferUtil.GetInt32LE(buffer, 0);
                int ssti = ByteBufferUtil.GetInt32LE(buffer, 4);
                indexMap.Add(locationId, ssti);
            }
        }
    }

    private void SaveIndex() {
        FileStream fileStream = File.OpenWrite(_workDir + "/" + FILE_LSST_INDEX);
        byte[] buffer = new byte[8];
        using (fileStream) {
            foreach (var pair in indexMap) {
                ByteBufferUtil.SetInt32LE(buffer, 0, pair.Key);
                ByteBufferUtil.SetInt32LE(buffer, 4, pair.Value);
                fileStream.Write(buffer, 0, buffer.Length);
            }
        }
    }

    private void LoadPartitions() {
        byte[] buffer = new byte[BUFFER_LENGTH];
        foreach (Partition partition in partitions) {
            string filePath = _workDir + "/" + partition.fileName;
            if (!File.Exists(filePath)) {
                continue;
            }
            using (FileStream fileStream = File.OpenRead(filePath)) {
                while (fileStream.Position < fileStream.Length) {
                    // [ssti, preload, len, data]
                    _ = fileStream.Read(buffer, 0, 4 + 1 + 2);
                    int ssti = ByteBufferUtil.GetInt32LE(buffer, 0);
                    bool preload = ByteBufferUtil.GetByte(buffer, 4) == 1;
                    int len = ByteBufferUtil.GetInt16LE(buffer, 4 + 1);
                    if (len > buffer.Length) {
                        throw new AssertionError();
                    }
                    _ = fileStream.Read(buffer, 0, len);
                    string value = Encoding.UTF8.GetString(buffer, 0, len);
                    partition.itemMap[value] = new Item(ssti, preload, value);
                }
            }
        }
    }

    private void SavePartitions() {
        byte[] buffer = new byte[BUFFER_LENGTH];
        foreach (Partition partition in partitions) {
            string filePath = _workDir + "/" + partition.fileName;
            using (FileStream fileStream = File.OpenWrite(filePath)) {
                foreach (Item item in partition.itemMap.Values) {
                    // [ssti, preload, len, data]
                    ByteBufferUtil.SetInt32LE(buffer, 0, item.ssti);
                    ByteBufferUtil.SetByte(buffer, 4, item.preload ? (byte)1 : (byte)0);

                    int len = Encoding.UTF8.GetBytes(item.value, 0, item.value.Length, buffer, 7);
                    ByteBufferUtil.SetInt16LE(buffer, 5, (short)len);
                    fileStream.Write(buffer, 0, 7 + len);
                }
            }
        }
    }

    #endregion

    private readonly struct Partition
    {
        public readonly int id;
        public readonly string fileName;
        public readonly LinkedDictionary<string, Item> itemMap;

        public Partition(int id) {
            this.id = id;
            this.fileName = FILE_SST_DB + "." + id;
            this.itemMap = new(1000);
        }
    }

    private readonly struct Item
    {
        public readonly int ssti;
        public readonly bool preload;
        public readonly string value;

        public Item(int ssti, bool preload, string value) {
            this.ssti = ssti;
            this.preload = preload;
            this.value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
}