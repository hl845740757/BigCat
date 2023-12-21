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

package cn.wjybxx.common.tools.protobuf;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.tools.util.Line;
import cn.wjybxx.common.tools.util.LineIterator;
import cn.wjybxx.common.tools.util.Utils;
import cn.wjybxx.dson.DsonObject;
import cn.wjybxx.dson.DsonValue;
import it.unimi.dsi.fastutil.ints.IntOpenHashSet;
import it.unimi.dsi.fastutil.ints.IntSet;
import org.apache.commons.io.FilenameUtils;
import org.apache.commons.io.IOUtils;
import org.apache.commons.lang3.StringUtils;

import javax.annotation.Nullable;
import java.io.File;
import java.io.FileReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

/**
 * 实际上我们并不会完全解析protobuf文件，因为没有必要；
 * 我们的预处理只为了生成Rpc服务接口，因此只会解析必要的部分，其它内容按行导出到中间文件，进行编译。
 *
 * @author wjybxx
 * date - 2023/10/8
 */
public class PBParser {

    private final File file;
    private final PBParserOptions options;

    /** 当前递归深度 */
    private int recursionDepth;
    /** 当前衫修改为 */
    private Context context;

    private LineIterator lineIterator;
    /** 用于debug和生成异常信息 */
    private Line curLine;

    /** 处理后的行数据 */
    private final List<Line> processedLines = new ArrayList<>(100);
    /** 方便随时获取 */
    private final PBFile pbFile;

    public PBParser(File file, PBParserOptions options) {
        this.file = file;
        this.options = options;

        this.pbFile = new PBFile();
        this.context = new Context(null, ContextType.FILE, pbFile);
        this.context.started = true;
    }

    /** 不包含Option、import、syntax声明 */
    public List<Line> getProcessedLines() {
        return processedLines;
    }

    public PBFile getPbFile() {
        return pbFile;
    }

    public PBFile parse() {
        PBFile pbFile = context.asFile();
        pbFile.setSyntax(options.getSyntax()); // 默认语法

        // 处理文件名
        String fileName = file.getName();
        pbFile.setFileName(fileName);
        pbFile.setSimpleName(FilenameUtils.removeExtension(fileName));

        // 处理导入
        addCommonImports(pbFile);

        // 处理option
        options.getDefOptions().forEach(pbFile::addOption);

        try {
            final LineIterator lineIterator = new LineIterator(new FileReader(file, StandardCharsets.UTF_8));
            this.lineIterator = lineIterator;
            while (lineIterator.hasNext()) {
                Line line = lineIterator.next();
                this.curLine = line;
                this.processedLines.add(line);

                LineInfo lineInfo = parseLine(line);
                switch (context.contextType) {
                    case FILE -> topLevelReadLine(lineInfo);
                    case SERVICE -> serviceReadLine(lineInfo);
                    case MESSAGE -> messageReadLine(lineInfo);
                    case ENUM -> enumReadLine(lineInfo);
                }
            }
        } catch (Exception e) {
            if (e instanceof PBParserException parserException) {
                throw parserException;
            } else {
                throw new PBParserException("line: " + curLine, e);
            }
        } finally {
            IOUtils.closeQuietly(lineIterator, Exception::printStackTrace);
            this.lineIterator = null;
            this.curLine = null;
        }
        if (context.contextType != ContextType.FILE) {
            throw new PBParserException("eof");
        }
        return pbFile;
    }

    private void addCommonImports(PBFile pbFile) {
        String simpleName = pbFile.getSimpleName();
        if (options.getRoot() == null || simpleName.equals(options.getRoot())) {
            return;
        }
        if (options.getCommons().contains(simpleName)) { // 公共文件只导入root
            pbFile.addImport(options.getRoot() + ".proto");
        } else {
            for (String common : options.getCommons()) { // 其它文件导入所有公共文件
                pbFile.addImport(common + ".proto");
            }
        }
    }

    // region 容器

