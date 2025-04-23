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
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// PB中的注解数据，格式：<code>//@Type{}</code>
/// </summary>
public sealed class PBAnnotation
{
    /** 注解类型 */
    public readonly string type;
    /** 注解值 -- dson格式 */
    public readonly string value;

    /** 解析缓存 -- 延迟初始化 */
    [NonSerialized] private DsonObject<string>? dsonValue;

    public PBAnnotation(string type, string value) {
        this.type = type;
        this.value = value;
    }

    /** 用于程序动态构造注解 */
    public PBAnnotation(string type, DsonObject<string> dsonValue) {
        this.type = type;
        this.dsonValue = dsonValue ?? throw new ArgumentNullException(nameof(dsonValue));
        this.value = dsonValue.ToDson(ObjectStyle.Flow);
    }

    public DsonObject<string>? DsonValue {
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
}
}