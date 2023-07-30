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

package cn.wjybxx.common.config;

/**
 * 参数表格解析
 *
 * @author wjybxx
 * date 2023/4/15
 */
public class ParamSheetReader extends CellProviderReader {

    public ParamSheetReader(Sheet sheet, ValueParser parser) {
        super(new SheetAdapter(sheet), parser);
    }

    static class SheetAdapter implements CellProvider {

        final Sheet sheet;

        SheetAdapter(Sheet sheet) {
            this.sheet = sheet;
        }

        @Override
        public SheetCell getCell(String name) {
            return sheet.getParamCell(name);
        }
    }
}