    private void topLevelReadLine(final LineInfo lineInfo) {
        if (lineInfo.isCommentLine()) { // 累积注释行
            cumulativeCommentLine(lineInfo);
            return;
        }
        // 顶层元素
        switch (lineInfo.keyword) {
            case PBKeywords.SERVICE -> {
                readStartContainer(ContextType.SERVICE, lineInfo);
                processServiceAnnotations(context.asService());
            }
            case PBKeywords.MESSAGE -> readStartContainer(ContextType.MESSAGE, lineInfo);
            case PBKeywords.ENUM -> readStartContainer(ContextType.ENUM, lineInfo);

            case PBKeywords.OPTION -> {
                context.clearCommentLines();
                parseOption(context.container, lineInfo);
                processedLines.removeLast(); // 删除原始行，由pbFile内信息覆盖
            }
            case PBKeywords.IMPORT -> {
                context.clearCommentLines();
                context.asFile().addImport(parseQuoteValue(lineInfo));
                processedLines.removeLast(); // 删除原始行，由pbFile内信息覆盖
            }
            case PBKeywords.SYNTAX -> {
                context.clearCommentLines();
                context.asFile().setSyntax(parseQuoteValue(lineInfo));
                processedLines.removeLast(); // 删除原始行，由pbFile内信息覆盖
            }

            case null, default -> context.clearCommentLines();
        }
    }

    /**
     * 暂无需详细解析Message
     */
    private void messageReadLine(final LineInfo lineInfo) throws PBParserException {
        if (!context.started) {
            checkStart(lineInfo);
            return;
        }
        if (lineInfo.isCommentLine()) { // 累积注释行
            cumulativeCommentLine(lineInfo);
            return;
        }
        // 处理嵌套，消息内不可嵌套服务
        switch (lineInfo.keyword) {
            case PBKeywords.SERVICE -> {
                throw new PBParserException("services cannot be nested within message, line: " + lineInfo);
            }
            case PBKeywords.MESSAGE -> {
                readStartContainer(ContextType.MESSAGE, lineInfo);
                return;
            }
            case PBKeywords.ENUM -> {
                readStartContainer(ContextType.ENUM, lineInfo);
                return;
            }
            case PBKeywords.OPTION -> {
                context.clearCommentLines();
                parseOption(context.container, lineInfo);
                return;
            }
            case PBKeywords.RESERVED -> {
                context.clearCommentLines();
                return;
            }
            case null -> {
                context.clearCommentLines(); // 可能是结束行，不返回
            }
            default -> { // 字段
                PBField field = parseField(context.popCommentLines(), lineInfo);
                context.container.addEnclosedElement(field);
                return;
            }
        }
        // 结束符
        if (checkEnd(lineInfo)) {
            PBMessage message = context.asMessage();
            readEndContainer(lineInfo);

            if (options.getMessageInterceptor() != null) {
                options.getMessageInterceptor().accept(message);
            }
        }
    }

    /**
     * Service
     */
    private void serviceReadLine(final LineInfo lineInfo) throws PBParserException {
        if (!context.started) {
            checkStart(lineInfo);
            return;
        }
        if (lineInfo.isCommentLine()) { // 累积注释行
            cumulativeCommentLine(lineInfo);
            return;
        }
        // Service内部只支持rpc方法定义
        switch (lineInfo.keyword) {
            case PBKeywords.SERVICE, PBKeywords.MESSAGE, PBKeywords.ENUM -> {
                throw new PBParserException("No structures can be nested within enum, line: " + lineInfo);
            }
            case PBKeywords.RPC -> {
                PBMethod method = parseMethod(context.popCommentLines(), lineInfo);
                context.container.addEnclosedElement(method);
                return;
            }
            case PBKeywords.OPTION -> {
                context.clearCommentLines();
                parseOption(context.container, lineInfo);
                return;
            }
            case null -> {
                context.clearCommentLines(); // 可能是结束行，不返回
            }
            default -> {
                context.clearCommentLines();
                return;
            }
        }
        // 结束符
        if (checkEnd(lineInfo)) {
            PBService service = context.asService();
            readEndContainer(lineInfo);

            if (options.getServiceInterceptor() != null) {
                options.getServiceInterceptor().accept(service);
            }
            // 注释掉service相关行
            commentServiceLines(service);
            checkMethodIds(service);
        }
    }

