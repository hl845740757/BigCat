using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Wjybxx.Commons.Concurrent;

public class UnityWorkerImpl : AbstractEventLoop
{
    public List<EventLoopModule> modules;

    private void Awake() {
        throw new NotImplementedException();
    }

    // Start is called before the first frame update
    public override IFuture Start()
    {
        return Promise<int>.COMPLETED;
    }

    // Update is called once per frame
    void Update()
    {
        Time.time
    }

    public UnityWorkerImpl([CanBeNull] IEventLoopGroup parent, List<EventLoopModule> moduleList) : base(parent, moduleList) {
    }

    public override void Shutdown() {
        throw new System.NotImplementedException();
    }

    public override List<ITask> ShutdownNow() {
        throw new System.NotImplementedException();
    }

    public override bool InEventLoop() {
        throw new System.NotImplementedException();
    }

    public override bool InEventLoop(Thread thread) {
        throw new System.NotImplementedException();
    }

    public override void Wakeup() {
        throw new System.NotImplementedException();
    }

    public override IFuture RunningFuture { get; }
    public override IFuture TerminationFuture { get; }
    public override EventLoopState State { get; }

    public override void Execute(ITask task) {
        throw new System.NotImplementedException();
    }
}
