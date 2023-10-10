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

import javax.annotation.Nullable;
import java.util.function.BiFunction;

/**
 * 表格名解析器
 * 部分项目的表格名可能有特殊规则，eg: Item|物品表，因此需要指定解析方式。
 *
 * @author wjybxx
 * date - 2023/4/16
 */
public interface SheetNameParser extends BiFunction<String, String, String> {

    /**
     * 解析表格的实际名
     *
     * @param fileName  原始文件名
     * @param sheetName 原始表名
     * @return 如果返回null或空字符串，则表示该表格不需要读取
     */
    @Nullable
    @Override
    String apply(String fileName, String sheetName);

}