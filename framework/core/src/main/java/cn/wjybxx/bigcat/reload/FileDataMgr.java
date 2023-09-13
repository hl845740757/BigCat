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

package cn.wjybxx.bigcat.reload;

/**
 * 文件数据管理器 -- 业务用
 *
 * @author wjybxx
 * date - 2023/5/19
 */
public interface FileDataMgr {

    /**
     * 创建一个新的实例，只包含必要的组件即可；新的实例将用于沙盒测试，以确保表格读取过程中的原子性
     */
    FileDataMgr newInstance();

    /**
     * 将KV形式的文件数据赋值到最终的文件数据管理器上。
     * 它的主要目的：将KV形式的数据平铺到对象上，以提高访问性能和提高可读性。
     *
     * @implNote 1.如果存在缓存数据，在赋值前最好先进行清理。
     * 2.这里最好只进行赋值操作，而不进行校验操作，否则会破坏原子性。
     * 3.可以保存provider的引用
     */
    void assignFrom(FileDataProvider provider);
}