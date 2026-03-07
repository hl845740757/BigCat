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

using System;
using System.IO;
using Wjybxx.BigCat.Assetor;
using Wjybxx.BTree;
using Wjybxx.Commons;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;

namespace Wjybxx.BigCat.Editor.Assetor.Tasks
{
/// <summary>
/// 编辑器下直接将收集器到信息生成PackageInfo文件
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class EditorBuildTask : LeafTask<Blackboard>
{
    /// <summary>
    /// 二进制格式输出路径
    /// </summary>
    public string binFilePath;
    /// <summary>
    /// 文本格式输出路径
    /// </summary>
    public string textFilePath;

    protected override void Execute() {
        BuildPackageInfo packageInfo = blackboard.Get(BuildKeys.packageInfo);
        string binFilePath = UnityEditorUtil.ConvertToFilePath(this.binFilePath);
        if (File.Exists(binFilePath)) {
            File.Delete(binFilePath);
        }
        string textFilePath = UnityEditorUtil.ConvertToFilePath(this.textFilePath);
        if (File.Exists(textFilePath)) {
            File.Delete(textFilePath);
        }
        AssetPackageInfo manifest = packageInfo.Build();
        if (!string.IsNullOrEmpty(binFilePath)) {
            using DsonOutputs.ArrayOutput output = DsonOutputs.NewInstance(IArrayPool<byte>.Shared, 64 * 1024, 1024 * 1024);
            using DsonBinaryWriter<int> writer = new DsonBinaryWriter<int>(DsonWriterSettings.Default, output, false);
            manifest.Serialize(writer);
            //
            byte[] data = ArrayUtil.CopyOf(output.Buffer, 0, output.Position);
            File.WriteAllBytes(binFilePath, data);
        }
        if (!string.IsNullOrEmpty(textFilePath)) {
            DsonWriterSettings writerSettings = new DsonTextWriterSettings.Builder() { SoftLineLength = 150 }.Build();

            using StreamWriter streamWriter = new StreamWriter(File.Create(textFilePath), UnityEditorUtil.UTF8);
            using DsonTextWriter textWriter = new DsonTextWriter(writerSettings as DsonTextWriterSettings, streamWriter);
            manifest.Serialize(textWriter);
        }
        SetSuccess();
    }

    protected override void OnEventImpl(object eventObj) {
    }
}
}