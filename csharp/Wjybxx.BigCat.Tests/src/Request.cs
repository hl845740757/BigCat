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
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Attributes;

namespace Commons.Tests;

[DsonSerializable]
public class Request
{
#nullable disable
    private int val1;
    private int val2;
    private string string1;
    private string string2;
    private List<string> stringList = new();

    public static Request OfString(string val) {
        return new Request()
        {
            string1 = val
        };
    }

    public static Request OfInt(int val) {
        return new Request()
        {
            val1 = val
        };
    }

    public int Val1 {
        get => val1;
        set => val1 = value;
    }
    public int Val2 {
        get => val2;
        set => val2 = value;
    }
    public string String1 {
        get => string1;
        set => string1 = value;
    }
    public string String2 {
        get => string2;
        set => string2 = value;
    }
    public List<string> StringList {
        get => stringList;
        set => stringList = value;
    }

    public override string ToString() {
        return $"{nameof(val1)}: {val1}," +
               $" {nameof(val2)}: {val2}," +
               $" {nameof(string1)}: {string1}," +
               $" {nameof(string2)}: {string2}," +
               $" {nameof(stringList)}: {CollectionUtil.ToString(stringList)}";
    }
}