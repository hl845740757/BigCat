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

using UnityEngine;
using Wjybxx.BigCat.Core;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Editor.DataScript
{
public class VarAABBCodec : IVarCodec
{
    public void WriteVariable(IDsonWriter<string> writer, Variable variable, DataGraphHelper helper) {
        const Double4Style style = Double4Style.Vector | Double4Style.Len3;
        Vector3 min = variable[0].vector3Value;
        Vector3 size = variable[1].vector3Value - min;
        // min + size的可维护性更高
        writer.WriteStartObject(ObjectStyle.Flow);
        writer.WriteDouble4("min", min.ToDouble4(), style);
        writer.WriteDouble4("size", size.ToDouble4(), style);
        writer.WriteEndObject();
    }

    public void ReadVariable(DsonValue dsonValue, Variable variable, bool applySerializedType, DataGraphHelper helper) {
        DsonObject<string> dsonObject = dsonValue.AsObject();
        if (dsonObject.TryGetValue("min", out DsonValue min)) {
            helper.ReadVariable(variable[0], min);
        }
        if (dsonObject.TryGetValue("max", out DsonValue max)) {
            helper.ReadVariable(variable[1], max);
        }
        if (dsonObject.TryGetValue("size", out DsonValue size)) {
            helper.ReadVariable(variable[1], size);
            variable[1].vector3Value += variable[0].vector3Value; // min + size
        }
    }
}
}