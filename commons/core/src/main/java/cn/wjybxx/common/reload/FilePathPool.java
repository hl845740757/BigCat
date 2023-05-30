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

package cn.wjybxx.common.reload;

import cn.wjybxx.common.ConstantPool;

/**
 * 一个辅助类
 *
 * @author wjybxx
 * date - 2023/5/22
 */
public class FilePathPool {

    private final ConstantPool<FilePath<?>> POOL;

    protected FilePathPool(ConstantPool<FilePath<?>> pool) {
        POOL = pool;
    }

    public static FilePathPool newPool() {
        return new FilePathPool(ConstantPool.newPool());
    }

    @SuppressWarnings("unchecked")
    public <T> FilePath<T> newPath(String path) {
        return (FilePath<T>) POOL.newInstance(FilePath.newBuilder(path));
    }

    @SuppressWarnings("unchecked")
    public <T> FilePath<T> newVirtualPath(String path) {
        return (FilePath<T>) POOL.newInstance(FilePath.newBuilder(path).setVirtual(true));
    }

    @SuppressWarnings("unchecked")
    public <T> FilePath<T> newPath(FilePath.Builder builder) {
        return (FilePath<T>) POOL.newInstance(builder);
    }

    /**
     * @return 如果存在对应的文件名常量，则返回true
     */
    public boolean exists(String path) {
        return POOL.exists(path);
    }

    /**
     * @return 返回常量名关联的常量，若不存在则返回null。
     */
    public FilePath<?> get(String path) {
        return POOL.get(path);
    }

    /**
     * @throws IllegalArgumentException 如果不存在对应的常量
     */
    public FilePath<?> getOrThrow(String path) {
        return POOL.getOrThrow(path);
    }

}