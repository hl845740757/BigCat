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
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Editor.DataScript;
using Wjybxx.BigCat.Util;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BTree;
using Wjybxx.BTreeCodec;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCat.Editor.Assetor
{
public class BuildWindow : DataEditor
{
    private IDsonConverter converter;

    [MenuItem("Window/BigCat/PackageBuilder")]
    private static void OpenWindow() {
        BuildWindow wnd = GetWindow<BuildWindow>();
        wnd.titleContent = new GUIContent("BuildWindow");
        DSRepository repository = wnd.repository;
        // TODO 通过配置文件加载关联的ds文件
        string scriptDir = Application.dataPath + "/Resources/DataScript";
        foreach (string filePath in Directory.GetFiles(scriptDir, "*.ds", SearchOption.AllDirectories)) {
            DSFile dsFile = DSFileParser.Parse(new FileInfo(filePath));
            repository.AddFile(dsFile);
        }
        repository.Build();
    }

    public override void OnNodeExecuteRequest(NodeView nodeView) {
        string filePath = UnityEditorUtil.ConvertToFilePath(dataGraph.assetPath);
        DsonArray<string> collection = Dsons.FromCollectionDson(File.ReadAllText(filePath));

        int index = IndexOf(collection, nodeView.dataNode.localId);
        DsonValue root = collection[index];
        collection.RemoveAt(index);
        collection.Insert(0, root); // 插到首部，只解码第一个

        converter ??= CreateConverter();
        // 泛型参数需要为所以节点的超类
        PackageBuilder builder = converter.ReadFromDsonCollection<object>(collection) as PackageBuilder;
        TaskEntry<Blackboard> taskEntry = new TaskEntry<Blackboard>()
        {
            RootTask = builder,
            Blackboard = new Blackboard()
        };
        taskEntry.Update();
    }

    private static int IndexOf(DsonArray<string> collection, long localId) {
        for (int index = 0; index < collection.Count; index++) {
            if (collection[index] is DsonObject<string> dsonObject
                && dsonObject.Header.TryGetValue("localId", out DsonValue boxLocalId)
                && boxLocalId.AsNumber().LongValue == localId) {
                return index;
            }
        }
        return -1;
    }

    /// <summary>
    /// 需要统计行为树和打包工具相关的对象
    /// </summary>
    /// <returns></returns>
    private IDsonConverter CreateConverter() {
        DsonConverterBuilder builder = new DsonConverterBuilder();
        string interfaceName = typeof(IDsonCodec).FullName;
        // 行为树Codec
        foreach (Type type in typeof(BtreeCodecLinker).Assembly.ExportedTypes) {
            TryAddCodec(builder, type, interfaceName);
        }
        // 打包工具Codec
        builder.AddTypeMeta(TypeMeta.Of(typeof(Task<>), "Task"));
        builder.AddTypeMeta(TypeMeta.Of(typeof(Blackboard), "Blackboard"));
        foreach (Type type in typeof(PackageBuilder).Assembly.ExportedTypes) {
            TryAddCodec(builder, type, interfaceName);
        }
        return builder.Build();
    }

    private void TryAddCodec(DsonConverterBuilder builder, Type type, string interfaceName) {
        if (type.Name.EndsWith("Codec") && type.GetInterface(interfaceName!) != null) {
            Type encoderType = GetEncoderType(type);
            // 添加Codec
            if (type.IsGenericType) {
                builder.AddGenericCodec(encoderType, type);
                builder.AddTypeMeta(TypeMeta.Of(encoderType, encoderType.GetGenericTypeDefinition().Name));
            } else {
                builder.AddTypeMeta(TypeMeta.Of(encoderType, encoderType.Name));
                builder.AddCodec((IDsonCodec)Activator.CreateInstance(type)!);
            }
        }
    }

    private static Type GetEncoderType(Type codecType) {
        Type @interface = codecType.GetInterface(typeof(IDsonCodec<>).Name);
        return @interface.GetGenericArguments()[0];
    }
}
}