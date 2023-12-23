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

package cn.wjybxx.common.concurrent;

/**
 * 可分时运行的任务
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface TimeSharingTask<V> {

    /**
     * null可能是一个合理的返回值，因此需要处理。
     * 封装的代价并不高，因为此类任务并不常见，而且只在完成时封装。
     *
     * @return 如果返回值不为null，则表示已完成；返回null表示还需要运行。
     */
    ResultHolder<V> step() throws Exception;

}