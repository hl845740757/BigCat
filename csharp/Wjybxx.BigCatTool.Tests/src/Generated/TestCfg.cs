using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.Demo
{
/// <summary>
/// @SheetInfo {name: "Test", type: 0}
/// </summary>
[Generated("Wjybxx.BigCatTool.Generator.Excel.ClassGenerator")]
[DsonSerializable(NameStyle = DsonNameStyle.CamelCaseNoPrefix)]
[SerializeVersion(877673664)]
public class TestCfg
{

    #nullable disable
    // ReSharper disable All
    /// <summary>
    /// 物品id
    /// </summary>
    private int _itemId;
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
    /// <summary>
    /// @Options{ssti: true, nonSerialized: false}
    /// 测试字符串池化
    /// </summary>
    private ImmutableList<int> _list2 = ImmutableList<int>.Empty;

    public TestCfg(int itemId) {
        this._itemId = itemId;
    }

    public TestCfg() {
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

    public ImmutableList<int> list1 {
        get => _list1;
        internal set => this._list1 = value;
    }

    public ImmutableList<int> list2 {
        get => _list2;
        internal set => this._list2 = value;
    }

    #region copy

    public virtual void CopyFrom(TestCfg src) {
        this._rate = src._rate;
        this._rate2 = src._rate2;
        this._v3orV4 = src._v3orV4;
        this._list1 = src._list1;
        this._list2 = src._list2;
    }

    #endregion

    #region ToString

    public override string ToString() {
        return "TestCfg{itemId: " + itemId + "}";
    }

    #endregion
}
}
