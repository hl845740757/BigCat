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

using Wjybxx.BigCatEditor.Protobuf;
using Wjybxx.Commons;

namespace Wjybxx.BigCatEditor.Generator.Protobuf
{
/// <summary>
/// 用于扩展行为
///
/// 1.相比于每个Service都手动配置，通过命名或id进行区分更符合一般工程设计.
/// 2.以下接口只有在PB文件上未显示定义的情况下才会调用。
/// </summary>
public interface ServiceGeneratorHandler
{
    /// <summary>
    /// 如果rpc方法没有指定方法参数的名字，则由该函数计算默认的名字
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    string ParameterName(string typeName) {
        return "request";
    }

    /// <summary>
    /// 是否是异步方法
    /// </summary>
    /// <returns></returns>
    bool IsAsyncMethod(PBMethod method) => false;

    /// <summary>
    /// 方法是否需要context参数
    /// </summary>
    bool IsRequireContext(PBMethod method) => false;

    /// <summary>
    /// 方法是否手动返回结果
    /// </summary>
    bool IsManualReturn(PBMethod method) => false;
}
}