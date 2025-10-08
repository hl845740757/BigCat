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
using Wjybxx.BigCatTool.DataScript;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 数据字段
/// </summary>
[Serializable]
public sealed class DataVariable
{
    /// <summary>
    /// 变量的元数据信息
    ///
    /// 1.如果<see cref="DSNamedType"/>，则表示是顶层对象；否则是<see cref="DSField"/>。
    /// 2.如果是泛型类，必须是已构造具体泛型，即泛型参数也是<see cref="DSNamedType"/>
    /// </summary>
    public DSElement defineInfo { get; internal set; }
    /// <summary>
    /// 变量编辑器相关配置
    ///
    /// 注：由<see cref="defineInfo"/>上的注解信息解析得到，为避免频繁查询和类型转换，我们缓存在变量上。
    /// </summary>
    public DataDisplayCfg displayCfg { get; internal set; }
    /// <summary>
    /// 变量的类型
    ///
    /// 注：对于多态字段，该属性会变更。
    /// </summary>
    public DSNamedType type { get; internal set; }

    /// <summary>
    /// 整数类型值(int32、int64、bool)
    /// </summary>
    public long longValue;
    /// <summary>
    /// 浮点数类型值(float、double)
    /// </summary>
    public double doubleValue;
    /// <summary>
    /// 字符串值(string, bytes)
    /// </summary>
    public string stringValue;
    /// <summary>
    /// 如果不是原子类型，则Value按字段存储在List中。
    /// 
    /// 1.对于字典类型，按照[key,value,key,value]的格式存储。
    /// 2.对于Nullable值类型，value会存储在这里。
    /// 3.由框架创建数据结构实例时初始化，可能为null
    /// </summary>
    public List<DataVariable> values;
    /// <summary>
    /// 当前是否是null值
    ///
    /// 注：普通结构体和Nullable支持为null；基础类型不应当赋值null。
    /// </summary>
    public bool isNull { get; set; }

    /// <summary>
    /// 属性绘制器
    /// </summary>
    public DataVariableDrawer drawer { get; internal set; }
    /// <summary>
    /// 关联的端口
    /// </summary>
    public NodePort port { get; internal set; }

    /// <summary>
    /// 是否处于展开状态
    /// </summary>
    public bool isExpanded { get; set; }
    /// <summary>
    /// 编辑器使用的缓存数据，通过该字段可以让Drawer总是保持为无（可变）状态的。
    /// </summary>
    public object editorState { get; set; }

    #region util

    public int intValue {
        get => (int)longValue;
        set => longValue = value;
    }

    public float floatValue {
        get => (float)doubleValue;
        set => doubleValue = value;
    }

    public bool boolValue {
        get => longValue != 0;
        set => longValue = value ? 1 : 0;
    }

    public byte byteValue {
        get => (byte)longValue;
        set => longValue = value;
    }

    public DataVariable FindValue(string name) {
        if (values == null) return null;
        // TODO 支持路径表达式
        foreach (DataVariable variable in values) {
            if (variable.defineInfo.SimpleName == name) return variable;
        }
        return null;
    }

    public T GetEditorState<T>() where T : new() {
        if (editorState == null) {
            editorState = new T();
        }
        return (T)editorState;
    }

    #endregion
}
}