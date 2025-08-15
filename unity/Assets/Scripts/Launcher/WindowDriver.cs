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
using System.Collections.Generic;
using UnityEngine;
using Wjybxx.BigCat.Launcher.UI;
using Wjybxx.BigCat.MVC;
using Wjybxx.BigCat.UI;

namespace Wjybxx.BigCat.Launcher
{
/// <summary>
/// Window驱动示例
/// 
/// 用于编辑器下驱动UI相关的所有管理器，主要指<see cref="WindowMgr"/>
/// (该类仅作为参考)
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class WindowDriver : MonoBehaviour
{
    private WindowMgr windowMgr;

    private void Awake() {
        Canvas canvas = GetComponent<Canvas>();
        windowMgr = new WindowMgr(canvas, null, null);
        WindowMgr.Inst = windowMgr;
        //
        // DontDestroyOnLoad(gameObject);
        // 此时不能查询子节点，延迟到Start -- 其实已经存在
    }

    private void Start() {
        Transform child = transform.Find("LoginWindow");
        if (child) {
            windowMgr.Open("LoginWindow", child.gameObject, new WindowOpenArgs());
        }
    }

    private void Destroy() {
        WindowMgr.Inst = null;
    }

    private void Update() {
        if (windowMgr != null) {
            windowMgr.BeginOfFrame(Time.unscaledTime);
            windowMgr.EarlyUpdate();
            windowMgr.Update();
        }
    }

    private void LateUpdate() {
        if (windowMgr != null) {
            windowMgr.LateUpdate();
            windowMgr.EndOfFrame();
        }
    }
}
}