    /**
     * 暂无需详细解析Enum
     */
    private void enumReadLine(final LineInfo lineInfo) throws PBParserException {
        if (!context.started) {
            checkStart(lineInfo);
            return;
        }
        if (lineInfo.isCommentLine()) { // 累积注释行
            cumulativeCommentLine(lineInfo);
            return;
        }
        // enum内不可以嵌套
        switch (lineInfo.keyword) {
            case PBKeywords.SERVICE, PBKeywords.MESSAGE, PBKeywords.ENUM -> {
                throw new PBParserException("No structures can be nested within enum, line: " + lineInfo);
            }
            case PBKeywords.OPTION -> {
                context.clearCommentLines();
                parseOption(context.container, lineInfo);
                return;
            }
            case PBKeywords.RESERVED -> {
                context.clearCommentLines();
                return;
            }
            case null -> {
                context.clearCommentLines(); // 可能是结束行，不返回
            }
            default -> {
                PBEnumValue enumValue = parseEnumValue(context.popCommentLines(), lineInfo);
                context.container.addEnclosedElement(enumValue);
                return;
            }
        }
        // 结束符
        if (checkEnd(lineInfo)) {
            readEndContainer(lineInfo);
        }
    }

    private void readStartContainer(ContextType contextType, LineInfo lineInfo) {
        if (recursionDepth > 32) throw new IllegalStateException("proto had too many levels of nesting");

        Context parent = this.context;
        Context context;
        switch (contextType) {
            case SERVICE -> context = new Context(parent, ContextType.SERVICE, new PBService());
            case MESSAGE -> context = new Context(parent, ContextType.MESSAGE, new PBMessage());
            case ENUM -> context = new Context(parent, ContextType.ENUM, new PBEnum());
            default -> throw new AssertionError();
        }
        context.container.setSourceLine(lineInfo.asLine());
        context.container.setSimpleName(parseContainerName(lineInfo));
        context.started = lineInfo.content.indexOf('{') >= 0;

        parent.container.addEnclosedElement(context.container);
        drainCommentLine(parent.popCommentLines(), context.container);
        if (lineInfo.hasComment()) {
            context.container.addComment(lineInfo.comment);
        }

        recursionDepth++;
        this.context = context;
    }

    private void readEndContainer(LineInfo lineInfo) {
        if (context.parent == null || !context.started) {
            throw new IllegalStateException();
        }
        context.container.setSourceEndLine(lineInfo.asLine());

        recursionDepth--;
        this.context = context.parent;
    }

    /** 解析容器的名字 */
    private static String parseContainerName(LineInfo lineInfo) {
        String content = lineInfo.content;
        int startIdx = content.indexOf(' ');
        int endIdx = content.indexOf('{');
        assert startIdx > 0 && (endIdx < 0 || endIdx > startIdx) : String.format("startIdx: %d, endIdx: %d", startIdx, endIdx);

        String name;
        if (endIdx > 0) {
            name = content.substring(startIdx, endIdx).trim();
        } else {
            name = content.substring(startIdx).trim();
        }
        return name;
    }

    private void checkStart(LineInfo lineInfo) throws PBParserException {
        // '{' 之前最多只能是空行
        if (lineInfo.isEmptyLine()) {
            return;
        }
        if (!isStartWith(lineInfo, '{')) {
            throw new PBParserException("'{' is required, line: " + lineInfo);
        }
        context.started = true;
    }

    private boolean checkEnd(LineInfo lineInfo) {
        return isStartWith(lineInfo, '}');
    }

    // endregion

    // region file

    private void parseOption(PBElement element, LineInfo lineInfo) {
        final String content = lineInfo.content;
        ensureEndWithSemicolon(lineInfo, content);
        int startIdx = content.indexOf(' '); // 跳过 'option'
        int eqIdx = content.indexOf('=');
        int endIdx = content.lastIndexOf(';');

        String name = content.substring(startIdx + 1, eqIdx).trim();
        String value = content.substring(eqIdx + 1, endIdx).trim();
        element.addOption(name, value);
    }
    // endregion

