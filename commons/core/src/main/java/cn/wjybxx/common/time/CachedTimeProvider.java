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
 * 该接口表示实现类的缓存了时间戳的，需要外部定时去更新
 * 线程安全性取决于实现类
 *
 * @author wjybxx
 * date 2023/4/4
 */
public interface CachedTimeProvider extends TimeProvider {

    /**
     * @param curTime 最新的时间；部分实现可能不支持回调时间
     */
    void setTime(long curTime);

}