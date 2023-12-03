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