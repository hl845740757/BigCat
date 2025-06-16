#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 用于扩展类型功能
/// </summary>
public interface DSTypeHandler
{
    /// <summary>
    /// 值转换
    /// 
    /// 转换默认的文本解析结果，当用户为类型提供了特殊的语法时，应该实现该接口。
    /// (由于没有走完整的序列化过程，因此不能递归处理多态问题，该方案的实际收益不大)
    /// </summary>
    /// <param name="repository">归属的仓库，用于解析字段</param>
    /// <param name="namedType">当前类型，可能是泛型</param>
    /// <param name="srcValue">根据用户字符串解析出来的DsonValue</param>
    /// <returns></returns>
    DsonValue ConvertValue(DSRepository repository, DSNamedType namedType, DsonValue srcValue) {
        return srcValue;
    }
}
}