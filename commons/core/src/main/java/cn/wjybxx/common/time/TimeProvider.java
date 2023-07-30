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

package cn.wjybxx.common.time;

/**
 * 时间提供者，用户外部获取时间
 * 线程安全性取决于具体实现
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface TimeProvider {

    /**
     * 获取当前的时间戳
     * 时间的单位需要自行约定，通常是毫秒
     */
    long getTime();

}