using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using System;
using Wjybxx.Dson.Types;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.BigCat.Fx;
using Wjybxx.Dson.Codec;
using System.Text;

namespace Wjybxx.BigCatTool.Tests.Generated
{
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "SimpleBean" })]
public class SimpleBean
{
    #nullable disable
    // ReSharper disable All
    private readonly int _age;
    private readonly string _name;
    /// <summary>
    /// 测试nullable
    /// </summary>
    private int? _opt;
    /// <summary>
    /// 测试时间类型
    /// </summary>
    private DateTime _dt;
    /// <summary>
    /// 测试字节数组    
    /// </summary>
    private Binary _data1;
    /// <summary>
    /// 测试List
    /// </summary>
    private List<int> _list;
    /// <summary>
    /// 测试HashSet
    /// </summary>
    private HashSet<int> _hashset;
    /// <summary>
    /// 测试Map
    /// </summary>
    private Dictionary<int, string> _dic;
    /// <summary>
    /// 测试二维List    
    /// </summary>
    private List<List<int>> _listX;
    /// <summary>
    /// @Options {ssti: true}
    /// 测试ssti
    /// </summary>
    private int _strLink;
    [NonSerialized]
    private string _strLinkCache;
    /// <summary>
    /// @Options {ssti: true}    
    /// 测试sstiList
    /// </summary>
    private List<int> _strLinkList;
    [NonSerialized]
    private ImmutableList<string> _strLinkListCache;

    public SimpleBean(int age, string name) {
        this._age = age;
        this._name = name;
    }

    public int age => _age;
    public string name => _name;
    public int? opt {
        get => _opt;
        set => this._opt = value;
    }

    public DateTime dt {
        get => _dt;
        set => this._dt = value;
    }

    public Binary data1 {
        get => _data1;
        set => this._data1 = value;
    }

    public List<int> list {
        get => _list;
        set => this._list = value;
    }

    public HashSet<int> hashset {
        get => _hashset;
        set => this._hashset = value;
    }

    public Dictionary<int, string> dic {
        get => _dic;
        set => this._dic = value;
    }

    public List<List<int>> listX {
        get => _listX;
        set => this._listX = value;
    }

    public string strLink => _strLinkCache ??= SstMgr.GetString(_strLink);
    public ImmutableList<string> strLinkList => _strLinkListCache ??= SstMgr.GetStringList(_strLinkList);
    #region codec

    public SimpleBean(IDsonObjectReader reader) {
        this._age = reader.ReadInt("age");
        this._name = reader.ReadString("name");
        if (reader.ReadName("opt")) this._opt = reader.ReadObject<int?>(null);
        if (reader.ReadName("dt")) this._dt = reader.ReadDateTime(null);
        if (reader.ReadName("data1")) this._data1 = reader.ReadBinary(null);
        if (reader.ReadName("list")) this._list = reader.ReadObject<List<int>>(null);
        if (reader.ReadName("hashset")) this._hashset = reader.ReadObject<HashSet<int>>(null);
        if (reader.ReadName("dic")) this._dic = reader.ReadObject<Dictionary<int, string>>(null);
        if (reader.ReadName("listX")) this._listX = reader.ReadObject<List<List<int>>>(null);
        if (reader.ReadName("strLink")) this._strLink = reader.ReadInt(null);
        if (reader.ReadName("strLinkList")) this._strLinkList = reader.ReadObject<List<int>>(null);
    }

    public virtual void WriteObject(IDsonObjectWriter writer) {
        writer.WriteInt("age", this._age);
        writer.WriteString("name", this._name);
        writer.WriteObject("opt", this._opt);
        writer.WriteDateTime("dt", this._dt);
        writer.WriteBinary("data1", this._data1);
        writer.WriteObject("list", this._list);
        writer.WriteObject("hashset", this._hashset);
        writer.WriteObject("dic", this._dic);
        writer.WriteObject("listX", this._listX);
        writer.WriteInt("strLink", this._strLink);
        writer.WriteObject("strLinkList", this._strLinkList);
    }

    public virtual void BeforeEncode(ConverterOptions options) {
    }

    public virtual void AfterDecode(ConverterOptions options) {
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((SimpleBean)obj);
    }

