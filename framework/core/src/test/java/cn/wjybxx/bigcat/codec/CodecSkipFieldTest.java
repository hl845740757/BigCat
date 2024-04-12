/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.codec;

import cn.wjybxx.dson.codec.ClassImpl;
import cn.wjybxx.dson.codec.dson.DsonSerializable;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteSerializable;

import java.util.IdentityHashMap;

/**
 * 测试跳过{@link IdentityHashMap#size()}字段。
 *
 * @author houlei
 * date - 2024/4/12
 */
@DsonSerializable
@DsonLiteSerializable
@ClassImpl(skipFields = "IdentityHashMap.size")
public class CodecSkipFieldTest<K, V> extends IdentityHashMap<K, V> {

}