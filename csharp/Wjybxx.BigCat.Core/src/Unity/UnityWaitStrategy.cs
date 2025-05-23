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

using System.Threading;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Unity
{
/// <summary>
/// Unity下的等待策略
/// 1. 先尝试自旋等待一定次数。
/// 2. 然后尝试yield方式自旋一定次数。
/// 3.如果数据仍不可用，返回超时（sequence-1）。
/// </summary>
public class UnityWaitStrategy : WaitStrategy
{
    public static readonly UnityWaitStrategy Inst = new UnityWaitStrategy();

    private readonly int spinTries;
    private readonly int spinIterations;
    private readonly int yieldTries;

    public UnityWaitStrategy() {
        this.spinTries = 10;
        this.spinIterations = 1;
        this.spinTries = 10;
    }

    public UnityWaitStrategy(int spinTries, int spinIterations,
                             int yieldTries) {
        this.spinTries = spinTries;
        this.spinIterations = spinIterations;
        this.yieldTries = yieldTries;
    }

    public long WaitFor(long sequence, ProducerBarrier producerBarrier, ConsumerBarrier barrier) {
        int counter = spinTries + yieldTries;
        int yieldThreshold = yieldTries;

        long availableSequence;
        while ((availableSequence = barrier.DependentSequence()) < sequence) {
            barrier.CheckAlert();

            if (counter > yieldThreshold) {
                --counter;
                Thread.SpinWait(spinIterations);
            } else if (counter > 0) {
                --counter;
                Thread.Yield();
            } else {
                return sequence - 1;
            }
        }
        return availableSequence;
    }
}
}