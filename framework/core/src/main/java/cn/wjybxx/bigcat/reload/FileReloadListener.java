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

import java.util.Set;

/**
 * 文件数据变化监听器
 * 项目可以提供一个新的抽象进行适配，以将{@link FileDataMgr}转换为目标类型
 *
 * @author wjybxx
 * date - 2023/5/21
 */
public interface FileReloadListener {

    /**
     * 当关注的任一文件修改时，该方法将被调用。
     *
     * @param fileDataMgr        所有的文件数据
     * @param changedFilePathSet 监听的文件中变化的文件路径集合
     */
    void afterReload(FileDataMgr fileDataMgr, Set<FilePath<?>> changedFilePathSet) throws Exception;

}