    protected virtual bool EqualsHelper(SimpleBean other) {
        if (this._age != other._age) return false;
        if (this._name != other._name) return false;
        if (this._opt != other._opt) return false;
        if (this._dt != other._dt) return false;
        if (!Equals(this._data1, other._data1)) return false;
        if (!CollectionUtil.SequenceEqual(this._list, other._list)) return false;
        if (!CollectionUtil.DataEquals(this._hashset, other._hashset)) return false;
        if (!CollectionUtil.DataEquals(this._dic, other._dic)) return false;
        if (!CollectionUtil.SequenceEqual(this._listX, other._listX)) return false;
        if (this._strLink != other._strLink) return false;
        if (!CollectionUtil.SequenceEqual(this._strLinkList, other._strLinkList)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = this._age.GetHashCode();
        hashCode = (hashCode * 397) ^ (this._name != null ? this._name.GetHashCode() :  0);
        hashCode = (hashCode * 397) ^ this._opt.GetHashCode();
        hashCode = (hashCode * 397) ^ this._dt.GetHashCode();
        hashCode = (hashCode * 397) ^ (this._data1 != null ? this._data1.GetHashCode() :  0);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._list);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._hashset);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._dic);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._listX);
        hashCode = (hashCode * 397) ^ this._strLink.GetHashCode();
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._strLinkList);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("age").Append(':').Append(this._age);
        sb.Append(", ");
        sb.Append("name").Append(':').Append(this._name != null ? this._name.ToString() :  "null");
        sb.Append(", ");
        sb.Append("opt").Append(':').Append(this._opt != null ? this._opt.ToString() :  "null");
        sb.Append(", ");
        sb.Append("dt").Append(':').Append(this._dt.ToString("s"));
        sb.Append(", ");
        sb.Append("data1").Append(':').Append(this._data1 != null ? this._data1.ToString() :  "null");
        sb.Append(", ");
        sb.Append("list").Append(':');
        CollectionUtil.ToStringHelper(this._list, sb);
        sb.Append(", ");
        sb.Append("hashset").Append(':');
        CollectionUtil.ToStringHelper(this._hashset, sb);
        sb.Append(", ");
        sb.Append("dic").Append(':');
        CollectionUtil.ToStringHelper(this._dic, sb);
        sb.Append(", ");
        sb.Append("listX").Append(':');
        CollectionUtil.ToStringHelper(this._listX, sb);
        sb.Append(", ");
        sb.Append("strLink").Append(':').Append(this._strLink);
        sb.Append(", ");
        sb.Append("strLinkList").Append(':');
        CollectionUtil.ToStringHelper(this._strLinkList, sb);
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "SimpleChildBean" })]
public class SimpleChildBean : SimpleBean
{
    #nullable disable
    // ReSharper disable All
    private List<string> _addresses;

    public SimpleChildBean(int age, string name)
        : base(age, name) {
    }

    public List<string> addresses {
        get => _addresses;
        set => this._addresses = value;
    }

    #region codec

    public SimpleChildBean(IDsonObjectReader reader)
        : base(reader) {
        if (reader.ReadName("addresses")) this._addresses = reader.ReadObject<List<string>>(null);
    }

    public override void WriteObject(IDsonObjectWriter writer) {
        base.WriteObject(writer);
        writer.WriteObject("addresses", this._addresses);
    }

    public override void BeforeEncode(ConverterOptions options) {
        base.BeforeEncode(options);
    }

    public override void AfterDecode(ConverterOptions options) {
        base.AfterDecode(options);
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((SimpleChildBean)obj);
    }

    protected override bool EqualsHelper(SimpleBean _other) {
        var other = (SimpleChildBean)_other;
        if (!base.EqualsHelper(other)) return false;
        if (!CollectionUtil.SequenceEqual(this._addresses, other._addresses)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = base.GetHashCode();
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._addresses);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append(base.ToString());
        sb.Append(", ");
        sb.Append("addresses").Append(':');
        CollectionUtil.ToStringHelper(this._addresses, sb);
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Flow, Names = new[] { "Vector3", "V3" })]
public struct Vector3 : IEquatable<Vector3>
{
    #nullable disable
    // ReSharper disable All
    private readonly float _x;
    private readonly float _y;
    private readonly float _z;

