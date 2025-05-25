using System;
using System.Collections.Generic;
using UnityEngine;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCat.Unity;

namespace Wjybxx.BigCat.GamePlay
{
/// <summary>
/// 
/// </summary>
public class GameLauncher : MonoBehaviour
{

    /// <summary>
    /// 挂载的模块
    /// 由于不能直接配置Type，因此我们配置Type的全限定名
    /// </summary>
    public List<string> moduleClasses = new ();

    [NonSerialized] private Node _node;
    [NonSerialized] private UnityWorker _worker;
    
    private void Awake() {
        var type = typeof(LoginService).Assembly.GetType(typeof(LoginService).FullName + "Proxy");
        if (type == null) {
            throw new InvalidOperationException();
        }
    }
}
}