    // region service

    /** 检查方法id重复等 */
    private void checkMethodIds(PBService service) {
        List<PBMethod> methods = service.getMethods();
        IntSet methodIdSet = new IntOpenHashSet(methods.size());
        for (PBMethod method : methods) {
            if (methodIdSet.add(method.getMethodId())) {
                continue;
            }
            throw new PBParserException("methodId is duplicate, serviceName: %s, methodName: %s, methodId: %d"
                    .formatted(service.getServiceId(), method.getSimpleName(), method.getMethodId()));
        }
    }

    private void commentServiceLines(PBService service) {
        int startLn = service.getSourceLine().ln;
        int endLn = service.getSourceEndLine().ln;

        int startIdx = CollectionUtils.lastIndexOfCustom(processedLines, e -> e.ln == startLn);
        if (startIdx < 0) {
            return;
        }

        for (int idx = startIdx; idx < processedLines.size(); idx++) {
            Line srcLine = processedLines.get(idx);
            if (srcLine.ln > endLn) {
                return;
            }
            int firstCharNonWhitespace = Utils.firstCharNonWhitespace(srcLine.data);
            if (firstCharNonWhitespace == -1 || firstCharNonWhitespace == '/') { // 空行和注释行
                continue;
            }
            processedLines.set(idx, new Line(srcLine.ln, "// " + srcLine.data));
        }
    }

    private PBMethod parseMethod(List<LineInfo> commentLines, LineInfo lineInfo) {
        // 'rpc Search(SearchRequest request) returns (SearchResponse);'
        final String content = lineInfo.content;
        ensureEndWithSemicolon(lineInfo, content);
        String name;
        String argType;
        String argName;
        {
            int nameStart = content.indexOf(' ');
            int argStart = content.indexOf('(');
            int argEnd = content.indexOf(')');

            name = content.substring(nameStart + 1, argStart).trim();

            String args = content.substring(argStart + 1, argEnd);
            if (StringUtils.isBlank(args)) {
                argType = null;
                argName = null;
            } else {
                String[] argArray = StringUtils.split(args, ' ');
                if (argArray.length == 1) { // 没有参数名
                    argType = argArray[0].trim();
                    argName = options.getArgNameFunc().apply(argType);
                } else {
                    argType = argArray[0].trim();
                    argName = argArray[1].trim();
                }
            }
        }

        String resultType;
        {
            int rStart = content.lastIndexOf('(');
            int rEnd = content.lastIndexOf(')');
            String results = content.substring(rStart + 1, rEnd);
            if (StringUtils.isBlank(results)) {
                resultType = null;
            } else {
                resultType = results.trim();
            }
        }

        PBMethod method = new PBMethod()
                .setArgType(argType)
                .setArgName(argName)
                .setResultType(resultType);
        method.setSimpleName(name)
                .setSourceLine(lineInfo.asLine())
                .setSourceEndLine(lineInfo.asLine());

        // 追加注释
        drainCommentLine(commentLines, method);
        if (lineInfo.hasComment()) {
            method.addComment(lineInfo.comment);
        }
        // 处理元注解
        processMethodAnnotations(lineInfo, method);
        return method;
    }

    private void processMethodAnnotations(LineInfo lineInfo, PBMethod method) {
        PBAnnotation annotation = method.getAnnotation(AnnotationTypes.METHOD);
        if (annotation == null) {
            throw new PBParserException("The annotation @RpcMethod is absent, line: " + lineInfo);
        }
        {
            DsonObject<String> dsonValue = annotation.getDsonValue();
            DsonValue id = dsonValue.get("id"); // 默认是double类型
            if (id == null) {
                throw new PBParserException("The id of method is absent, line: " + lineInfo);
            }
            int methodId = id.asNumber().intValue();
            if (methodId < 0 || methodId > 9999) {
                throw new PBParserException("invalid methodId, line: " + lineInfo);
            }
            method.setMethodId(methodId);

            DsonValue mode = dsonValue.get("mode"); // 也可根据service的name或id计算
            if (mode != null) {
                method.setMode(mode.asNumber().intValue());
            } else {
                method.setMode(options.getMethodDefMode());
            }
            DsonValue ctx = dsonValue.get("ctx");
            if (ctx != null) {
                method.setCtx(ctx.asBool());
            } else {
                method.setCtx(options.isMethodDefCtx());
            }
        }
    }

