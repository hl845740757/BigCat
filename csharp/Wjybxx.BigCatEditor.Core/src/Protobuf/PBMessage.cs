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

using System.Collections.Generic;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// Message类型
///
/// 注意：oneof字段会被打包为<see cref="PBOneof"/>
/// </summary>
public class PBMessage : PBTypeElement
{
    public override PBElementKind Kind => PBElementKind.Message;

    /// <summary>
    /// 获取所有的字段
    /// </summary>
    /// <param name="includeOneof">是否包含oneof字段</param>
    /// <returns></returns>
    public List<PBField> GetFields(bool includeOneof = false) {
        List<PBField> result = new List<PBField>();
        foreach (PBElement element in EnclosedElements) {
            if (element.Kind == PBElementKind.Field) {
                result.Add((PBField)element);
                continue;
            }
            // oneof内部元素只应该是字段
            if (includeOneof && element.Kind == PBElementKind.Oneof) {
                foreach (PBElement oneofElement in element.EnclosedElements) {
                    result.Add((PBField)oneofElement);
                }
            }
        }
        return result;
    }
}
}