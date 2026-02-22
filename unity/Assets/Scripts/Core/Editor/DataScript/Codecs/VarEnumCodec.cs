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

using System.IO;
using System.Text;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarEnumCodec : IVarCodec
{
    private readonly StringBuilder _sbCache = new StringBuilder();

    public void WriteVariable(IDsonWriter<string> writer, Variable variable, DataGraphHelper helper) {
        DSNamedType varType = variable.type;
        if (!helper.IsWriteEnumAsString(variable)) {
            NumberStyle style = DSUtil.IsFlagEnum(varType) ? NumberStyle.Hex : NumberStyle.Simple;
            writer.WriteInt32(variable.intValue, style);
            return;
        }
        int value = variable.intValue;
        if (!DSUtil.IsFlagEnum(varType) || MathCommon.BitCount(value) <= 1) {
            DSEnumValue enumValue = varType.GetEnumValue(variable.intValue);
            if (enumValue == null) {
                throw new InvalidDataException($"enumValue {variable.intValue} is absent");
            }
            writer.WriteString(enumValue.Name, StringStyle.Unquote);
            return;
        }
        // 输出为 A | B |C
        StringBuilder sb = _sbCache.Clear();
        foreach (DSElement element in varType.EnclosedElements) {
            if (element is not DSEnumValue enumValue) continue;
            if (value.IsIntersect(enumValue.Number)) {
                if (sb.Length > 0) sb.Append('|');
                sb.Append(enumValue.Name);
            }
        }
        writer.WriteString(sb.ToString(), StringStyle.Quote); // 需引号
    }

    public void ReadVariable(DsonValue dsonValue, Variable variable, bool applySerializedType, DataGraphHelper helper) {
        DSNamedType varType = variable.type;
        if (dsonValue.IsNumber) {
            variable.longValue = dsonValue.AsNumber().IntValue;
            return;
        }
        if (dsonValue.DsonType == DsonType.String) { // 可能是字典的key
            string stringValue = dsonValue.AsString();
            if (int.TryParse(stringValue, out int intValue)) {
                variable.longValue = intValue;
                return;
            }
            if (stringValue.Contains('|')) { // Flags
                variable.longValue = ParseFlagsEnum(varType, stringValue);
                return;
            }
            DSEnumValue enumValue = varType.GetEnumValue(stringValue, true);
            variable.longValue = enumValue == null ? 0 : enumValue.Number;
        }
        // 还可以支持数组
    }

    private static int ParseFlagsEnum(DSNamedType namedType, string str) {
        int value = 0;
        foreach (string name in ObjectUtil.SplitAndTrim(str, '|')) {
            DSEnumValue enumValue = namedType.GetEnumValue(name, true);
            if (enumValue == null) {
                continue; // 解码过程都是有损恢复
            }
            value |= enumValue.Number;
        }
        return value;
    }
}
}