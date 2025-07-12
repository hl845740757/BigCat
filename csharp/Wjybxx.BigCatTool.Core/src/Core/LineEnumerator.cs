#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using System.Collections;
using System.Collections.Generic;

namespace Wjybxx.BigCatTool.Core
{
public class LineEnumerator : IEnumerator<string>
{
#nullable disable
    private readonly IEnumerator<string> _backing;
    private readonly int _initLn;

    private int _currentLn;
    private string _current;
#nullable restore
    public LineEnumerator(IEnumerator<string> backing, int nextLn = 1) {
        _backing = backing ?? throw new ArgumentNullException(nameof(backing));
        _initLn = nextLn;
        _currentLn = nextLn - 1;
    }

    /// <summary>
    /// 当前行号
    /// </summary>
    public int CurrentLn => _currentLn;

    /// <summary>
    /// 当前文本
    /// (允许特殊情况下替换文本)
    /// </summary>
    public string Current {
        get => _current;
        set => _current = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 重置当前文本
    /// </summary>
    public void ResetCurrent() {
        _current = _backing.Current;
    }

    object? IEnumerator.Current => _current;

    public bool MoveNext() {
        if (_backing.MoveNext()) {
            _currentLn++;
            _current = _backing.Current;
            return true;
        }
        return false;
    }

    public void Reset() {
        _backing.Reset();
        _currentLn = _initLn - 1;
        _current = null;
    }

    public void Dispose() {
        _backing.Dispose();
    }
}
}