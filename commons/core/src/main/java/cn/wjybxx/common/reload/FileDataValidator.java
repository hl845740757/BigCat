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
 * 文件数据验证器
 * 通常用于校验文件数据之间的一致性
 * <p>
 * Q: 它的调用时机？
 * A: 在调用{@link FileDataMgr#assignFrom(FileDataProvider)}之后。4
 *
 * @author wjybxx
 * date - 2023/5/19
 */
public interface FileDataValidator {

    void validate(FileDataMgr fileDataMgr);

}