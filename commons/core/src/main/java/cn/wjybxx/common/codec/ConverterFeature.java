/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.document.codecs.MapAsObjectCodec;

/**
 * @author wjybxx
 * date - 2023/6/29
 */
public enum ConverterFeature {

    /**
     * 是否写入对象基础类型字段的默认值
     * 1.数值类型默认值为0
     * 2.bool类型默认值为false
     * <p>
     * 基础值类型需要单独控制，因为有时候我们仅想不输出null，但要输出基础类型字段的默认值 -- 通常是在文本模式下。
     */
    APPEND_DEF(false),
    /**
     * 是否写入对象引用类型字段的默认值(null)
     */
    APPEND_NULL(false),

    /**
     * 是否把Map编码为普通对象
     * 1.只在文档编解码中生效
     * 2.如果要将一个Map结构编码为普通对象，<b>Key的运行时必须和声明类型相同</b>，且只支持String、Integer、Long、EnumLite。
     * 3.即使不开启该选项，用户也可以通过定义字段的writeProxy实现将Map写为普通Object - 可参考{@link MapAsObjectCodec}
     *
     * <h3>Map不是Object</h3>
     * 本质上讲，Map是数组，而不是普通的Object，因为标准的Map是允许复杂key的，因此Map默认应该序列化为数组。但存在两个特殊的场景：
     * 1.与脚本语言通信
     * 脚本语言通常没有静态语言中的字典结构，由object充当，但object不支持复杂的key作为键，通常仅支持数字和字符串作为key。
     * 因此在与脚本语言通信时，要求将Map序列化为简单的object。
     * 2.配置文件读写
     * 配置文件通常是无类型的，因此读取到内存中通常是一个字典结构；程序在输出配置文件时，同样需要将字典结构输出为object结构。
     */
    ENCODE_MAP_AS_OBJECT(false),
    ;

    private final boolean _defaultState;
    private final int _mask;

    ConverterFeature(boolean defaultState) {
        _defaultState = defaultState;
        _mask = (1 << ordinal());
    }

    public boolean enabledByDefault() {
        return _defaultState;
    }

    public int getMask() {
        return _mask;
    }

    public boolean enabledIn(int flags) {
        return (flags & _mask) == _mask;
    }

    public int setEnable(int flags, boolean enable) {
        if (enable) {
            return (flags | _mask);
        } else {
            return (flags & ~_mask);
        }
    }

}