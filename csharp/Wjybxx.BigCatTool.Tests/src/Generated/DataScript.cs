using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec.Attributes;
using System;
using Wjybxx.Dson.Types;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.BigCat.Fx;
using Wjybxx.Dson.Codec;
using System.Text;

namespace Wjybxx.BigCatTool.Tests.Generated
{/// <summary>
/// 测试普通类
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "SimpleBean" })]
public class SimpleBean
{
    #nullable disable
    // ReSharper disable All
    private int _age;
    private string _name;
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

    public string strLink => SstMgr.GetString(_strLink);
    public ImmutableList<string> strLinkList => _strLinkListCache ??= SstMgr.GetStringList(_strLinkList);
    #region codec

    public SimpleBean(IDsonObjectReader reader) {
    }

    public virtual void ReadFields(IDsonObjectReader reader) {
        this._age = reader.ReadInt();
        this._name = reader.ReadString();
        this._opt = reader.ReadObject<int?>(default);
        this._dt = reader.ReadDateTime();
        this._data1 = reader.ReadBinary();
        this._list = reader.ReadObject<List<int>>(default);
        this._hashset = reader.ReadObject<HashSet<int>>(default);
        this._dic = reader.ReadObject<Dictionary<int, string>>(default);
        this._listX = reader.ReadObject<List<List<int>>>(default);
        this._strLink = reader.ReadInt();
        this._strLinkList = reader.ReadObject<List<int>>(default);
    }

    public virtual bool ReadField(IDsonObjectReader reader, string name) {
        switch (name) {
            case "age": this._age = reader.ReadInt(); return true;
            case "name": this._name = reader.ReadString(); return true;
            case "opt": this._opt = reader.ReadObject<int?>(default); return true;
            case "dt": this._dt = reader.ReadDateTime(); return true;
            case "data1": this._data1 = reader.ReadBinary(); return true;
            case "list": this._list = reader.ReadObject<List<int>>(default); return true;
            case "hashset": this._hashset = reader.ReadObject<HashSet<int>>(default); return true;
            case "dic": this._dic = reader.ReadObject<Dictionary<int, string>>(default); return true;
            case "listX": this._listX = reader.ReadObject<List<List<int>>>(default); return true;
            case "strLink": this._strLink = reader.ReadInt(); return true;
            case "strLinkList": this._strLinkList = reader.ReadObject<List<int>>(default); return true;
            default: return false;
        }
    }

    public virtual void WriteFields(IDsonObjectWriter writer) {
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
/// <summary>
/// 测试普通类继承
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "SimpleChildBean" })]
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
    }

    public override void ReadFields(IDsonObjectReader reader) {
        base.ReadFields(reader);
        this._addresses = reader.ReadObject<List<string>>(default);
    }

    public override bool ReadField(IDsonObjectReader reader, string name) {
        if (base.ReadField(reader, name)) return true;
        switch (name) {
            case "addresses": this._addresses = reader.ReadObject<List<string>>(default); return true;
            default: return false;
        }
    }

    public override void WriteFields(IDsonObjectWriter writer) {
        base.WriteFields(writer);
        writer.WriteObject("addresses", this._addresses);
    }

    public override void BeforeEncode(ConverterOptions options) {
    }

    public override void AfterDecode(ConverterOptions options) {
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
/// <summary>
/// 测试结构体，顺便测试codec注解解析
/// @Options{ alias: [Vector3, V3], style: flow }
/// @Editor{ displayType: Vector3 }
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "Vector3", "V3" }, EncodeFeatures = (SerializeFeatures)2097152)]
public struct Vector3 : IEquatable<Vector3>
{
    #nullable disable
    // ReSharper disable All
    private float _x;
    private float _y;
    private float _z;

    public Vector3(float x, float y, float z) {
        this._x = x;
        this._y = y;
        this._z = z;
    }

    public float x => _x;
    public float y => _y;
    public float z => _z;
    #region codec

    public Vector3(IDsonObjectReader reader)
        : this() {
    }

