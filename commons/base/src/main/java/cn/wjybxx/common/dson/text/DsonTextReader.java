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

package cn.wjybxx.common.dson.text;

import cn.wjybxx.common.dson.*;
import cn.wjybxx.common.dson.io.BinaryUtils;
import cn.wjybxx.common.dson.io.DsonInput;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.Parser;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonTextReader extends AbstractDsonDocReader {

    private DsonInput input;

    public DsonTextReader(DsonInput input, int recursionLimit) {
        super(recursionLimit);
        this.input = input;
        setContext(new Context(null, DsonContextType.TOP_LEVEL));
    }

    @Override
    public void close() {
        super.close();
        if (input != null) {
            input.close();
            input = null;
        }
    }

    @Override
    public Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public Context getPooledContext() {
        return (Context) super.getPooledContext();
    }

    // region state

    @Override
    public boolean isAtEndOfObject() {
        return input.isAtEnd();
    }

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        final int fullType = input.isAtEnd() ? 0 : BinaryUtils.toUint8(input.readRawByte());
        DsonType dsonType = DsonType.forNumber(Dsons.dsonTypeOfFullType(fullType));
        WireType wireType = WireType.forNumber(Dsons.wireTypeOfFullType(fullType));
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentName = INVALID_NAME;

        // topLevel只可是容器对象
        if (context.contextType == DsonContextType.TOP_LEVEL && !dsonType.isContainer()) {
            throw DsonCodecException.invalidDsonType(context.contextType, dsonType);
        }

        if (dsonType == DsonType.END_OF_OBJECT) {
            context.setState(DsonReaderState.WAIT_END_OBJECT);
        } else {
            // name/name总是和type同时解析
            if (context.contextType == DsonContextType.OBJECT) {
                // 如果是header则直接进入VALUE状态 - header匿名属性
                if (dsonType == DsonType.HEADER) {
                    context.setState(DsonReaderState.VALUE);
                } else {
                    context.setState(DsonReaderState.NAME);
                }
            } else {
                context.setState(DsonReaderState.VALUE);
            }
        }
        return dsonType;
    }

    @Override
    protected void doReadName() {
        currentName = Dsons.internField(input.readString());
    }

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        return currentWireType.readInt32(input);
    }

    @Override
    protected long doReadInt64() {
        return currentWireType.readInt64(input);
    }

    @Override
    protected float doReadFloat() {
        return input.readFloat();
    }

    @Override
    protected double doReadDouble() {
        return input.readDouble();
    }

    @Override
    protected boolean doReadBool() {
        return input.readBool();
    }

    @Override
    protected String doReadString() {
        return input.readString();
    }

    @Override
    protected void doReadNull() {

    }

    @Override
    protected DsonBinary doReadBinary() {
        return DsonReaderUtils.readDsonBinary(input);
    }

    @Override
    protected DsonExtString doReadExtString() {
        return DsonReaderUtils.readDsonExtString(input);
    }

    @Override
    protected DsonExtInt32 doReadExtInt32() {
        return DsonReaderUtils.readDsonExtInt32(input, currentWireType);
    }

    @Override
    protected DsonExtInt64 doReadExtInt64() {
        return DsonReaderUtils.readDsonExtInt64(input, currentWireType);
    }

    @Override
    protected ObjectRef doReadRef() {
        return DsonReaderUtils.readObjectRef(input);
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType) {
        Context newContext = newContext(getContext(), contextType);
        int length = input.readFixed32();
        newContext.oldLimit = input.pushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    @Override
    protected void doReadEndContainer() {
        if (!input.isAtEnd()) {
            throw DsonCodecException.bytesRemain(input.getBytesUntilLimit());
        }
        Context context = getContext();
        input.popLimit(context.oldLimit);

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        setContext(context.parent);
        poolContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doSkipName() {
        // 避免构建字符串
        int size = input.readUint32();
        if (size > 0) {
            input.skipRawBytes(size);
        }
    }

    @Override
    protected void doSkipValue() {
        DsonReaderUtils.skipValue(input, getContextType(), currentDsonType, currentWireType);
    }

    @Override
    protected void doSkipToEndOfObject() {
        DsonReaderUtils.skipToEndOfObject(input);
    }

    @Override
    protected <T> T doReadMessage(int binaryType, Parser<T> parser) {
        return DsonReaderUtils.readMessage(input, binaryType, parser);
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        return DsonReaderUtils.readValueAsBytes(input, currentDsonType);
    }

    // endregion

    // region context

    private Context newContext(Context parent, DsonContextType contextType) {
        Context context = getPooledContext();
        if (context != null) {
            setPooledContext(null);
        } else {
            context = new Context();
        }
        context.init(parent, contextType);
        return context;
    }

    private void poolContext(Context context) {
        context.reset();
        setPooledContext(context);
    }

    private static class Context extends AbstractDsonDocReader.Context {

        int oldLimit = -1;

        public Context() {
        }

        public Context(Context parent, DsonContextType contextType) {
            super(parent, contextType);
        }

        public void reset() {
            super.reset();
            oldLimit = -1;
        }
    }

    // endregion

    /** 文件行是指原始的输入行，尚未处理行的合并问题 */
    public static DsonValue parseOfFileLines(List<String> lines) {
        return parseOfFileLines(mergeLines(lines));
    }

    /** 逻辑行是指合并行之后的行 */
    public static DsonValue parseOfLogicLines(List<String> lines) {
        return null;
    }

    /**
     * 解析行首
     * 1. 空白行 和 #开头的行 都认为是注释行，返回 #
     * 2. 如果是约定的内容行行首，则返回行首标识
     * 3. 其它情况下返回null
     */
    private static String parseLhead(final String line) {
        if (line.isBlank() || line.startsWith("#")) {
            return "#"; // 空白行也当做注释行
        }
        // 减少不必要的字符串切割
        int startIndex = 0;
        while (startIndex < line.length() && Character.isWhitespace(line.charAt(startIndex))) {
            startIndex++;
        }
        int endIndex = line.indexOf(' ');
        String lhead;
        if (endIndex == -1) { // 可能没有空格直接换行
            lhead = line.substring(startIndex);
        } else {
            lhead = line.substring(startIndex, endIndex);
        }
        if (DsonTexts.CONTENT_LHEAD_SET.contains(lhead)) {
            return lhead;
        }
        return null;
    }

    private static List<String> mergeLines(List<String> lines) {
        ArrayList<String> mergedLines = new ArrayList<>(lines.size());
        for (String line : lines) {
            line = line.stripLeading(); // 去掉首部空格-大概率返回当前字符串

            String lhead = parseLhead(line);
            if (lhead == null) {
                throw new DsonParseException("invalid line " + line);
            }
            switch (lhead) {
                case DsonTexts.LHEAD_APPEND_LINE, DsonTexts.LHEAD_TEXT_APPEND_LINE -> {
                    // 独立行，字符串段落的行不能提前合并
                    mergedLines.add(line);
                }
                case DsonTexts.LHEAD_APPEND -> {
                    // 合并到前一行
                    mergeToPreline(mergedLines, line, DsonTexts.LHEAD_APPEND.length() + 1);
                }
                default -> throw new DsonParseException("unhandled line head " + lhead);
            }
        }
        return mergedLines;
    }

    private static void mergeToPreline(List<String> mergedLines, String line, int beginIndex) {
        if (beginIndex < line.length()) {
            int lastLineIndex = mergedLines.size() - 1;
            String lastLine = mergedLines.get(lastLineIndex);
            String newLine = lastLine + line.substring(beginIndex);
            mergedLines.set(lastLineIndex, newLine);
        }
    }

    public static void main(String[] args) {
        String x = "-- pos: {@Vector3 x: 0.5, y: 0.5, z: 0.5}\n" +
                "-- posArray: [@Vector3 {x: 0.1, y: 0.1, z: 0.1}, {x: 0.2, y: 0.2, z: 0.2}]\n" +
                "--\n" +
                "--\n" +
                "-- @ss name:\n" +
                "-|     salkjlxaaslkhalkhsal,anxksjah\\n\n" +
                "-| xalsjalkjlkalhjalskhalhslahlsanlkanclxa\n" +
                "-| salkhaslkanlnlkhsjlanx,nalkxanla\n" +
                "-> lsaljsaljsalsaajsal\n" +
                "-> saklhskalhlsajlxlsamlkjalj\n" +
                "-> salkhjsaljsljldjaslna\n" +
                "--";
        List<String> logicLines = mergeLines(x.lines().collect(Collectors.toList()));
        logicLines.forEach(System.out::println);
    }

}