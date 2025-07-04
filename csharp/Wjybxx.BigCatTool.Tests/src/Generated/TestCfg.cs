using Wjybxx.Commons.Attributes;
using Wjybxx.BigCat.Util;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using Wjybxx.Commons.Collections;
using System;
using Wjybxx.BigCat.Fx;
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCat.Demo
{/// <summary>
/// @SheetInfo {name: "Test", type: 0}
/// </summary>
[Generated("Wjybxx.BigCatTool.Generator.Excel.ClassGenerator")]
[SerialVersion(502238137)]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "TestCfg" })]
public class TestCfg
{
    #nullable disable
    // ReSharper disable All
    /// <summary>
    /// 物品id
    /// </summary>
    private readonly int _itemId;
    /// <summary>
    /// 概率
    /// </summary>
    private double _rate;
    /// <summary>
    /// 概率-会被导出为科学计数法
    /// </summary>
    private double _rate2;
    /// <summary>
    /// 测试多态数据修正
    /// </summary>
    private object _v3orV4;
    /// <summary>
    /// @Options{ssti: true, nonSerialized: false}
    /// 测试字符串池化
    /// </summary>
    private ImmutableList<int> _list1 = ImmutableList<int>.Empty;
    [NonSerialized]
    private ImmutableList<string> _list1Cache;
    /// <summary>
    /// @Options{ssti: true, nonSerialized: false}
    /// 测试字符串池化
    /// </summary>
    private ImmutableList<int> _list2 = ImmutableList<int>.Empty;
    [NonSerialized]
    private ImmutableList<string> _list2Cache;

    public TestCfg(int itemId) {
        this._itemId = itemId;
    }

    public int itemId => _itemId;
    public double rate {
        get => _rate;
        internal set => this._rate = value;
    }

    public double rate2 {
        get => _rate2;
        internal set => this._rate2 = value;
    }

    public object v3orV4 {
        get => _v3orV4;
        internal set => this._v3orV4 = value;
    }

    public ImmutableList<string> list1 => _list1Cache ??= SstMgr.GetStringList(_list1);
    public ImmutableList<string> list2 => _list2Cache ??= SstMgr.GetStringList(_list2);
    #region codec

    public TestCfg(IDsonObjectReader reader) {
        this._itemId = reader.ReadInt("itemId");
        if (reader.ReadName("rate")) this._rate = reader.ReadDouble(null);
        if (reader.ReadName("rate2")) this._rate2 = reader.ReadDouble(null);
        if (reader.ReadName("v3orV4")) this._v3orV4 = reader.ReadObject<object>(null);
        if (reader.ReadName("list1")) this._list1 = reader.ReadObject<ImmutableList<int>>(null);
        if (reader.ReadName("list2")) this._list2 = reader.ReadObject<ImmutableList<int>>(null);
    }

    public virtual void WriteObject(IDsonObjectWriter writer) {
        writer.WriteInt("itemId", this._itemId);
        writer.WriteDouble("rate", this._rate);
        writer.WriteDouble("rate2", this._rate2);
        writer.WriteObject("v3orV4", this._v3orV4);
        writer.WriteObject("list1", this._list1);
        writer.WriteObject("list2", this._list2);
    }

    public virtual void BeforeEncode(ConverterOptions options) {
    }

    public virtual void AfterDecode(ConverterOptions options) {
    }

    #endregion

    #region copy

    public virtual void CopyFrom(TestCfg src) {
        this._rate = src._rate;
        this._rate2 = src._rate2;
        this._v3orV4 = src._v3orV4;
        this._list1 = src._list1;
        this._list1Cache = src._list1Cache;
        this._list2 = src._list2;
        this._list2Cache = src._list2Cache;
    }

    #endregion

    #region ToString

    public override string ToString() {
        return "TestCfg{itemId: " + itemId + "}";
    }

    #endregion
}
}
