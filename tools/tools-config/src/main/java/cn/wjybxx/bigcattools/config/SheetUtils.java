/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcattools.config;

/**
 * 将迭代和解析Sheet的代码转移到Utils类。
 * 因为{@link ValueParser}这套读表已不再是必须的。
 *
 * @author wjybxx
 * date 2024/5/25
 */
public class SheetUtils {

    public static NormalSheetReader readNormalSheet(Sheet sheet, ValueParser valueParser) {
        if (sheet.isParamSheet()) {
            throw new IllegalArgumentException("this sheet is a param sheet");
        }
        return new NormalSheetReader(sheet, valueParser);
    }

    public static ParamSheetReader readParamSheet(Sheet sheet, ValueParser valueParser) {
        if (!sheet.isParamSheet()) {
            throw new IllegalArgumentException("this sheet is a normal sheet");
        }
        return new ParamSheetReader(sheet, valueParser);
    }
}