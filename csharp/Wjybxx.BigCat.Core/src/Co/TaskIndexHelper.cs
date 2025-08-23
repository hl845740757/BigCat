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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.Co
{
internal sealed class TaskIndexHelper : IIndexedElementHelper<PromiseTask>
{
    private readonly int queueId;

    private TaskIndexHelper(int queueId) {
        this.queueId = queueId;
    }

    public int CollectionIndex(object collection, PromiseTask element) {
        return element.qIndex;
    }

    public void CollectionIndex(object collection, PromiseTask element, int index) {
        if (index >= 0) {
            element.qIndex = index;
            element.queueId = queueId;
        } else {
            element.qIndex = -1;
            element.queueId = -1;
        }
    }

    private static readonly TaskIndexHelper[] CACHE = new TaskIndexHelper[30];

    static TaskIndexHelper() {
        for (int index = 0; index < CACHE.Length; index++) {
            CACHE[index] = new TaskIndexHelper(index);
        }
    }

    public static TaskIndexHelper GetInst(int queueId) {
        if (queueId < 0 || queueId >= CACHE.Length) {
            throw new ArgumentException("queueId: " + queueId);
        }
        return CACHE[queueId];
    }
}
}