    private void processServiceAnnotations(PBService service) {
        PBAnnotation annotation = service.getAnnotation(AnnotationTypes.SERVICE);
        if (annotation == null) {
            throw new PBParserException("The annotation @RpcService is absent, line: " + service.getSourceLine());
        }
        DsonObject<String> dsonValue = annotation.getDsonValue();
        {
            DsonValue id = dsonValue.get("id"); // 默认是double类型
            if (id == null) {
                throw new PBParserException("The id of method is absent, line: " + service.getSourceLine());
            }
            int serviceId = id.asNumber().intValue();
            if (serviceId < Short.MIN_VALUE || serviceId > Short.MAX_VALUE) {
                throw new PBParserException("invalid serviceId, line: " + service.getSourceLine());
            }
            service.setServiceId(serviceId);

            DsonValue genProxy = dsonValue.get("genProxy"); // 最好是根据name计算
            if (genProxy != null) {
                service.setGenProxy(genProxy.asBool());
            }
            DsonValue genExporter = dsonValue.get("genExporter");
            if (genExporter != null) {
                service.setGenExporter(genExporter.asBool());
            }
        }
    }

    // endregion

    // region message

    private PBField parseField(List<LineInfo> commentLines, LineInfo lineInfo) {
        // 为方便解析，会逐渐进行裁剪
        String content = lineInfo.content;
        ensureEndWithSemicolon(lineInfo, content);

        Integer modifier;
        switch (lineInfo.keyword) {
            case PBKeywords.REQUIRED -> modifier = PBField.MODIFIER_REQUIRED;
            case PBKeywords.REPEATED -> modifier = PBField.MODIFIER_REPEATED;
            case PBKeywords.OPTIONAL -> modifier = PBField.MODIFIER_OPTIONAL;
            default -> modifier = null;
        }
        if (modifier != null) { // 去掉修饰符
            content = content.substring(content.indexOf(' ')).stripLeading();
        }

        final String keyType;
        final String valueType;
        {
            int startIdx = content.indexOf('<');
            if (startIdx > 0) {
                int endIdx = content.indexOf('>');
                String[] genericTypes = StringUtils.splitPreserveAllTokens(content.substring(startIdx + 1, endIdx), ',');
                if (genericTypes.length != 2) {
                    throw new PBParserException("Invalid genericTypes, line: " + lineInfo);
                }
                keyType = genericTypes[0].trim();
                valueType = genericTypes[1].trim();
                content = content.substring(0, startIdx) + content.substring(endIdx + 1); // 去掉泛型部分
            } else {
                keyType = null;
                valueType = null;
            }
        }

        final int typeEndIdx = content.indexOf(' ');
        int eqIdx = content.indexOf('=');
        int opIdx = content.indexOf('['); // 可选项开始符

        final String type = content.substring(0, typeEndIdx);
        final String name = content.substring(typeEndIdx + 1, eqIdx).trim();
        final int number;
        {
            String numberString = opIdx > 0
                    ? content.substring(eqIdx + 1, opIdx)
                    : content.substring(eqIdx + 1, content.length() - 1); // -1去掉 ';'
            number = Integer.parseInt(numberString.trim());
        }

        PBField field = new PBField()
                .setModifier(modifier == null ? PBField.MODIFIER_OPTIONAL : modifier)
                .setType(type)
                .setKeyType(keyType)
                .setValueType(valueType)
                .setNumber(number);
        field.setSimpleName(name)
                .setSourceLine(lineInfo.asLine())
                .setSourceEndLine(lineInfo.asLine());
        // 追加注释
        drainCommentLine(commentLines, field);
        if (lineInfo.hasComment()) {
            field.addComment(lineInfo.comment);
        }
        return field;
    }

