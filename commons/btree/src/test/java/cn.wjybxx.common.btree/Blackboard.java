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
package cn.wjybxx.common.btree;

import java.util.HashMap;
import java.util.Map;

/**
 * 测试用例使用的简单黑板
 *
 * @author wjybxx
 * date - 2023/12/3
 */
class Blackboard {

    private final Map<String, Object> map = new HashMap<>(8);

    public boolean containsKey(String key) {
        return map.containsKey(key);
    }

    public Object get(String key) {
        return map.get(key);
    }

    public Object put(String key, Object value) {
        return map.put(key, value);
    }

    public Object remove(String key) {
        return map.remove(key);
    }

    public Object putIfAbsent(String key, Object value) {
        return map.putIfAbsent(key, value);
    }
}