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

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 场景的排序key
/// </summary>
public readonly struct SceneSortKey : IEquatable<SceneSortKey>
{
    public readonly int configId;
    public readonly long instId;

    public SceneSortKey(int configId, long instId) {
        this.configId = configId;
        this.instId = instId;
    }

    public bool Equals(SceneSortKey other) {
        return configId == other.configId && instId == other.instId;
    }

    public override bool Equals(object obj) {
        return obj is SceneSortKey other && Equals(other);
    }

    public override int GetHashCode() {
        return (configId * 397) ^ instId.GetHashCode();
    }

    public override string ToString() {
        return $"{nameof(configId)}: {configId}, {nameof(instId)}: {instId}";
    }

    private sealed class CComparer : IComparer<SceneSortKey>
    {
        public int Compare(SceneSortKey x, SceneSortKey y) {
            int r = x.configId.CompareTo(y.configId);
            return r != 0 ? r : x.instId.CompareTo(y.instId);
        }
    }

    public static IComparer<SceneSortKey> Comparer { get; } = new CComparer();
}
}