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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wjybxx.BigCat.Co;
using Wjybxx.BigCat.UI;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Launcher.UI
{
/// <summary>
/// 登录界面控制器
/// </summary>
public class LoginView : UINode
{
    private TMP_InputField _inputAccount;
    private Button _buttonLogin;

    protected override void OnShow(bool firstShow) {
        if (firstShow) {
            _inputAccount = FindElement("Input_Account").GetComponent<TMP_InputField>();
            _buttonLogin = FindElement("Button_Login").GetComponent<Button>();
            _buttonLogin.onClick.AddListener(OnClickLogin);
        }
        base.OnShow(firstShow);
    }

    private void OnClickLogin() {
        string accountText = _inputAccount.text;
        if (ObjectUtil.ContainsWhitespace(accountText) || string.IsNullOrWhiteSpace(accountText)) {
            Debug.Log("account is illegal");
            return;
        }
        Window.CoroutineMgr.StartCoroutine(LoginAsync, new CoroutineStartArgs()
        {
            startArg1 = accountText
        }).Dispose();
        // 未调用Forget的情况下为什么没提示？
        // Window.CoroutineMgr.TimerMgr.ScheduleAction(() => { }, 1);
    }

    private async ValueFuture LoginAsync(CoroutineTaskContext context) {
        float timeBefore = Time.time;
        await context.Sleep(1);

        float timeAfter = Time.time;
        string accountText = (string)context.StartArg1;
        Debug.Log($"Login, timeElapsed: {timeAfter - timeBefore}, account: {accountText}");
    }
}
}