    public Vector3(float x, float y, float z) {
        this._x = x;
        this._y = y;
        this._z = z;
    }

    public float x => _x;
    public float y => _y;
    public float z => _z;
    #region codec

    public Vector3(IDsonObjectReader reader) {
        this._x = reader.ReadFloat("x");
        this._y = reader.ReadFloat("y");
        this._z = reader.ReadFloat("z");
    }

    public void WriteObject(IDsonObjectWriter writer) {
        writer.WriteFloat("x", this._x);
        writer.WriteFloat("y", this._y);
        writer.WriteFloat("z", this._z);
    }

    public void BeforeEncode(ConverterOptions options) {
    }

    public void AfterDecode(ConverterOptions options) {
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        return obj is Vector3 other && Equals(other);
    }

    public bool Equals(Vector3 other) {
        if (this._x != other._x) return false;
        if (this._y != other._y) return false;
        if (this._z != other._z) return false;
        return true;
    }

    public static bool operator ==(Vector3 left, Vector3 right) {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3 left, Vector3 right) {
        return !left.Equals(right);
    }

    public override int GetHashCode() {
        int hashCode = this._x.GetHashCode();
        hashCode = (hashCode * 397) ^ this._y.GetHashCode();
        hashCode = (hashCode * 397) ^ this._z.GetHashCode();
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("x").Append(':').Append(this._x);
        sb.Append(", ");
        sb.Append("y").Append(':').Append(this._y);
        sb.Append(", ");
        sb.Append("z").Append(':').Append(this._z);
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
public enum Color
{
    White = 0,
    Red = 1,
    Green = 2,
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "GenericBean" })]
public class GenericBean<T, U> 
        where T : struct
        where U : class 
{
    #nullable disable
    // ReSharper disable All
    private T? _key;
    private U _value;

    public GenericBean() {
    }

    public T? key {
        get => _key;
        set => this._key = value;
    }

    public U value {
        get => _value;
        set => this._value = value;
    }

    #region codec

    public GenericBean(IDsonObjectReader reader) {
        if (reader.ReadName("key")) this._key = reader.ReadObject<T?>(null);
        if (reader.ReadName("value")) this._value = reader.ReadObject<U>(null);
    }

    public virtual void WriteObject(IDsonObjectWriter writer) {
        writer.WriteObject("key", this._key);
        writer.WriteObject("value", this._value);
    }

    public virtual void BeforeEncode(ConverterOptions options) {
    }

    public virtual void AfterDecode(ConverterOptions options) {
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((GenericBean<T, U>)obj);
    }

    protected virtual bool EqualsHelper(GenericBean<T, U> other) {
        if (!this._key.Equals(other._key)) return false;
        if (!Equals(this._value, other._value)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = this._key.GetHashCode();
        hashCode = (hashCode * 397) ^ (this._value != null ? this._value.GetHashCode() :  0);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("key").Append(':').Append(this._key != null ? this._key.ToString() :  "null");
        sb.Append(", ");
        sb.Append("value").Append(':').Append(this._value != null ? this._value.ToString() :  "null");
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "GenericChildBean" })]
public class GenericChildBean<T, U> : GenericBean<T, U> 
        where T : struct
        where U : class 
{
    #nullable disable
    // ReSharper disable All
    private List<string> _addresses;

    public GenericChildBean() {
    }

    public List<string> addresses {
        get => _addresses;
        set => this._addresses = value;
    }

    #region codec

    public GenericChildBean(IDsonObjectReader reader)
        : base(reader) {
        if (reader.ReadName("addresses")) this._addresses = reader.ReadObject<List<string>>(null);
    }

    public override void WriteObject(IDsonObjectWriter writer) {
        base.WriteObject(writer);
        writer.WriteObject("addresses", this._addresses);
    }

    public override void BeforeEncode(ConverterOptions options) {
        base.BeforeEncode(options);
    }

    public override void AfterDecode(ConverterOptions options) {
        base.AfterDecode(options);
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((GenericChildBean<T, U>)obj);
    }

    protected override bool EqualsHelper(GenericBean<T, U> _other) {
        var other = (GenericChildBean<T, U>)_other;
        if (!base.EqualsHelper(other)) return false;
        if (!CollectionUtil.SequenceEqual(this._addresses, other._addresses)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = base.GetHashCode();
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._addresses);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append(base.ToString());
        sb.Append(", ");
        sb.Append("addresses").Append(':');
        CollectionUtil.ToStringHelper(this._addresses, sb);
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "GenericChildBean2" })]
public class GenericChildBean2<T, U> : GenericBean<T, List<string>> 
        where T : struct
        where U : class 
{
    #nullable disable
    // ReSharper disable All
    private List<string> _addresses;

    public GenericChildBean2() {
    }

    public List<string> addresses {
        get => _addresses;
        set => this._addresses = value;
    }

    #region codec

    public GenericChildBean2(IDsonObjectReader reader)
        : base(reader) {
        if (reader.ReadName("addresses")) this._addresses = reader.ReadObject<List<string>>(null);
    }

    public override void WriteObject(IDsonObjectWriter writer) {
        base.WriteObject(writer);
        writer.WriteObject("addresses", this._addresses);
    }

    public override void BeforeEncode(ConverterOptions options) {
        base.BeforeEncode(options);
    }

    public override void AfterDecode(ConverterOptions options) {
        base.AfterDecode(options);
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((GenericChildBean2<T, U>)obj);
    }

    protected override bool EqualsHelper(GenericBean<T, List<string>> _other) {
        var other = (GenericChildBean2<T, U>)_other;
        if (!base.EqualsHelper(other)) return false;
        if (!CollectionUtil.SequenceEqual(this._addresses, other._addresses)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = base.GetHashCode();
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._addresses);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append(base.ToString());
        sb.Append(", ");
        sb.Append("addresses").Append(':');
        CollectionUtil.ToStringHelper(this._addresses, sb);
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "OuterClass" })]
public class OuterClass
{
    #nullable disable
    // ReSharper disable All
    private float _x;
    private float _y;
    private OuterClass.Request _request;
    private OuterClass.Result _result;

