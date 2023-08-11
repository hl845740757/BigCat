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

import org.apache.commons.lang3.StringUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date 2023/4/15
 */
public abstract class CellProviderReader {

    private final CellProvider provider;
    private final ValueParser parser;

    protected CellProviderReader(CellProvider provider, ValueParser parser) {
        this.provider = provider;
        this.parser = parser;
    }

    protected CellProvider getProvider() {
        return provider;
    }

    @Nullable
    public SheetCell getCell(String name) {
        return provider.getCell(name);
    }

    @Nonnull
    public SheetCell checkedGetCell(String name) {
        final SheetCell cell = provider.getCell(name);
        if (cell == null) {
            throw new IllegalArgumentException(name + " cell does not exist");
        }
        return cell;
    }
    // region

    public String readAsString(String name) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsString(cell.getType(), cell.getValue());
    }

    public int readAsInt(String name) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsInt(cell.getType(), cell.getValue());
    }

    public long readAsLong(String name) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsLong(cell.getType(), cell.getValue());
    }

    public float readAsFloat(String name) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsFloat(cell.getType(), cell.getValue());
    }

    public double readAsDouble(String name) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsDouble(cell.getType(), cell.getValue());
    }

    public boolean readAsBool(String name) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsBool(cell.getType(), cell.getValue());
    }

    public <T> T readAsArray(String name, @Nonnull Class<T> typeToken) {
        final SheetCell cell = checkedGetCell(name);
        return parser.readAsArray(cell.getType(), cell.getValue(), typeToken);
    }
    // endregion

    // region 默认值

    @Nonnull
    public String readAsString(String name, String defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsString(cell.getType(), cell.getValue());
    }

    public int readAsInt(String name, int defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsInt(cell.getType(), cell.getValue());
    }

    public long readAsLong(String name, long defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsLong(cell.getType(), cell.getValue());
    }

    public float readAsFloat(String name, float defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsFloat(cell.getType(), cell.getValue());
    }

    public double readAsDouble(String name, double defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsDouble(cell.getType(), cell.getValue());
    }

    public boolean readAsBool(String name, boolean defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsBool(cell.getType(), cell.getValue());
    }

    public <T> T readAsArray(String name, @Nonnull Class<T> typeToken, T defaultValue) {
        final SheetCell cell = getCell(name);
        return isBlank(cell) ? defaultValue : parser.readAsArray(cell.getType(), cell.getValue(), typeToken);
    }

    private static boolean isBlank(SheetCell cell) {
        return cell == null || StringUtils.isBlank(cell.getValue());
    }
    // endregion
}