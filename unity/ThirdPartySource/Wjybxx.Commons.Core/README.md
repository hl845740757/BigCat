# Core

Core模块包含一些基础的工具类和注解，这些基础工具和注解被其它所有模块依赖。

## Collections

Core的主要内容是集合库，但凡C#的基础集合库好用一点，我也不至于自己造轮子，实现的集合类：

1. LinkedDictionary 保持插入顺序的字典，并提供大量的有用方法。
2. LinkedHashSet 保持插入顺序的Set，并提供大量的有用方法。
3. IndexedPriorityQueue 含索引的优先级队列，高查询和删除效率。
4. BoundedArrayDeque 基于数组的有界双端队列，允许手动调整容量。
5. MultiChunkDeque 分块无界双端队列。
6. ImmutableLinkedHastSet 保持插入序的不可变HashSet。
7. ImmutableLinkedDictionary 保持插入序的不可变Dictionary。

LinkedDictionary特殊接口示例：

```csharp

    public class LinkedDictionary<TKey,TValue> {
        TKey PeekFirstKey();
        TKey PeekLastKey();
        void AddFirst(TKey key, TValue value);
        void AddLast(TKey key, TValue value);
        KeyValuePair<TKey, TValue> RemoveFirst();
        KeyValuePair<TKey, TValue> RemoveLast();
        TValue GetAndMoveToFirst(TKey key);        
        TValue GetAndMoveToLast(TKey key);
    }
    
```

## ReleaseNotes

### 1.2.1

1. fix LinkedHashSet和LinkedDictionary中`FixPointers`方法head维护错误..
2. fix LinkedHashSet与Unity的兼容问题（IReadOnlySet）

```
    private void FixPointers(int source, int dest) {
        if (_count == 1) {
            _head = _tail = dest;
            return;
        }
        if (_head == source) {
            _head = dest; // 旧时将source赋值给了head...
            ref Node node = ref _table[dest];
            ref Node nextNode = ref _table[node.next];
            nextNode.prev = dest;
        } else if (_tail == source) {
            _tail = dest;
            ref Node node = ref _table[dest];
            ref Node prevNode = ref _table[node.prev];
            prevNode.next = dest;
        }
        // ...
    }
```

### 1.2.0

1. 新增无界数组队列`ArrayDeque`，以方便统一Java和C#代码
2. `LinkedHashSet`类型兼容`ISet`和`IReadOnlySet`接口
3. 重命名`EmptyDequeue` => `EmptyDeque`
4. 优化`MultiChunkDeque`的迭代器