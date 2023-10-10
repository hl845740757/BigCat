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

package cn.wjybxx.common.tools.util;

import org.apache.commons.io.IOUtils;

import java.io.BufferedReader;
import java.io.Closeable;
import java.io.IOException;
import java.io.Reader;
import java.util.Iterator;
import java.util.NoSuchElementException;
import java.util.Objects;

/**
 * 修改自{@link org.apache.commons.io.LineIterator}
 *
 * @author wjybxx
 * date - 2023/10/8
 */
public class LineIterator implements Iterator<Line>, Closeable {

    private final BufferedReader bufferedReader;

    /** 缓存行 */
    private Line cachedLine;
    /** 下一行的行号 */
    private int nextLn;
    /** 是否已完成读取 */
    private boolean finished;

    public LineIterator(final Reader reader) {
        this(reader, 1);
    }

    public LineIterator(final Reader reader, int nextLn) {
        if (nextLn < 0) throw new IllegalArgumentException("nextLn cant be negative");
        this.nextLn = nextLn;

        Objects.requireNonNull(reader, "reader");
        if (reader instanceof BufferedReader) {
            bufferedReader = (BufferedReader) reader;
        } else {
            bufferedReader = new BufferedReader(reader);
        }
    }

    @Override
    public void close() throws IOException {
        finished = true;
        cachedLine = null;
        IOUtils.close(bufferedReader);
    }

    @Override
    public boolean hasNext() {
        if (cachedLine != null) {
            return true;
        }
        if (finished) {
            return false;
        }
        try {
            while (true) {
                final String lineData = bufferedReader.readLine();
                if (lineData == null) {
                    finished = true;
                    return false;
                }
                Line line = new Line(nextLn++, lineData);
                if (isValidLine(line)) {
                    cachedLine = line;
                    return true;
                }
            }
        } catch (final IOException ioe) {
            IOUtils.closeQuietly(this, ioe::addSuppressed);
            throw new IllegalStateException(ioe);
        }
    }

    protected boolean isValidLine(Line line) {
        return true;
    }

    @Override
    public Line next() {
        if (!hasNext()) {
            throw new NoSuchElementException("No more lines");
        }
        final Line currentLine = cachedLine;
        cachedLine = null;
        return currentLine;
    }

}