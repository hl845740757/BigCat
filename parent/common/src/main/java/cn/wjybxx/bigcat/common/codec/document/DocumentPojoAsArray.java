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

package cn.wjybxx.bigcat.common.codec.document;

import cn.wjybxx.bigcat.common.annotation.MarkInterface;

/**
 * 用于标注一个对象在文档型序列化时要写为数组格式
 * <p>
 * Q:为什么不是注解？
 * A：因为要在运行时进行测试，注解的测试效率低。
 *
 * @author wjybxx
 * date 2023/4/4
 */
@MarkInterface
public interface DocumentPojoAsArray {

}