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
using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 可扩展的服务
/// </summary>
[StableName]
public interface ExtensibleService
{
    /// <summary>
    /// 扩展黑板
    /// </summary>
    public Dictionary<string, object> ExtBlackboard { get; }

    /// <summary>
    /// 执行任意行为
    /// </summary>
    /// <param name="request">请求参数</param>
    [RpcMethod(MethodId = 9999)]
    public ExecuteResult Execute(ExecuteRequest request);

#nullable disable

    [DsonSerializable]
    public class ExecuteRequest
    {
        private string cmd;
        private Dictionary<string, object> paramDic = new();

        public string Cmd {
            get => cmd;
            set => cmd = value;
        }
        public Dictionary<string, object> ParamDic {
            get => paramDic;
            set => paramDic = value;
        }
    }

    [DsonSerializable]
    public class ExecuteResult
    {
        private int code;
        private object result;

        public ExecuteResult() {
        }

        public ExecuteResult(int code, object result) {
            this.code = code;
            this.result = result;
        }

        public int Code {
            get => code;
            set => code = value;
        }
        public object Result {
            get => result;
            set => result = value;
        }
    }
}
}