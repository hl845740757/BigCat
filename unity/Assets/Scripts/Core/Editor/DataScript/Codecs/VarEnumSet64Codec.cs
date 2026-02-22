#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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

using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarEnumSet64Codec : IVarCodec
{
    public void WriteVariable(IDsonWriter<string> writer, Variable variable, DataGraphHelper helper) {
        DSNamedType enumType = (DSNamedType)variable.type.TypeArguments[0];
        long values = variable[1].longValue << 32 | variable[0].longValue;
        // 编码为 [A, B, C] 格式
        writer.WriteStartArray(ObjectStyle.Flow);
        foreach (DSElement element in enumType.EnclosedElements) {
            if (element is not DSEnumValue enumValue) continue;
            if (BitFlags.GetAt(values, enumValue.Number)) {
                writer.WriteString(enumValue.Name);
            }
        }
        writer.WriteEndArray();
    }

    public void ReadVariable(DsonValue dsonValue, Variable variable, bool applySerializedType, DataGraphHelper helper) {
        DSNamedType enumType = (DSNamedType)variable.type.TypeArguments[0];
        long values = 0;
        foreach (DsonValue enumName in dsonValue.AsArray()) {
            DSEnumValue enumValue = enumType.GetEnumValue(enumName.AsString(), true);
            if (enumValue == null) {
                continue; // 解码过程都是有损恢复
            }
            values = BitFlags.SetAt(values, enumValue.Number, true);
        }
        const long mask = uint.MaxValue;
        variable[0].longValue = (int)values;
        variable[1].longValue = (values >> 32) & mask;
    }
}
}