    public OuterClass() {
    }

    public float x {
        get => _x;
        set => this._x = value;
    }

    public float y {
        get => _y;
        set => this._y = value;
    }

    public OuterClass.Request request {
        get => _request;
        set => this._request = value;
    }

    public OuterClass.Result result {
        get => _result;
        set => this._result = value;
    }

    #region codec

    public OuterClass(IDsonObjectReader reader) {
        if (reader.ReadName("x")) this._x = reader.ReadFloat(null);
        if (reader.ReadName("y")) this._y = reader.ReadFloat(null);
        if (reader.ReadName("request")) this._request = reader.ReadObject<OuterClass.Request>(null);
        if (reader.ReadName("result")) this._result = reader.ReadObject<OuterClass.Result>(null);
    }

    public virtual void WriteObject(IDsonObjectWriter writer) {
        writer.WriteFloat("x", this._x);
        writer.WriteFloat("y", this._y);
        writer.WriteObject("request", this._request);
        writer.WriteObject("result", this._result);
    }

    public virtual void BeforeEncode(ConverterOptions options) {
    }

    public virtual void AfterDecode(ConverterOptions options) {
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((OuterClass)obj);
    }

    protected virtual bool EqualsHelper(OuterClass other) {
        if (this._x != other._x) return false;
        if (this._y != other._y) return false;
        if (!Equals(this._request, other._request)) return false;
        if (!Equals(this._result, other._result)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = this._x.GetHashCode();
        hashCode = (hashCode * 397) ^ this._y.GetHashCode();
        hashCode = (hashCode * 397) ^ (this._request != null ? this._request.GetHashCode() :  0);
        hashCode = (hashCode * 397) ^ (this._result != null ? this._result.GetHashCode() :  0);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("x").Append(':').Append(this._x);
        sb.Append(", ");
        sb.Append("y").Append(':').Append(this._y);
        sb.Append(", ");
        sb.Append("request").Append(':').Append(this._request != null ? this._request.ToString() :  "null");
        sb.Append(", ");
        sb.Append("result").Append(':').Append(this._result != null ? this._result.ToString() :  "null");
        return sb.ToString();
    }

    #endregion

    [DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "Request" })]
    public class Request
    {
        #nullable disable
        // ReSharper disable All
        private string _value;

        public Request() {
        }

        public string value {
            get => _value;
            set => this._value = value;
        }

        #region codec

        public Request(IDsonObjectReader reader) {
            if (reader.ReadName("value")) this._value = reader.ReadString(null);
        }

        public virtual void WriteObject(IDsonObjectWriter writer) {
            writer.WriteString("value", this._value);
        }

        public virtual void BeforeEncode(ConverterOptions options) {
        }

        public virtual void AfterDecode(ConverterOptions options) {
        }

        #endregion

        #region equals

        public override bool Equals(object? obj) {
            if (null == obj) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (this.GetType() != obj.GetType()) return false;
            return EqualsHelper((OuterClass.Request)obj);
        }

        protected virtual bool EqualsHelper(OuterClass.Request other) {
            if (this._value != other._value) return false;
            return true;
        }

        public override int GetHashCode() {
            int hashCode = (this._value != null ? this._value.GetHashCode() :  0);
            return hashCode;
        }

        #endregion

        #region ToString

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("value").Append(':').Append(this._value != null ? this._value.ToString() :  "null");
            return sb.ToString();
        }

        #endregion
    }

    [DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "Result" })]
    public class Result
    {
        #nullable disable
        // ReSharper disable All
        private string _value;

        public Result() {
        }

        public string value {
            get => _value;
            set => this._value = value;
        }

        #region codec

        public Result(IDsonObjectReader reader) {
            if (reader.ReadName("value")) this._value = reader.ReadString(null);
        }

        public virtual void WriteObject(IDsonObjectWriter writer) {
            writer.WriteString("value", this._value);
        }

        public virtual void BeforeEncode(ConverterOptions options) {
        }

        public virtual void AfterDecode(ConverterOptions options) {
        }

        #endregion

        #region equals

        public override bool Equals(object? obj) {
            if (null == obj) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (this.GetType() != obj.GetType()) return false;
            return EqualsHelper((OuterClass.Result)obj);
        }

        protected virtual bool EqualsHelper(OuterClass.Result other) {
            if (this._value != other._value) return false;
            return true;
        }

        public override int GetHashCode() {
            int hashCode = (this._value != null ? this._value.GetHashCode() :  0);
            return hashCode;
        }

        #endregion

        #region ToString

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("value").Append(':').Append(this._value != null ? this._value.ToString() :  "null");
            return sb.ToString();
        }

        #endregion
    }
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Style = ObjectStyle.Indent, Names = new[] { "PeerClass" })]
public class PeerClass
{
    #nullable disable
    // ReSharper disable All
    private OuterClass.Request _request;
    private OuterClass.Result _result;

