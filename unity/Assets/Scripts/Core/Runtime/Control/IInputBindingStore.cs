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

using UnityEngine;

namespace Wjybxx.BigCat.Control
{
/// <summary>
/// 按键重绑定的存储接口
///
/// 1.重绑定数据由InputSystem序列化为json（<c>SaveBindingOverridesAsJson</c>），该接口只负责读写。
/// 2.定义为接口，以支持用户存到自己的存档系统（服务器、本地文件、注册表等）；
///   框架默认提供<see cref="PlayerPrefsBindingStore"/>。
/// </summary>
public interface IInputBindingStore
{
    /// <summary>
    /// 读取重绑定数据
    /// </summary>
    /// <param name="key">键，通常是ActionAsset或ActionMap的名字</param>
    /// <returns>不存在时返回null</returns>
    string Load(string key);

    /// <summary>
    /// 写入重绑定数据
    /// </summary>
    /// <param name="key">键，通常是ActionAsset或ActionMap的名字</param>
    /// <param name="json">重绑定数据</param>
    void Save(string key, string json);

    /// <summary>
    /// 删除重绑定数据
    /// </summary>
    /// <param name="key">键，通常是ActionAsset或ActionMap的名字</param>
    void Delete(string key);

    /// <summary>
    /// 提交改动
    ///
    /// 注：批量<see cref="Save"/>后调用一次，便于实现方做落盘等聚合操作。
    /// </summary>
    void Flush();
}

/// <summary>
/// 基于<see cref="PlayerPrefs"/>的重绑定存储
///
/// 注：PlayerPrefs在各平台上都有大小限制（如WebGL的localStorage），按键配置数据量很小，通常够用。
/// </summary>
public sealed class PlayerPrefsBindingStore : IInputBindingStore
{
    /// <summary>
    /// 默认的键前缀
    /// </summary>
    public const string DefaultPrefix = "input.bindings.";

    private readonly string _prefix;

    /// <summary>
    /// </summary>
    /// <param name="prefix">键前缀，用于避免和其它存档键冲突</param>
    public PlayerPrefsBindingStore(string prefix = DefaultPrefix) {
        _prefix = prefix ?? "";
    }

    public string Load(string key) {
        string value = PlayerPrefs.GetString(_prefix + key, "");
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public void Save(string key, string json) {
        PlayerPrefs.SetString(_prefix + key, json ?? "");
    }

    public void Delete(string key) {
        PlayerPrefs.DeleteKey(_prefix + key);
    }

    public void Flush() {
        PlayerPrefs.Save();
    }
}
}
