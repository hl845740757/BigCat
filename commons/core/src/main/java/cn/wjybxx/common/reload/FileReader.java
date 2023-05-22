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
import java.io.File;
import java.util.Set;

/**
 * 最基础的FileReader，该类型的FileReader读取数据的过程中是无依赖的，
 *
 * <h3>实现约定</h3>
 * 1. reader必须是无状态的，这允许在文件数较多时扩展为多线程读取。
 * 2. 建议作为对应的模板数据类的静态内部类。
 * 3. 需要放指定的包下，这样我们可以通过反射扫描自动加入。
 *
 * @author wjybxx
 * date - 2023/5/19
 */
public interface FileReader<T> {

    /**
     * 关联的文件
     * 1.可以关联到一个虚拟文件（不存在的文件），
     * 虚拟文件可用于构建缓存数据——根据依赖的文件数据，构建缓存数据。
     * 2.如果关联到一个真实文件，则真实文件必须存在，注册reader时会进行校验。
     *
     * @return 关联的文件路径
     */
    @Nonnull
    FilePath<T> filePath();

    /**
     * 依赖的文件
     * 如果返回空集合，则表示无依赖，则会在并发读取阶段执行{@link #read(File, FileDataProvider)}。
     * 如果返回的集合不为空，则表示存在依赖，会在串行读取阶段执行read方法。
     * <p>
     * 建议：不必存储为常量，访问的频率不高，用{@code Set.of}创建即可
     */
    @Nonnull
    default Set<FilePath<?>> dependents() {
        return Set.of();
    }

    /**
     * 读取文件的内容
     * 1.可以在这里完成自身内容的校验，以减少validator类。
     * 2.如果期望延迟解析文件内容，可直接返回File对象 -- 这会丧失一部分安全性，但有时是必要的——通常是因为文件读取后内存占用较大。
     *
     * @param provider 用于获取依赖的文件数据；<b>传入的Provider应当限制只可访问依赖的文件（含递归）</b>，以尽早暴露错误。
     */
    T read(File file, FileDataProvider provider) throws Exception;

}