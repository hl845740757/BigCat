/*
 * Copyright 2023 wjybxx
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

import javax.annotation.Nonnull;
import java.util.Collections;
import java.util.IdentityHashMap;
import java.util.Map;
import java.util.Objects;

/**
 * 简单KV结构存储文件数据的容器
 *
 * @author wjybxx
 * date - 2023/5/19
 */
public final class FileDataContainer implements FileDataProvider {

    private final Map<FilePath<?>, Object> fileDataMap;

    public FileDataContainer() {
        fileDataMap = new IdentityHashMap<>(50);
    }

    public FileDataContainer(int dataSize) {
        fileDataMap = new IdentityHashMap<>(dataSize);
    }

    public FileDataContainer(FileDataProvider src) {
        fileDataMap = new IdentityHashMap<>(src.getAll());
    }

    public FileDataContainer clear() {
        fileDataMap.clear();
        return this;
    }

    public FileDataContainer putAll(FileDataProvider src) {
        fileDataMap.putAll(src.getAll());
        return this;
    }

    public <T> FileDataContainer add(FilePath<T> filePath, T fileData) {
        Objects.requireNonNull(filePath, "filePath");
        Objects.requireNonNull(fileData, "fileData");
        if (fileDataMap.containsKey(filePath)) {
            throw new IllegalArgumentException("filePath already exists, path: " + filePath);
        }
        fileDataMap.put(filePath, fileData);
        return this;
    }

    public <T> FileDataContainer put(FilePath<T> filePath, T fileData) {
        Objects.requireNonNull(filePath, "filePath");
        Objects.requireNonNull(fileData, "fileData");
        fileDataMap.put(filePath, fileData);
        return this;
    }

    @SuppressWarnings("unchecked")
    public <T> T remove(FilePath<T> filePath) {
        Objects.requireNonNull(filePath, "filePath");
        return (T) fileDataMap.remove(filePath);
    }

    @Override
    public <T> T get(@Nonnull FilePath<T> filePath) {
        Objects.requireNonNull(filePath, "filePath");
        @SuppressWarnings("unchecked") final T result = (T) fileDataMap.get(filePath);
        if (null == result) {
            throw FileDataProviders.fileDataAbsent(filePath);
        }
        return result;
    }

    @Override
    public boolean contains(FilePath<?> filePath) {
        return fileDataMap.containsKey(filePath);
    }

    @Override
    public Map<FilePath<?>, Object> getAll() {
        return Collections.unmodifiableMap(fileDataMap);
    }

}