    private PBEnumValue parseEnumValue(List<LineInfo> commentLines, LineInfo lineInfo) {
        final String content = lineInfo.content;
        ensureEndWithSemicolon(lineInfo, content);
        int eqIdx = content.indexOf('=');
        int opIdx = content.indexOf('['); // 可选项开始符

        String name = content.substring(0, eqIdx).stripTrailing();
        int number;
        {
            String numberString = opIdx > 0
                    ? content.substring(eqIdx + 1, opIdx)
                    : content.substring(eqIdx + 1, content.length() - 1); // -1去掉 ';'
            number = Integer.parseInt(numberString.trim());
        }
        PBEnumValue enumValue = new PBEnumValue()
                .setNumber(number);
        enumValue.setSimpleName(name)
                .setSourceLine(lineInfo.asLine())
                .setSourceEndLine(lineInfo.asLine());

        // 追加注释
        drainCommentLine(commentLines, enumValue);
        if (lineInfo.hasComment()) {
            enumValue.addComment(lineInfo.comment);
        }
        return enumValue;
    }

    // endregion

    // region 注释

    /** 累积注释行 -- 空白行会导致注释中断 */
    private void cumulativeCommentLine(LineInfo lineInfo) {
        if (lineInfo.hasComment()) {
            context.addCommentLine(lineInfo);
        } else {
            // 空白行中断注释
            context.clearCommentLines();
        }
    }

    /** 传递注释行 */
    private void drainCommentLine(List<LineInfo> commentLines, PBElement element) throws PBParserException {
        for (LineInfo commentLine : commentLines) {
            element.addComment(commentLine.comment);

            PBAnnotation annotation = tryParseAnnotation(commentLine);
            if (annotation != null) {
                element.addAnnotation(annotation);
            }
        }
    }

    /** 解析注解 */
    @Nullable
    private PBAnnotation tryParseAnnotation(LineInfo lineInfo) throws PBParserException {
        // '//@RpcService {}'
        String comment = lineInfo.comment;
        int startIndex = Utils.indexOfNonWhitespace(comment, 2);
        if (startIndex < 0 || comment.charAt(startIndex) != '@') {
            return null; // @前面有其它内容
        }

        int valueStartIndex = comment.indexOf('{');
        int valueEndIndex = comment.lastIndexOf('}');
        if (valueStartIndex < 0 || valueStartIndex >= valueEndIndex) {
            return null;
        }

        String type = comment.substring(startIndex + 1, valueStartIndex).trim();
        if (StringUtils.containsWhitespace(type)) {
            return null; // '//@ A C{' , 类型不连贯
        }
        String value = comment.substring(valueStartIndex, valueEndIndex + 1);
        PBAnnotation annotation = new PBAnnotation(type, value);
        try {
            annotation.getDsonValue(); // 提前抛出异常
            return annotation;
        } catch (Exception e) {
            throw new PBParserException("invalid dson, line: " + lineInfo, e);
        }
    }

    /** 测试注释是否是注解 */
    public static boolean isAnnotationComment(String comment) {
        int startIndex = Utils.indexOfNonWhitespace(comment, 2);
        if (startIndex < 0 || comment.charAt(startIndex) != '@') {
            return false; // @前面有其它内容
        }
        int valueStartIndex = comment.indexOf('{');
        int valueEndIndex = comment.lastIndexOf('}');
        if (valueStartIndex < 0 || valueStartIndex >= valueEndIndex) {
            return false;
        }

        String type = comment.substring(startIndex + 1, valueStartIndex).trim();
        if (StringUtils.containsWhitespace(type)) {
            return false; // '//@ A C{' , 类型不连贯
        }
        return true;
    }

    // endregion

    // region 工具方法

    private static boolean isStartWith(LineInfo lineInfo, char c) {
        String content = lineInfo.content;
        return content.length() > 0 && content.charAt(0) == c;
    }

    /** 确保内容行以 ';' 结尾 */
    private static void ensureEndWithSemicolon(LineInfo lineInfo, String content) {
        if (Utils.lastChar(content) != ';') {
            throw new PBParserException("expected ';', line: " + lineInfo);
        }
    }