    public PeerClass() {
    }

    public OuterClass.Request request {
        get => _request;
        set => this._request = value;
    }

    public OuterClass.Result result {
        get => _result;
        set => this._result = value;
    }

    #region codec

    public PeerClass(IDsonObjectReader reader) {
        if (reader.ReadName("request")) this._request = reader.ReadObject<OuterClass.Request>(null);
        if (reader.ReadName("result")) this._result = reader.ReadObject<OuterClass.Result>(null);
    }

    public virtual void WriteObject(IDsonObjectWriter writer) {
        writer.WriteObject("request", this._request);
        writer.WriteObject("result", this._result);
    }

    public virtual void BeforeEncode(ConverterOptions options) {
    }

    public virtual void AfterDecode(ConverterOptions options) {
    }

    #endregion

    #region equals

    public override bool Equals(object? obj) {
        if (null == obj) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return false;
        return EqualsHelper((PeerClass)obj);
    }

    protected virtual bool EqualsHelper(PeerClass other) {
        if (!Equals(this._request, other._request)) return false;
        if (!Equals(this._result, other._result)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = (this._request != null ? this._request.GetHashCode() :  0);
        hashCode = (hashCode * 397) ^ (this._result != null ? this._result.GetHashCode() :  0);
        return hashCode;
    }

    #endregion

    #region ToString

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append("request").Append(':').Append(this._request != null ? this._request.ToString() :  "null");
        sb.Append(", ");
        sb.Append("result").Append(':').Append(this._result != null ? this._result.ToString() :  "null");
        return sb.ToString();
    }

    #endregion
}
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
public interface FirstService
{
    Vector3 Echo(Vector3 v3);

    List<Vector3> Echo(List<Vector3> list);
}
}