    public void ReadFields(IDsonObjectReader reader) {
        this._x = reader.ReadFloat();
        this._y = reader.ReadFloat();
        this._z = reader.ReadFloat();
    }

    public bool ReadField(IDsonObjectReader reader, string name) {
        switch (name) {
            case "x": this._x = reader.ReadFloat(); return true;
            case "y": this._y = reader.ReadFloat(); return true;
            case "z": this._z = reader.ReadFloat(); return true;
            default: return false;
        }
    }

    public void WriteFields(IDsonObjectWriter writer) {
        writer.WriteFloat("x", this._x);
        writer.WriteFloat("y", this._y);
        writer.WriteFloat("z", this._z);
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
/// <summary>
/// 测试枚举
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
public enum Color
{
    White = 0,
    Red = 1,
    Green = 2,
}
/// <summary>
/// 测试泛型类
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "GenericBean" })]
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
    }

    public virtual void ReadFields(IDsonObjectReader reader) {
        this._key = reader.ReadObject<T?>(default);
        this._value = reader.ReadObject<U>(default);
    }

    public virtual bool ReadField(IDsonObjectReader reader, string name) {
        switch (name) {
            case "key": this._key = reader.ReadObject<T?>(default); return true;
            case "value": this._value = reader.ReadObject<U>(default); return true;
            default: return false;
        }
    }

    public virtual void WriteFields(IDsonObjectWriter writer) {
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
/// <summary>
/// 测试泛型继承
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "GenericChildBean" })]
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
    }

    public override void ReadFields(IDsonObjectReader reader) {
        base.ReadFields(reader);
        this._addresses = reader.ReadObject<List<string>>(default);
    }

    public override bool ReadField(IDsonObjectReader reader, string name) {
        if (base.ReadField(reader, name)) return true;
        switch (name) {
            case "addresses": this._addresses = reader.ReadObject<List<string>>(default); return true;
            default: return false;
        }
    }

    public override void WriteFields(IDsonObjectWriter writer) {
        base.WriteFields(writer);
        writer.WriteObject("addresses", this._addresses);
    }

    public override void BeforeEncode(ConverterOptions options) {
    }

    public override void AfterDecode(ConverterOptions options) {
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
/// <summary>
/// 测试泛型继承2
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "GenericChildBean2" })]
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
    }

    public override void ReadFields(IDsonObjectReader reader) {
        base.ReadFields(reader);
        this._addresses = reader.ReadObject<List<string>>(default);
    }

    public override bool ReadField(IDsonObjectReader reader, string name) {
        if (base.ReadField(reader, name)) return true;
        switch (name) {
            case "addresses": this._addresses = reader.ReadObject<List<string>>(default); return true;
            default: return false;
        }
    }

    public override void WriteFields(IDsonObjectWriter writer) {
        base.WriteFields(writer);
        writer.WriteObject("addresses", this._addresses);
    }

    public override void BeforeEncode(ConverterOptions options) {
    }

    public override void AfterDecode(ConverterOptions options) {
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
/// <summary>
/// 测试内部类 
/// 测试内部类定义
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "OuterClass" })]
public class OuterClass
{
    #nullable disable
    // ReSharper disable All
    private float _x;
    private float _y;
    private OuterClass.Request _request;
    private OuterClass.Result _result;
    private List<Vector3> _vecList;
    private Dictionary<int, Color> _colorMap;
    private Dictionary<int, Vector3> _posMap;

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

    public List<Vector3> vecList {
        get => _vecList;
        set => this._vecList = value;
    }

    public Dictionary<int, Color> colorMap {
        get => _colorMap;
        set => this._colorMap = value;
    }

    public Dictionary<int, Vector3> posMap {
        get => _posMap;
        set => this._posMap = value;
    }

    #region codec

    public OuterClass(IDsonObjectReader reader) {
    }

