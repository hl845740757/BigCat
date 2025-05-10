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
using System.IO;
using Wjybxx.Commons;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatEditor.Core
{
/// <summary>
/// 注解数据，
/// 格式：<code>//@Type{}</code>
/// 注意：注解暂不支持换行，注解换行会导致复杂的token解析。
/// </summary>
public sealed class Annotation
{
    /** 注解类型 */
    public readonly string type;
    /** 注解值 -- dson格式 */
    public readonly string value;

    /** 解析缓存 -- 延迟初始化 */
    [NonSerialized] private DsonObject<string>? dsonValue;

    public Annotation(string type, string value) {
        this.type = type;
        this.value = value;
    }

    /** 用于程序动态构造注解 */
    public Annotation(string type, DsonObject<string> dsonValue) {
        this.type = type;
        this.dsonValue = dsonValue ?? throw new ArgumentNullException(nameof(dsonValue));
        this.value = dsonValue.ToDson(ObjectStyle.Flow);
    }

    public DsonObject<string> DsonValue {
        get {
            if (dsonValue == null) {
                dsonValue = Dsons.FromDson(value).AsObject();
            }
            return dsonValue;
        }
        set => dsonValue = value; // 用于运行时替换数据
    }


    public override string ToString() {
        return $"{nameof(type)}: {type}, {nameof(value)}: {value}";
    }

    #region parse

    /** 解析注解 */
    public static Annotation? TryParseAnnotation(string comment) {
        // 允许'//'和'@'符号之间有空格，但'@'符号后面的类名无空格，类名和'{}'可以有空格
        // '//@RpcService{}'
        int atIdx = Util.IndexOfNonWhitespace(comment, 2);
        if (atIdx < 0 || comment[atIdx] != '@') {
            return null; // '@'符号前面有其它内容
        }
        int valueStartIndex = comment.IndexOf('{');
        int valueEndIndex = comment.LastIndexOf('}');
        if (valueStartIndex < 0 || valueStartIndex >= valueEndIndex) {
            return null;
        }
        string type = comment.Substring2(atIdx + 1, valueStartIndex).Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            return null; // 类型信息为空
        }
        string value = comment.Substring2(valueStartIndex, valueEndIndex + 1);
        Annotation annotation = new Annotation(type, value);
        if (annotation.DsonValue == null) { // 提前检查dson文本格式
            throw new IOException("invalid dson value");
        }
        return annotation;
    }

    /** 是否是注解类型注释 */
    public static bool IsAnnotationComment(string comment) {
        int atIdx = Util.IndexOfNonWhitespace(comment, 2);
        if (atIdx < 0 || comment[atIdx] != '@') {
            return false; // '@'符号前面有其它内容
        }
        int valueStartIndex = comment.IndexOf('{');
        int valueEndIndex = comment.LastIndexOf('}');
        if (valueStartIndex < 0 || valueStartIndex >= valueEndIndex) {
            return false;
        }
        string type = comment.Substring2(atIdx + 1, valueStartIndex).Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            return false; // 类型信息为空
        }
        return true;
    }

    #endregion
}
}