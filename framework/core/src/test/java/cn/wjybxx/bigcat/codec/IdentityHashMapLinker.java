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

import cn.wjybxx.dson.codec.CodecLinkerBean;
import cn.wjybxx.dson.codec.ConverterOptions;
import cn.wjybxx.dson.codec.FieldImpl;
import cn.wjybxx.dson.codec.dson.DsonObjectReader;
import cn.wjybxx.dson.codec.dson.DsonObjectWriter;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectReader;
import cn.wjybxx.dson.codec.dsonlite.DsonLiteObjectWriter;

import java.util.IdentityHashMap;

/**
 * @author houlei
 * date - 2024/4/16
 */
@CodecLinkerBean(value = IdentityHashMap.class)
public class IdentityHashMapLinker {

    @FieldImpl(readProxy = "readSize", writeProxy = "writeSize")
    private IdentityHashMap<?, ?> size;

    public static void writeSize(IdentityHashMap<?, ?> inst, DsonObjectWriter writer, String name) {

    }

    public static void writeSize(IdentityHashMap<?, ?> inst, DsonLiteObjectWriter writer, int name) {

    }

    public static void readSize(IdentityHashMap<?, ?> inst, DsonObjectReader reader, String name) {

    }

    public static void readSize(IdentityHashMap<?, ?> inst, DsonLiteObjectReader reader, int name) {

    }

    public static void afterDecode(IdentityHashMap<?, ?> inst, ConverterOptions options) {

    }
}