    public virtual void ReadFields(IDsonObjectReader reader) {
        this._x = reader.ReadFloat();
        this._y = reader.ReadFloat();
        this._request = reader.ReadObject<OuterClass.Request>(default);
        this._result = reader.ReadObject<OuterClass.Result>(default);
        this._vecList = reader.ReadObject<List<Vector3>>(default);
        this._colorMap = reader.ReadObject<Dictionary<int, Color>>(default);
        this._posMap = reader.ReadObject<Dictionary<int, Vector3>>(default);
    }

    public virtual bool ReadField(IDsonObjectReader reader, string name) {
        switch (name) {
            case "x": this._x = reader.ReadFloat(); return true;
            case "y": this._y = reader.ReadFloat(); return true;
            case "request": this._request = reader.ReadObject<OuterClass.Request>(default); return true;
            case "result": this._result = reader.ReadObject<OuterClass.Result>(default); return true;
            case "vecList": this._vecList = reader.ReadObject<List<Vector3>>(default); return true;
            case "colorMap": this._colorMap = reader.ReadObject<Dictionary<int, Color>>(default); return true;
            case "posMap": this._posMap = reader.ReadObject<Dictionary<int, Vector3>>(default); return true;
            default: return false;
        }
    }

    public virtual void WriteFields(IDsonObjectWriter writer) {
        writer.WriteFloat("x", this._x);
        writer.WriteFloat("y", this._y);
        writer.WriteObject("request", this._request);
        writer.WriteObject("result", this._result);
        writer.WriteObject("vecList", this._vecList);
        writer.WriteObject("colorMap", this._colorMap);
        writer.WriteObject("posMap", this._posMap);
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
        if (!CollectionUtil.SequenceEqual(this._vecList, other._vecList)) return false;
        if (!CollectionUtil.DataEquals(this._colorMap, other._colorMap)) return false;
        if (!CollectionUtil.DataEquals(this._posMap, other._posMap)) return false;
        return true;
    }

