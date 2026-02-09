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

using Wjybxx.Dson;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 变量的编解码器
/// </summary>
public interface IVarCodec
{
    void WriteVariable(IDsonWriter<string> writer, Variable variable, DataGraphHelper helper);

    void ReadVariable(DsonValue dsonValue, Variable variable, bool applySerializedType, DataGraphHelper helper);
}
}