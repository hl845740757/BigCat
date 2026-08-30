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

namespace Wjybxx.BigCat.Gameplay
{
public sealed class GTimePiece : ITimeProvider
{
    private int _frameCount;
    private double _deltaTime;
    private double _time;

    public void Restart() {
        this._time = 0;
        this._deltaTime = 0;
        this._frameCount = 0;
    }

    public void Update(double deltaTime) {
        if (deltaTime < 0) deltaTime = 0;
        _frameCount++;
        _deltaTime = deltaTime;
        _time += deltaTime;
    }

    public int FrameCount {
        get => _frameCount;
        set => _frameCount = Math.Max(0, value);
    }

    public double DeltaTime {
        get => _deltaTime;
        set => _deltaTime = Math.Max(0, value);
    }

    public double Time {
        get => _time;
        set => _time = Math.Max(0, value);
    }
}
}