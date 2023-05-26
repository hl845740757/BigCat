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

/**
 * 用于延迟链接文件之间的数据依赖
 * 1. 在读表期间完成表格之间的数据连接，可提高运行时速度，且业务代码更加干净。
 * 2. 使用延迟链接可提高读表速度，因为减少了必须串行读取的文件，而链接数据是很快的。
 * 3. 使用延迟链接会导致引用是可变的，存在一定程度的不安全性，业务通常应该小心。
 *
 * @author wjybxx
 * date - 2023/5/25
 * @see FileDataRef
 */
public interface FileDataLinker {

    /**
     * @implNote 该实现应该保持幂等性，对于同样的数据，应该总是成功或总是失败
     */
    void link(FileDataMgr fileDataMgr);

}