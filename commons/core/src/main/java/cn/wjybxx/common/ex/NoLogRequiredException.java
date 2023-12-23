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

package cn.wjybxx.common.ex;

import cn.wjybxx.common.annotation.MarkInterface;

/**
 * 如果一个异常实现了该接口，那么当其被捕获时，我们并不为其自动记录日志。
 * 用于节省不必要的开销（抓取异常堆栈信息的开销较大）。
 * <p>
 * 注意：实现该接口的异常通常应该禁止填充堆栈，也不应包含额外数据。也就是说，实现该接口的异常通常应该是个单例。
 *
 * @author wjybxx
 * date 2023/4/2
 */
@MarkInterface
public interface NoLogRequiredException {

}