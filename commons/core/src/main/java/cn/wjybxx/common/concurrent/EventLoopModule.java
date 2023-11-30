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

import cn.wjybxx.common.annotation.MarkInterface;

/**
 * 事件循环的模块
 * 1.该接口为标记接口，具体的行为由子接口决定。
 * 2.推荐将事件循环实现为模块化的
 * 3.推荐由Agent驱动所有的模块，而不直接由EventLoop驱动
 *
 * @author wjybxx
 * date - 2023/11/17
 */
@MarkInterface
public interface EventLoopModule {

}
