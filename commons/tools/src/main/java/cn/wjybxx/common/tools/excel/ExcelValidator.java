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

package cn.wjybxx.common.tools.excel;

import cn.wjybxx.common.config.Sheet;

import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/10/14
 */
public interface ExcelValidator {

    /**
     * 该方法在主线程调用
     * <p>
     * 1.检查主键是否重复
     * 2.检测数据格式是否正确
     * 3.检查表数据关联的正确性
     */
    void validate(Map<String, Sheet> sheetMap);

}