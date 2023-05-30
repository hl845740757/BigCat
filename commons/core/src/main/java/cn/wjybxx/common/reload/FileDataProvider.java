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

import javax.annotation.Nonnull;
import java.util.Map;

/**
 * 提供简单KV查询方式的文件容器接口
 *
 * @author wjybxx
 * date - 2023/5/19
 */
public interface FileDataProvider {

    /**
     * @param filePath 文件路径
     * @throws IllegalArgumentException 如果请求的文件不可访问，则抛出该异常
     */
    <T> T get(@Nonnull FilePath<T> filePath);

    /** @return 如果包含给定的文件数据则返回true，否则返回false */
    boolean contains(FilePath<?> filePath);

    /** @return 所有的文件数据；返回的Map通常是一个不可修改的Map */
    Map<FilePath<?>, Object> getAll();

}