    /** 解析双引号内的值 */
    private String parseQuoteValue(LineInfo lineInfo) {
        final String content = lineInfo.content;
        int startIdx = content.indexOf('"');
        int endIdx = content.lastIndexOf('"');
        return content.substring(startIdx + 1, endIdx).trim();
    }

    /** 预解析行 */
    private static LineInfo parseLine(Line line) throws PBParserException {
        String rawLine = line.data;
        if (StringUtils.isBlank(rawLine)) {
            return new LineInfo(line, "", "", "");
        }
        String content;
        String comment;
        {
            int idx = rawLine.indexOf('/');
            if (idx >= 0) {
                if (rawLine.charAt(idx + 1) != '/') {
                    throw new PBParserException("incorrect comment format, ln: " + line.ln);
                }
                if (idx == 0) {
                    content = "";
                    comment = rawLine;
                } else {
                    content = rawLine.substring(0, idx).trim();
                    comment = rawLine.substring(idx).stripTrailing(); // 前部非空
                }
            } else {
                content = rawLine.trim();
                comment = "";
            }
        }
        String keyword;
        {
            int firstChar = content.length() > 0 ? content.charAt(0) : -1;
            if (firstChar == -1 || firstChar == '{' || firstChar == '}') {
                keyword = null;
            } else {
                int idx = content.indexOf(' ');
                if (idx > 0) {
                    int mapIdx = content.indexOf('<', 0, idx);
                    if (mapIdx > 0) { // 处理泛型
                        keyword = content.substring(0, mapIdx).trim();
                    } else {
                        keyword = content.substring(0, idx).trim();
                    }
                } else if ((idx = content.indexOf('=')) > 0) {
                    // 没有空格的情况下，可能有'='
                    keyword = content.substring(0, idx).trim();
                } else {
                    keyword = null;
                }
            }
        }
        return new LineInfo(line, content, comment, keyword);
    }

    // endregion

    private static class LineInfo {

        /** 原始行 */
        final Line rawLine;
        /** 内容部分 -- 执行了trim */
        final String content;
        /** 注释部分 -- 执行了trim */
        final String comment;
        /** 首个关键字 -- 这里只是简单的解析 */
        final String keyword;

        public LineInfo(Line line, String content, String comment, String keyword) {
            this.rawLine = line;
            this.content = content;
            this.comment = comment;
            this.keyword = keyword;
        }

        public int getLn() {
            return rawLine.ln;
        }

        public Line asLine() {
            return rawLine;
        }

        public boolean isEmptyLine() {
            return StringUtils.isBlank(content) && StringUtils.isBlank(comment);
        }

        /** 是否是注释行 -- 只要没有内容，就认为是注释行 */
        public boolean isCommentLine() {
            return StringUtils.isBlank(content);
        }

        public boolean hasContent() {
            return !StringUtils.isBlank(content);
        }

        public boolean hasComment() {
            return !StringUtils.isBlank(comment);
        }

        @Override
        public String toString() {
            return rawLine.toString();
        }
    }

    private static class Context {

        final Context parent;
        final ContextType contextType;
        final PBElement container;

        /** 是否已读取到开始符号 '{' */
        boolean started;
        final List<LineInfo> commentLines = new ArrayList<>(10);

        public Context(Context parent, ContextType contextType, PBElement container) {
            this.parent = parent;
            this.contextType = contextType;
            this.container = container;
        }

        public PBFile asFile() {
            return (PBFile) container;
        }

        public PBService asService() {
            return (PBService) container;
        }

        public PBMessage asMessage() {
            return (PBMessage) container;
        }

        public void addCommentLine(LineInfo lineInfo) {
            commentLines.add(lineInfo);
        }

        /** 弹出缓存的注释行 */
        public List<LineInfo> popCommentLines() {
            if (commentLines.isEmpty()) {
                return new ArrayList<>();
            }
            ArrayList<LineInfo> result = new ArrayList<>(commentLines);
            commentLines.clear();
            return result;
        }

        public void clearCommentLines() {
            commentLines.clear();
        }
    }
}