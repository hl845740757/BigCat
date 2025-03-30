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

package cn.wjybxx.bigcat.reload;

import javax.annotation.Nonnull;
import java.util.*;

/**
 * @author wjybxx
 * date - 2023/5/19
 */
public class FileDataProviders {

    /** 不包含数据的Provider */
    public static FileDataProvider empty() {
        return EmptyProvider.INSTANCE;
    }

    /** 只可访问特定文件的Provider */
    public static FileDataProvider limited(FileDataProvider provider, Set<FilePath<?>> filePathSet) {
        Objects.requireNonNull(provider);
        Objects.requireNonNull(filePathSet);
        return new FileDataProviders.LimitedProvider(provider, filePathSet);
    }

    static <T> IllegalArgumentException fileDataAbsent(FilePath<T> filePath) {
        return new IllegalArgumentException("fileData is absent, filePath: " + filePath);
    }

    static class LimitedProvider implements FileDataProvider {

        private final FileDataProvider delegate;
        private final Set<FilePath<?>> filePathSet;
        private Map<FilePath<?>, Object> cacheMap;

        private LimitedProvider(FileDataProvider delegate, Set<FilePath<?>> filePathSet) {
            ensureExist(delegate, filePathSet);
            this.delegate = delegate;
            this.filePathSet = filePathSet;
        }

        private static void ensureExist(FileDataProvider delegate, Set<FilePath<?>> filePathSet) {
            for (FilePath<?> filePath : filePathSet) {
                if (!delegate.contains(filePath)) {
                    throw new IllegalArgumentException("fileData is absent, filePath: " + filePath);
                }
            }
        }

        @Override
        public <T> T get(@Nonnull FilePath<T> filePath) {
            if (filePathSet.contains(filePath)) {
                return delegate.get(filePath);
            }
            throw new IllegalArgumentException("access denied, filePath: " + filePath);
        }

        @Override
        public boolean contains(FilePath<?> filePath) {
            return filePathSet.contains(filePath);
        }

        @Override
        public Map<FilePath<?>, Object> getAll() {
            if (cacheMap == null) {
                Map<FilePath<?>, Object> tempMap = new IdentityHashMap<>(filePathSet.size());
                for (FilePath<?> filePath : filePathSet) {
                    tempMap.put(filePath, delegate.get(filePath));
                }
                cacheMap = Collections.unmodifiableMap(tempMap);
            }
            return cacheMap;
        }

    }

    public static class EmptyProvider implements FileDataProvider {

        private static final EmptyProvider INSTANCE = new EmptyProvider();

        private EmptyProvider() {
        }

        @Override
        public <T> T get(@Nonnull FilePath<T> filePath) {
            throw fileDataAbsent(filePath);
        }

        @Override
        public boolean contains(FilePath<?> filePath) {
            return false;
        }

        @Override
        public Map<FilePath<?>, Object> getAll() {
            return Map.of();
        }
    }

}