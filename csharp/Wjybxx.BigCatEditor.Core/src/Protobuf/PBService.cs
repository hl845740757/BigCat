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
using System.Linq;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// Rpc服务类型
/// </summary>
public class PBService : PBElement
{
    /// <summary>
    /// 生成的Service接口需要继承的接口
    ///
    /// 1.Protobuf的原生语法是不支持继承的，但我们的业务可能需要Rpc服务实现一些公共的接口。
    /// 2.建议由parser根据service的名字计算。
    /// </summary>
    private LinkedHashSet<string> superinterfaces = new();

    public override PBElementKind Kind => PBElementKind.Service;

    /// <summary>
    /// 获取定义的Rpc方法
    /// </summary>
    /// <returns></returns>
    public List<PBMethod> GetMethods() {
        return EnclosedElements.Where(e => e.Kind == PBElementKind.Method)
            .Cast<PBMethod>()
            .ToList();
    }
}
}