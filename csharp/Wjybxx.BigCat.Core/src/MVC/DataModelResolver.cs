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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCat.MVC
{
/// <summary>
/// 默认的基于反射的数据模型解析器
///
/// <h3>规则</h3>
/// 1.斜杠'/'开头表示绝对路径，从聚合数模开始访问；非斜杠开头表示相对路径，从父节点数据开始访问。<br></br>
/// 2.<code>/logic/xxx</code>表示访问逻辑层数据模型；<code>/view/xxx</code>表示访问视图层数据模型。<br></br>
/// 3.<code>{name}</code>大括号表示变量，括号内为变量名；变量名是约定的，有限的。<br></br>
/// 4.<code>{uiIndex}</code>表示取ui节点的下标；一个dataAddress中只能出现一次{uiIndex}。<br></br>
/// 3.字典类型仅支持Int32、Int64、string三种键。
/// 
/// PS：非线程安全，应该不会在主线程之外访问。
/// TODO 改为点号分隔符，规范化
/// </summary>
[NotThreadSafe]
public class DataModelResolver : IDataModelResolver
{
    /// <summary>
    /// 反射数据缓存
    ///
    /// 1.如果是普通类型，value是属性或字段 -- 查找时属性优先。
    /// 2.如果是List或字典，value是索引器（属性）。
    /// </summary>
    private readonly Dictionary<MemberKey, MemberInfo> memberCache = new Dictionary<MemberKey, MemberInfo>();
    /// <summary>
    /// 数据地址解析缓存
    /// </summary>
    private readonly Dictionary<string, List<Item>> itemCache = new Dictionary<string, List<Item>>();
    /// <summary>
    /// 用于字典查询时的缓存
    /// </summary>
    private readonly object[] _arrayCache = new object[1];

    public object Resolve(IAggregationModel aggregationModel, object? parentModel, string dataAddress, int uiIndex = -1) {
        dataAddress = dataAddress.Trim();
        if (string.IsNullOrWhiteSpace(dataAddress)) {
            return parentModel ?? aggregationModel;
        }

        object dataModel;
        if (dataAddress[0] == '/' || parentModel == null) {
            dataModel = aggregationModel;
        } else {
            dataModel = parentModel;
        }
        List<Item> itemList = SplitAddr(dataAddress);
        foreach (Item item in itemList) {
            dataModel = Resolve(aggregationModel, dataModel, item, uiIndex);
            if (dataModel == null) {
                throw new Exception("Could not resolve data model, addr: " + dataAddress);
            }
        }
        return dataModel;
    }

    private object? Resolve(IAggregationModel aggregationModel, object dataModel, Item item, int uiIndex) {
        // 如果是根节点，只有view和logic两个key
        if (dataModel == aggregationModel) {
            if (item.name == NAME_LOGIC) return aggregationModel.LogicModel;
            if (item.name == NAME_VIEW) return aggregationModel.ViewModel;
            return null;
        }
        Type type = dataModel.GetType();
        // 测试是否是List类型 - TODO 测试接口类型是否范围过广？
        if (type.GetInterface(CNAME_ILIST) != null) {
            PropertyInfo propertyInfo = GetIndexer(type);
            int index = item.IsUiIndex ? uiIndex : (int)item.number;
            return propertyInfo.GetValue(dataModel, new object[] { index });
        }
        // 测试是否是字典类型
        if (type.GetInterface(CNAME_IDICTIONARY) != null) {
            PropertyInfo propertyInfo = GetIndexer(type);
            ParameterInfo indexParameter = propertyInfo.GetIndexParameters()[0]; // 这里会创建数组，但字典使用频率不高
            if (indexParameter.ParameterType == typeof(int)) {
                int index = (int)item.number;
                _arrayCache[0] = index;
                return propertyInfo.GetValue(dataModel, _arrayCache);
            }
            if (indexParameter.ParameterType == typeof(long)) {
                long index = item.number;
                _arrayCache[0] = index;
                return propertyInfo.GetValue(dataModel, _arrayCache);
            }
            if (indexParameter.ParameterType == typeof(string)) {
                string index = item.name;
                _arrayCache[0] = index;
                return propertyInfo.GetValue(dataModel, _arrayCache);
            }
            return null;
        }
        // 自定义类型 - 通过属性或字段赋值
        MemberInfo memberInfo = GetPropertyOrField(type, item.name);
        if (memberInfo is PropertyInfo property) {
            return property.GetValue(dataModel);
        }
        FieldInfo fieldInfo = (FieldInfo)memberInfo;
        return fieldInfo.GetValue(dataModel);
    }

    private PropertyInfo GetIndexer(Type type) {
        MemberKey key = new MemberKey(type, "Item");
        if (memberCache.TryGetValue(key, out MemberInfo memberInfo)) {
            return (PropertyInfo)memberInfo;
        }
        PropertyInfo propertyInfo = type.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo == null) {
            throw new Exception("invalid type: " + type);
        }
        ParameterInfo indexParameter = propertyInfo.GetIndexParameters()[0];
        if (!(indexParameter.ParameterType == typeof(int)
              || indexParameter.ParameterType == typeof(long)
              || indexParameter.ParameterType == typeof(string))) {
            throw new Exception("invalid type: " + type);
        }
        memberCache[key] = propertyInfo;
        return propertyInfo;
    }

    private MemberInfo GetPropertyOrField(Type type, string memberName) {
        MemberKey key = new MemberKey(type, memberName);
        if (memberCache.TryGetValue(key, out MemberInfo memberInfo)) {
            return memberInfo;
        }
        memberInfo = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (memberInfo == null) {
            memberInfo = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        }
        if (memberInfo == null) {
            throw new Exception($"member: {memberName} not found, type: {type}");
        }
        memberCache[key] = memberInfo;
        return memberInfo;
    }

    private List<Item> SplitAddr(string addr) {
        if (itemCache.TryGetValue(addr, out List<Item> result)) {
            return result;
        }
        string[] splitArray = ObjectUtil.SplitAndTrim(addr, '/');
        result = new List<Item>(splitArray.Length);

        int uiIndexCount = 0;
        for (int index = 0; index < splitArray.Length; index++) {
            string str = splitArray[index];
            if (index == 0 && addr[0] == '/') { // 去除首字符斜杠的空白字符
                continue;
            }
            Item item = ParseItem(addr, str);
            if (item.IsUiIndex) {
                uiIndexCount++;
            }
            result.Add(item);
        }
        if (uiIndexCount > 1) {
            throw new Exception("uiIndexCount > 1, addr: " + addr);
        }
        itemCache[addr] = result;
        return result;
    }

    private static Item ParseItem(string addr, string itemStr) {
        if (string.IsNullOrWhiteSpace(itemStr)) {
            throw new Exception("empty, addr: " + addr);
        }
        string name = itemStr;
        int flags = 0;
        if (itemStr[0] == '{') {
            if (itemStr[itemStr.Length - 1] != '}') {
                throw new Exception("missing brace, addr: " + addr);
            }
            name = itemStr.Substring2(1, itemStr.Length - 1).Trim();
            flags |= MASK_VARIABLE;

            if (name == NAME_UI_INDEX) {
                flags |= MASK_UI_INDEX;
            }
        }
        if (!long.TryParse(itemStr, out long number)) {
            number = -1; // 尽量保持无意义
        }
        return new Item(name, number, flags);
    }

    private const string CNAME_ILIST = "System.Collections.Generic.IList`1";
    private const string CNAME_IDICTIONARY = "System.Collections.Generic.IDictionary`2";

    private const string NAME_UI_INDEX = "uiIndex";
    private const string NAME_VIEW = "view";
    private const string NAME_LOGIC = "logic";

    private const int MASK_VARIABLE = 0x01;
    private const int MASK_UI_INDEX = 0x02; // UI索引变量

    private readonly struct Item
    {
        /// <summary>
        /// path切割后的每一段
        /// </summary>
        public readonly string name;
        /// <summary>
        /// 
        /// </summary>
        public readonly long number;
        /// <summary>
        /// 特征值，避免大量bool
        /// </summary>
        public readonly int flags;

        public Item(string name, long number, int flags) {
            this.name = name;
            this.flags = flags;
            this.number = number;
        }

        public bool IsVariable => (flags & MASK_VARIABLE) != 0;
        public bool IsUiIndex => (flags & MASK_UI_INDEX) != 0;
    }

    private readonly struct MemberKey : IEquatable<MemberKey>
    {
        private readonly Type type;
        private readonly string name;

        public MemberKey(Type type, string name) {
            this.type = type;
            this.name = name;
        }

        public bool Equals(MemberKey other) {
            return type == other.type && name == other.name;
        }

        public override bool Equals(object? obj) {
            return obj is MemberKey other && Equals(other);
        }

        public override int GetHashCode() {
            return (type.GetHashCode() * 397) ^ name.GetHashCode();
        }
    }
}
}