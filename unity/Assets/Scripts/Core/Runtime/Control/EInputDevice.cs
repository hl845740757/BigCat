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

using UnityEngine.InputSystem;

namespace Wjybxx.BigCat.Control
{
/// <summary>
/// 输入设备类型
///
/// 注：该枚举是<see cref="InputDevice"/>的粗粒度归类，主要用于驱动UI按键提示图标的切换，
/// 精确的设备信息请直接使用<see cref="InputManager.CurRawDevice"/>。
/// </summary>
public enum EInputDevice : byte
{
    /// <summary>
    /// 未知设备(尚无任何输入)
    /// </summary>
    None = 0,
    /// <summary>
    /// 键盘
    /// </summary>
    Keyboard = 1,
    /// <summary>
    /// 鼠标
    /// </summary>
    Mouse = 2,
    /// <summary>
    /// 游戏手柄(Xbox/DualShock等)
    /// </summary>
    Gamepad = 3,
    /// <summary>
    /// 触屏
    /// </summary>
    Touchscreen = 4,
    /// <summary>
    /// 触控笔
    /// </summary>
    Pen = 5,
    /// <summary>
    /// 摇杆(非标准手柄，如飞行摇杆)
    /// </summary>
    Joystick = 6,
    /// <summary>
    /// 其它设备
    /// </summary>
    Other = 7,
}

/// <summary>
/// <see cref="EInputDevice"/>与<see cref="InputDevice"/>的扩展方法
/// </summary>
public static class InputDeviceExtensions
{
    /// <summary>
    /// 是否是键鼠设备
    ///
    /// 注：UI提示图标通常将键盘和鼠标视为同一套，因此单独提供该方法。
    /// </summary>
    public static bool IsKeyboardMouse(this EInputDevice device) {
        return device == EInputDevice.Keyboard || device == EInputDevice.Mouse;
    }

    /// <summary>
    /// 将<see cref="InputDevice"/>归类为<see cref="EInputDevice"/>
    ///
    /// 注：<see cref="Mouse"/>、<see cref="Touchscreen"/>、<see cref="Pen"/>都继承自<c>Pointer</c>，
    /// 但它们互为兄弟类型，因此匹配顺序无影响。
    /// </summary>
    /// <param name="device">Unity的输入设备，可为null</param>
    public static EInputDevice ToDeviceKind(this InputDevice device) {
        switch (device) {
            case null: return EInputDevice.None;
            case Keyboard _: return EInputDevice.Keyboard;
            case Mouse _: return EInputDevice.Mouse;
            case Gamepad _: return EInputDevice.Gamepad;
            case Touchscreen _: return EInputDevice.Touchscreen;
            case Pen _: return EInputDevice.Pen;
            case Joystick _: return EInputDevice.Joystick;
            default: return EInputDevice.Other;
        }
    }
}
}
