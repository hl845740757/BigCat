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

package cn.wjybxx.common.pool;

/**
 * 可池化的对象
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface PoolableObject {

    /**
     * 重置被缓存对象的状态
     * 对象引用应为空或常量，字段可以设置为默认值。
     * <p>
     * Q: 为什么不使用简单的{@code reset}？
     * A: reset这个名字太通用了，容易造成歧义。<br>
     * 关键问题：reset一般表示恢复到初始状态，但是reset却没有表明这个初始状态是否可用（或者说“初始状态”指的并不是同一个状态）。<br>
     * 在某些问题域里，reset是指恢复到对象的初始可用状态，以便重新运行。<br>
     * 在某些问题域里，reset是指恢复到对象的内存初始状态，需要重新初始化才可以使用。<br>
     * 有时候，这两者会同时出现，那么命名为reset方法就会引发问题。因此选择一个更长更清晰的名字是有意义的。
     */
    void resetPoolable();

}