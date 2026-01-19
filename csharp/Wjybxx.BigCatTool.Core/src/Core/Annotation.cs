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
using Wjybxx.Commons;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatTool.Core
{
/// <summary>
/// 注解数据，
/// 格式：<code>//@Type{}</code>
/// 
/// 注意：注解暂不支持换行，注解换行会导致复杂的token解析。
/// </summary>
public sealed class Annotation
{
    /** 注解类型 */
    public readonly string type;
    /** 注解值 -- dson格式，object或array */
    public readonly string value;
    /** 解析缓存 -- 延迟初始化 */
    [NonSerialized] private DsonValue? _dsonValue;

    public Annotation(string type, string value) {
        this.type = type;
        this.value = value;
    }

    public Annotation(string type, DsonValue dsonValue, string? rawValue = null) {
        this.type = type;
        this._dsonValue = dsonValue ?? throw new ArgumentNullException(nameof(dsonValue));
        this.value = rawValue ?? dsonValue.ToDson(ObjectStyle.Flow);
    }

    public DsonValue dsonValue {
        get => _dsonValue ??= Dsons.FromDson(value);
        set => _dsonValue = value; // 用于运行时替换数据
    }

    public DsonObject<string> AsObject() => dsonValue.AsObject();

    public DsonArray<string> AsArray() => dsonValue.AsArray();

    public override string ToString() {
        return $"{nameof(type)}: {type}, {nameof(value)}: {value}";
    }

    #region parse

    /** 解析注解 */
    public static Annotation? TryParseAnnotation(string comment) {
        // 允许'//'和'@'符号之间有空格，但'@'符号后面的类名无空格，类名和'{}'可以有空格
        // '//@RpcService{}'
        int atIdx = ToolUtil.IndexOfNonWhitespace(comment, 2);
        if (atIdx < 0 || comment[atIdx] != '@') {
            return null; // '@'符号前面有其它内容
        }
        if (!TryGetRange(comment, out int startIndex, out int endIndex)) {
            return null;
        }
        string type = comment.Substring2(atIdx + 1, startIndex).Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            return null; // 类型信息为空
        }
        DsonValue dsonValue;
        string rawValue;
        if (startIndex > 0) {
            rawValue = comment.Substring2(startIndex, endIndex + 1);
            dsonValue = Dsons.FromDson(rawValue);
        } else {
            dsonValue = DsonNull.NULL;
            rawValue = "";
        }
        return new Annotation(type, dsonValue, rawValue);
    }

    /** 是否是注解类型注释 */
    public static bool IsAnnotationComment(string comment) {
        int atIdx = ToolUtil.IndexOfNonWhitespace(comment, 2);
        if (atIdx < 0 || comment[atIdx] != '@') {
            return false; // '@'符号前面有其它内容
        }
        if (!TryGetRange(comment, out int startIndex, out int endIndex)) {
            return false;
        }
        string type = comment.Substring2(atIdx + 1, startIndex).Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            return false; // 类型信息为空
        }
        return true;
    }

    private static bool TryGetRange(string comment, out int startIndex, out int endIndex) {
        // 允许object和array格式 -- 需要判断[和{出现的顺序，不能优先处理其中的某个，否则可能索引到其内部元素
        int arrIndex = comment.IndexOf('[');
        int objIndex = comment.IndexOf('{');
        if (arrIndex < 0 && objIndex < 0) {
            startIndex = 0;
            endIndex = 0;
            return false;
        }
        if (arrIndex >= 0 && arrIndex < objIndex) {
            startIndex = arrIndex;
            endIndex = comment.LastIndexOf(']');
        } else {
            startIndex = objIndex;
            endIndex = comment.LastIndexOf('}');
        }
        return endIndex > startIndex;
    }

    #endregion

    #region util

    /// <summary>
    /// 获取bool类型属性
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static bool GetBool(DsonObject<string> options, string key, bool defValue = false) {
        if (!options.TryGetValue(key, out DsonValue value)) {
            return defValue;
        }
        if (value.DsonType == DsonType.Bool) return value.AsBool();
        if (value.IsNumber) {
            int number = value.AsNumber().IntValue;
            if (number == 0) return false;
            if (number == 1) return true;
            throw new Exception("invalid number: " + number);
        }
        return defValue;
    }

    /// <summary>
    /// 获取int类型属性值
    /// </summary>
    /// <param name="options"></param>
    /// <param name="key"></param>
    /// <param name="defValue"></param>
    /// <returns></returns>
    public static int GetInt(DsonObject<string> options, string key, int defValue = 0) {
        if (!options.TryGetValue(key, out DsonValue value)) {
            return defValue;
        }
        return value.IsNumber ? value.AsNumber().IntValue : defValue;
    }

    /// <summary>
    /// 获取number类型属性值
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static double GetNumber(DsonObject<string> options, string key, double defValue = 0) {
        if (!options.TryGetValue(key, out DsonValue value)) {
            return defValue;
        }
        return value.IsNumber ? value.AsNumber().DoubleValue : defValue;
    }

    /// <summary>
    /// 获取string类型属性值
    /// </summary>
    /// <param name="options">表头options</param>
    /// <param name="key">Key</param>
    /// <param name="defValue">key不存在时的默认值</param>
    /// <returns></returns>
    public static string GetString(DsonObject<string> options, string key, string defValue = null) {
        if (!options.TryGetValue(key, out DsonValue value)) {
            return defValue;
        }
        return value.DsonType == DsonType.String ? value.AsString() : defValue;
    }

    #endregion
}
}