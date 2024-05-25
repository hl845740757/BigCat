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

import java.io.File;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/7/27
 */
class FileMetadata<T> {

    final FileReader<T> reader;
    final File file;
    FileStat fileStat;

    /** 读取文件的优先级 - 越小越靠前 */
    int priority = -1;
    /** 依赖的所有文件，缓存起来提高效率，在loadAll时初始化 */
    Set<FilePath<?>> allDependents = Set.of();

    FileMetadata(FileReader<T> reader, File file) {
        this.file = file;
        this.reader = reader;
    }

    FilePath<?> getFilePath() {
        return reader.filePath();
    }

}
