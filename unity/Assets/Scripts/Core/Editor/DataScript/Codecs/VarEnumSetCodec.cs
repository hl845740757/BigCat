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

using Wjybxx.BigCat.Util;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarEnumSetCodec : IVarCodec
{
    public void WriteVariable(IDsonWriter<string> writer, Variable variable, DataGraphHelper helper) {
        Variable arrayField = variable[0];
        int[] bitArray = new int[arrayField.Count];
        for (int idx = 0; idx < arrayField.Count; idx++) {
            bitArray[idx] = arrayField[idx].intValue;
        }
        EnumSet<MockEnum> enumSet = EnumSet<MockEnum>.NewInstance(bitArray);
        DSNamedType enumType = (DSNamedType)variable.type.TypeArguments[0];
        //
        writer.WriteStartArray(ObjectStyle.Flow);
        foreach (DSElement element in enumType.EnclosedElements) {
            if (element is not DSEnumValue enumValue) continue;
            if (enumSet.Get(enumValue.Number)) {
                writer.WriteString(enumValue.Name);
            }
        }
        writer.WriteEndArray();
    }

    public void ReadVariable(DsonValue dsonValue, Variable variable, bool applySerializedType, DataGraphHelper helper) {
        EnumSet<MockEnum> enumSet = new EnumSet<MockEnum>();
        DSNamedType enumType = (DSNamedType)variable.type.TypeArguments[0];
        //
        foreach (DsonValue enumName in dsonValue.AsArray()) {
            DSEnumValue enumValue = enumType.GetEnumValue(enumName.AsString(), true);
            if (enumValue == null) {
                continue;
            }
            enumSet.Set(enumValue.Number); // 还好当初留了一手...
        }
        Variable arrayField = variable[0];
        arrayField.ClearArray();
        //
        int[] bitArray = enumSet.ToIntArray();
        foreach (int flags in bitArray) {
            Variable nestedVar = helper.Graph.CreateListItem(arrayField);
            nestedVar.intValue = flags;
            arrayField.Add(nestedVar);
        }
    }
}
}