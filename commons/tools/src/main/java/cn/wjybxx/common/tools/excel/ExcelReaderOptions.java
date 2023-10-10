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

import cn.wjybxx.common.FunctionUtils;
import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.config.DefaultValueParser;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.Immutable;
import java.util.Objects;
import java.util.Set;
import java.util.function.BiFunction;
import java.util.function.Predicate;

/**
 * 读表参数
 *
 * @author wjybxx
 * date - 2023/4/16
 */
@Immutable
public class ExcelReaderOptions {

    public final static ExcelReaderOptions DEFAULT = ExcelReaderOptions.newBuilder().build();

    public final Set<String> supportedTypes;
    public final int bufferSize;
    public final SheetNameParser sheetNameParser;
    public final Predicate<String> sheetNameFilter;
    public final int skipRows;
    public final Mode mode;

    private ExcelReaderOptions(Builder builder) {
        this.supportedTypes = Set.copyOf(builder.supportedTypes);
        this.bufferSize = Math.max(8 * 1024, builder.bufferSize);
        this.sheetNameParser = ObjectUtils.nullToDef(builder.sheetNameParser, ((fileName, sheetName) -> sheetName));
        this.sheetNameFilter = ObjectUtils.nullToDef(builder.sheetNameFilter, FunctionUtils.alwaysTrue());
        this.skipRows = builder.skipRows;
        this.mode = ObjectUtils.nullToDef(builder.mode, Mode.BOTH);
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private int bufferSize = 64 * 1024;
        private Set<String> supportedTypes = DefaultValueParser.SUPPORTED_TYPES;
        private SheetNameParser sheetNameParser;
        private Predicate<String> sheetNameFilter;
        private int skipRows = 10;
        private Mode mode = Mode.BOTH;

        public Builder() {
        }

        public int getBufferSize() {
            return bufferSize;
        }

        public Builder setBufferSize(int bufferSize) {
            this.bufferSize = bufferSize;
            return this;
        }

        public Set<String> getSupportedTypes() {
            return supportedTypes;
        }

        /**
         * 支持的自定义字段类型 {int32, int64...}
         * 读取表格时，会对类型字段进行校验
         */
        public Builder setSupportedTypes(@Nonnull Set<String> supportedTypes) {
            this.supportedTypes = Objects.requireNonNull(supportedTypes);
            return this;
        }

        public BiFunction<String, String, String> getSheetNameParser() {
            return sheetNameParser;
        }

        /**
         * 表格名解析器
         * 部分项目的表格名可能有特殊规则，eg: Item|物品表，因此需要指定解析方式
         * 1.如果未指定，则使用原始表格名
         * 2.最好是不可变或无状态的，以支持并发读表
         */
        public Builder setSheetNameParser(SheetNameParser sheetNameParser) {
            this.sheetNameParser = sheetNameParser;
            return this;
        }

        public Predicate<String> getSheetNameFilter() {
            return sheetNameFilter;
        }

        /**
         * 表格名过滤条件
         * 用于用户读取特定表格或跳过部分表格
         * 1.如果未指定，则默认不过滤 -- 括默认的过滤规则仍然生效
         * 2.最好是不可变或无状态的，以支持并发读表
         */
        public Builder setSheetNameFilter(Predicate<String> sheetNameFilter) {
            this.sheetNameFilter = sheetNameFilter;
            return this;
        }

        public int getSkipRows() {
            return skipRows;
        }

        /**
         * 设置每张表跳过的行数，即：前X行作为注释行
         */
        public Builder setSkipRows(int skipRows) {
            this.skipRows = skipRows;
            return this;
        }

        public Mode getMode() {
            return mode;
        }

        public Builder setMode(Mode mode) {
            this.mode = Objects.requireNonNull(mode);
            return this;
        }

        public ExcelReaderOptions build() {
            return new ExcelReaderOptions(this);
        }
    }

}