    public override int GetHashCode() {
        int hashCode = this._x.GetHashCode();
        hashCode = (hashCode * 397) ^ this._y.GetHashCode();
        hashCode = (hashCode * 397) ^ (this._request != null ? this._request.GetHashCode() :  0);
        hashCode = (hashCode * 397) ^ (this._result != null ? this._result.GetHashCode() :  0);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._vecList);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._colorMap);
        hashCode = (hashCode * 397) ^ CollectionUtil.HashCode(this._posMap);
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
        sb.Append(", ");
        sb.Append("vecList").Append(':');
        CollectionUtil.ToStringHelper(this._vecList, sb);
        sb.Append(", ");
        sb.Append("colorMap").Append(':');
        CollectionUtil.ToStringHelper(this._colorMap, sb);
        sb.Append(", ");
        sb.Append("posMap").Append(':');
        CollectionUtil.ToStringHelper(this._posMap, sb);
        return sb.ToString();
    }

    #endregion

    [DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "OuterClass.Request" })]
    public class Request
    {
        #nullable disable
        // ReSharper disable All
        private int _a;
        private long _b;
        private string _str;

        public Request() {
        }

        public int a {
            get => _a;
            set => this._a = value;
        }

        public long b {
            get => _b;
            set => this._b = value;
        }

        public string str {
            get => _str;
            set => this._str = value;
        }

        #region codec

        public Request(IDsonObjectReader reader) {
        }

        public virtual void ReadFields(IDsonObjectReader reader) {
            this._a = reader.ReadInt();
            this._b = reader.ReadLong();
            this._str = reader.ReadString();
        }

        public virtual bool ReadField(IDsonObjectReader reader, string name) {
            switch (name) {
                case "a": this._a = reader.ReadInt(); return true;
                case "b": this._b = reader.ReadLong(); return true;
                case "str": this._str = reader.ReadString(); return true;
                default: return false;
            }
        }

        public virtual void WriteFields(IDsonObjectWriter writer) {
            writer.WriteInt("a", this._a);
            writer.WriteLong("b", this._b);
            writer.WriteString("str", this._str);
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
            if (this._a != other._a) return false;
            if (this._b != other._b) return false;
            if (this._str != other._str) return false;
            return true;
        }

        public override int GetHashCode() {
            int hashCode = this._a.GetHashCode();
            hashCode = (hashCode * 397) ^ this._b.GetHashCode();
            hashCode = (hashCode * 397) ^ (this._str != null ? this._str.GetHashCode() :  0);
            return hashCode;
        }

        #endregion

        #region ToString

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("a").Append(':').Append(this._a);
            sb.Append(", ");
            sb.Append("b").Append(':').Append(this._b);
            sb.Append(", ");
            sb.Append("str").Append(':').Append(this._str != null ? this._str.ToString() :  "null");
            return sb.ToString();
        }

        #endregion
    }

    [DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "OuterClass.Result" })]
    public class Result
    {
        #nullable disable
        // ReSharper disable All
        private int _a;
        private long _b;
        private string _str;

        public Result() {
        }

        public int a {
            get => _a;
            set => this._a = value;
        }

        public long b {
            get => _b;
            set => this._b = value;
        }

        public string str {
            get => _str;
            set => this._str = value;
        }

        #region codec

        public Result(IDsonObjectReader reader) {
        }

        public virtual void ReadFields(IDsonObjectReader reader) {
            this._a = reader.ReadInt();
            this._b = reader.ReadLong();
            this._str = reader.ReadString();
        }

        public virtual bool ReadField(IDsonObjectReader reader, string name) {
            switch (name) {
                case "a": this._a = reader.ReadInt(); return true;
                case "b": this._b = reader.ReadLong(); return true;
                case "str": this._str = reader.ReadString(); return true;
                default: return false;
            }
        }

        public virtual void WriteFields(IDsonObjectWriter writer) {
            writer.WriteInt("a", this._a);
            writer.WriteLong("b", this._b);
            writer.WriteString("str", this._str);
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
            if (this._a != other._a) return false;
            if (this._b != other._b) return false;
            if (this._str != other._str) return false;
            return true;
        }

        public override int GetHashCode() {
            int hashCode = this._a.GetHashCode();
            hashCode = (hashCode * 397) ^ this._b.GetHashCode();
            hashCode = (hashCode * 397) ^ (this._str != null ? this._str.GetHashCode() :  0);
            return hashCode;
        }

        #endregion

        #region ToString

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("a").Append(':').Append(this._a);
            sb.Append(", ");
            sb.Append("b").Append(':').Append(this._b);
            sb.Append(", ");
            sb.Append("str").Append(':').Append(this._str != null ? this._str.ToString() :  "null");
            return sb.ToString();
        }

        #endregion
    }
}
/// <summary>
/// 测试访问其它类型的内部类
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
[DsonSerializable(SkipFields = new[] { "*" }, Names = new[] { "PeerClass" })]
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
    }

    public virtual void ReadFields(IDsonObjectReader reader) {
        this._request = reader.ReadObject<OuterClass.Request>(default);
        this._result = reader.ReadObject<OuterClass.Result>(default);
    }

    public virtual bool ReadField(IDsonObjectReader reader, string name) {
        switch (name) {
            case "request": this._request = reader.ReadObject<OuterClass.Request>(default); return true;
            case "result": this._result = reader.ReadObject<OuterClass.Result>(default); return true;
            default: return false;
        }
    }

    public virtual void WriteFields(IDsonObjectWriter writer) {
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
/// <summary>
/// 测试实例 -- {}和[]暂时需要严格缩进
/// 大写名字表示关联类型的默认值
/// 测试实例从另一个模板初始化
/// 测试数组实例
/// 测试不换行缩进
/// 测试rpc服务
/// @Rpc{ id: 1 }
/// </summary>
[Generated("Wjybxx.BigCatTool.DataScript.CodeGenerator")]
public interface FirstService
{
    Vector3 Echo(Vector3 v3);

    List<Vector3> Echo(List<Vector3> list);
}
}
