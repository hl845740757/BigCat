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

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wjybxx.Commons;
using Wjybxx.Commons.IO;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Editor下的模拟Bundle
///
/// 注：
/// 1.应当走正常打包规则计算Bundle信息，保证Editor模式和运行时一致。
/// 2.assetPath无法恢复为原始文件路径，只能在不区分文件大小写的操作系统运行，Windows/MacOS安全。
/// </summary>
public class EditorAssetBundle : IAssetBundle
{
    private readonly AssetBundleInfo _bundleInfo;
    private readonly TaskScheduler _scheduler;
    private readonly List<FileItem> _fileItemList = new List<FileItem>(10);
    private readonly Dictionary<string, FileItem> _fileItemDic = new(20);
    private Action<EditorAssetBundle> _unloadCallback;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bundleInfo">bundle信息</param>
    /// <param name="scheduler">任务调度器</param>
    public EditorAssetBundle(AssetBundleInfo bundleInfo, TaskScheduler scheduler) {
        _bundleInfo = bundleInfo;
        _scheduler = scheduler;
        if (bundleInfo.bundleType == EBundleType.RawFileBundle) {
            foreach (AssetFileInfo assetInfo in bundleInfo.mainAssets) {
                string filePath = Application.dataPath + assetInfo.assetPath.Substring(6);
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) {
                    throw new FileNotFoundException(filePath);
                }
                FileItem item = new FileItem(assetInfo.assetPath, fileInfo);
                _fileItemList.Add(item);
                _fileItemDic.Add(item.assetPath, item); // 全路径索引，禁止重复
                _fileItemDic[item.fileName] = item; // 文件名索引，允许重复
            }
        }
    }

    public AssetBundleInfo BundleInfo => _bundleInfo;
    /// <summary>
    /// 卸载回调(用于解除对BundleManager的依赖)
    /// </summary>
    public Action<EditorAssetBundle> UnloadCallback {
        get => _unloadCallback;
        set => _unloadCallback = value;
    }

    public void UnloadBundle(bool unloadAllLoadedObjects) {
        _fileItemList.Clear();
        _fileItemDic.Clear();
        _unloadCallback?.Invoke(this);
    }

    public ResourceTask LoadAssetAsync(string assetPath, Type assetType) {
        if (_bundleInfo.bundleType != EBundleType.AssetBundle) {
            return null;
        }
        AssetLoadTask task = new AssetLoadTask(this, assetPath, assetType, ELoadMethod.LoadAsset);
        _scheduler.AddChild(task);
        return task;
    }

    public ResourceTask LoadAssetWithSubAssetsAsync(string assetPath, Type assetType) {
        if (_bundleInfo.bundleType != EBundleType.AssetBundle) {
            return null;
        }
        AssetLoadTask task = new AssetLoadTask(this, assetPath, assetType, ELoadMethod.LoadAssetWithSubAssets);
        _scheduler.AddChild(task);
        return task;
    }

    public ResourceTask LoadAllAssetsAsync(Type assetType) {
        if (_bundleInfo.bundleType != EBundleType.AssetBundle) {
            return null;
        }
        AssetLoadTask task = new AssetLoadTask(this, null, assetType, ELoadMethod.LoadAssetWithSubAssets);
        _scheduler.AddChild(task);
        return task;
    }

    public BinaryAsset LoadBinaryAsset(string assetPath) {
        if (_bundleInfo.bundleType != EBundleType.RawFileBundle) {
            return null;
        }
        _fileItemDic.TryGetValue(assetPath, out FileItem item);
        return item;
    }

    public IReadOnlyList<BinaryAsset> LoadAllBinaryAssets() {
        if (_bundleInfo.bundleType != EBundleType.RawFileBundle) {
            return null;
        }
        return _fileItemList;
    }

    #region load

    private Object LoadAsset(string assetPath, Type assetType) {
        return AssetDatabase.LoadAssetAtPath(assetPath, assetType);
    }

    private IReadOnlyList<Object> LoadAssetWithSubAssets(string assetPath, Type assetType) {
        Object[] assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assetType == typeof(Object)) {
            return assetsAtPath;
        }
        // 编辑器不支持assetType参数，我们手动筛选...
        List<Object> result = new List<Object>(assetsAtPath.Length);
        foreach (Object asset in assetsAtPath) {
            if (assetType.IsAssignableFrom(asset.GetType())) {
                result.Add(asset);
            }
        }
        return result;
    }

    private IReadOnlyList<Object> LoadAllAssets(Type assetType) {
        List<Object> result = new List<Object>(_bundleInfo.mainAssets.Count);
        foreach (AssetFileInfo fileInfo in _bundleInfo.mainAssets) {
            Object asset = AssetDatabase.LoadAssetAtPath(fileInfo.assetPath, assetType);
            if (asset != null) {
                result.Add(asset);
            }
        }
        return result;
    }

    private class AssetLoadTask : ResourceTask
    {
        private readonly EditorAssetBundle _bundle;
        private readonly string _assetPath;
        private readonly Type _assetType;
        private readonly ELoadMethod _loadMethod;

        public AssetLoadTask(EditorAssetBundle bundle, string assetPath,
                             Type assetType, ELoadMethod loadMethod) {
            _bundle = bundle;
            _assetType = assetType ?? typeof(UnityEngine.Object);
            _loadMethod = loadMethod;
            _assetPath = assetPath;
        }

        protected override void Execute() {
            promise.result = _loadMethod switch
            {
                ELoadMethod.LoadAsset => _bundle.LoadAsset(_assetPath, _assetType),
                ELoadMethod.LoadAssetWithSubAssets => _bundle.LoadAssetWithSubAssets(_assetPath, _assetType),
                ELoadMethod.LoadAllAssets => _bundle.LoadAllAssets(_assetType),
                _ => throw new AssertionError()
            };
            SetSuccess();
        }
    }

    #endregion

    private class FileItem : BinaryAsset
    {
        private readonly FileInfo _fileInfo;

        public FileItem(string assetPath, FileInfo fileInfo)
            : base(assetPath) {
            this._fileInfo = fileInfo;
        }

        public override int dataLength => (int)_fileInfo.Length;

        public override void GetData(byte[] buffer, int offset) {
            ByteBufferUtil.CheckBuffer(buffer, offset, dataLength);
            using (FileStream fileStream = _fileInfo.OpenRead()) {
                _ = fileStream.Read(buffer, offset, dataLength);
            }
        }
    }
}
#endif
}