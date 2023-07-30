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

import javax.annotation.Nonnull;
import java.util.Iterator;

/**
 * 普通表格解析
 *
 * @author wjybxx
 * date 2023/4/15
 */
public class NormalSheetReader implements Iterable<ValueRowReader> {

    private final Sheet sheet;
    private final ValueParser parser;

    public NormalSheetReader(Sheet sheet, ValueParser parser) {
        this.sheet = sheet;
        this.parser = parser;
    }

    @Nonnull
    @Override
    public Iterator<ValueRowReader> iterator() {
        return new Itr(sheet, parser);
    }

    static class Itr implements Iterator<ValueRowReader> {

        final Iterator<SheetRow> delegated;
        final ValueParser parser;

        private Itr(Sheet sheet, ValueParser parser) {
            this.delegated = sheet.getValueRowList().iterator();
            this.parser = parser;
        }

        @Override
        public boolean hasNext() {
            return delegated.hasNext();
        }

        @Override
        public ValueRowReader next() {
            final SheetRow row = delegated.next();
            return new ValueRowReader(row, parser);
        }
    }
}