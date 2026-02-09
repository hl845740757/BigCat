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
    /// <summary>
    /// 反序列化工具，允许用户自定义初始化
    /// </summary>
    public IDsonConverter converter;

    [MenuItem("Window/BigCat/PackageBuilder")]
    private static void OpenWindow() {
        BuildWindow wnd = GetWindow<BuildWindow>();
        wnd.titleContent = new GUIContent("BuildWindow");
        //
        string scriptDir = Application.dataPath + "/Editor/DataScripts";
        wnd.InitRepository(Directory.GetFiles(scriptDir, "*.ds", SearchOption.AllDirectories));
    }

    /// <summary>
    /// Ctrl + E 快捷打包
    /// </summary>
    [MenuItem("Window/BigCat/EditorBuild %E")]
    private static void QuickBuild() {
        const string buildConfigPath = "Assets/Editor/DataScripts/PackageBuilder.dson";
        const string buildNodeName = "EditorPacker"; // 编辑器打包节点任务的名字

        string filePath = UnityEditorUtil.ConvertToFilePath(buildConfigPath);
        DsonArray<string> collection = Dsons.FromFlatDson(File.ReadAllText(filePath));
        // 查找执行节点
        int index = DataEditorUtil.IndexOf(collection, buildNodeName);
        if (index < 0) {
            Debug.LogError($"Node {buildNodeName} does not exist");
            return;
        }
        DsonValue root = collection[index];
        collection.RemoveAt(index);
        collection.Insert(0, root); // 插到首部，只解码第一个对象及其引用的对象

        IDsonConverter converter = UnityEditorUtil.Converter;
        PackageBuilder builder = converter.ReadFromDsonCollection<object>(collection) as PackageBuilder;
        TaskEntry<Blackboard> taskEntry = new TaskEntry<Blackboard>()
        {
            RootTask = builder,
            Blackboard = new Blackboard()
        };
        taskEntry.Update();
        //
        if (taskEntry.IsSucceeded) {
            Debug.Log("Build success");
        } else if (taskEntry.IsFailed) {
            BuildErrorCodec errorCodec = (BuildErrorCodec)taskEntry.Status;
            Debug.LogError($"Build failed, ErrorCode: {taskEntry.Status}, desc: {errorCodec}");
        } else {
            Debug.LogError("Task is not completed!");
        }
    }

    public override void OnNodeExecuteRequest(NodeView nodeView) {
        string filePath = UnityEditorUtil.ConvertToFilePath(dataGraph.assetPath);
        DsonArray<string> collection = Dsons.FromFlatDson(File.ReadAllText(filePath));
        // 查找执行节点
        int index = DataEditorUtil.IndexOf(collection, nodeView.dataNode.localId);
        DsonValue root = collection[index];
        collection.RemoveAt(index);
        collection.Insert(0, root); // 插到首部，只解码第一个对象及其引用的对象

        converter ??= UnityEditorUtil.Converter;
        // 泛型参数需要为所有节点的超类
        PackageBuilder builder = converter.ReadFromDsonCollection<object>(collection) as PackageBuilder;
        TaskEntry<Blackboard> taskEntry = new TaskEntry<Blackboard>()
        {
            RootTask = builder,
            Blackboard = new Blackboard()
        };
        taskEntry.Update();
        //
        if (taskEntry.IsSucceeded) {
            Debug.Log("Build success");
        } else if (taskEntry.IsFailed) {
            BuildErrorCodec errorCodec = (BuildErrorCodec)taskEntry.Status;
            Debug.LogError($"Build failed, ErrorCode: {taskEntry.Status}, desc: {errorCodec}");
        } else {
            Debug.LogError("Task is not completed!");
        }
    }
}
}