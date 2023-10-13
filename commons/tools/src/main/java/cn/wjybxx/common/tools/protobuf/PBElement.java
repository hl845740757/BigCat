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

import cn.wjybxx.common.tools.util.Line;
import cn.wjybxx.common.tools.util.Utils;

import javax.annotation.Nonnull;
import java.util.*;

/**
 * protobuf所有元素抽象
 *
 * @author wjybxx
 * date - 2023/10/7
 */
public abstract class PBElement {

    /** 简单名 */
    private String simpleName;
    /** 注释 -- 包含注解的原始注释 */
    private final List<String> comments = new ArrayList<>();
    /** 可选项 */
    private final Map<String, String> options = new LinkedHashMap<>();

    /** 定义该元素的元素 */
    private PBElement enclosingElement;
    /** 嵌套定义的元素 -- 任何便捷查询都是筛选后的快照 */
    private final List<PBElement> enclosedElements = new ArrayList<>();
    /** 注解数据 -- 同一类型允许重复 */
    private final List<PBAnnotation> annotations = new ArrayList<>();

    /** 定义元素的源代码行 -- 可能为null，表示非源码文件定义 */
    private Line sourceLine;
    /** 结束元素定义的行 -- 可能为null，特殊用途（节选） */
    private Line sourceEndLine;

    // region

    @Nonnull
    public abstract PBElementKind getKind();

    public PBElement addEnclosedElement(PBElement enclosed) {
        Objects.requireNonNull(enclosed);
        if (this == enclosed) throw new IllegalArgumentException("add self");

        enclosed.enclosingElement = this;
        enclosedElements.add(enclosed);
        return this;
    }

    public PBElement addAnnotation(PBAnnotation annotation) {
        annotations.add(annotation);
        return this;
    }

    public PBElement addComment(String comment) {
        comments.add(comment);
        return this;
    }

    public PBElement addOption(String key, String value) {
        options.put(key, value);
        return this;
    }

    /** 获取指定类型注解 */
    public PBAnnotation getAnnotation(String type) {
        return annotations.stream()
                .filter(e -> e.type.equals(type))
                .findFirst()
                .orElse(null);
    }

    /** 获取指定类型注解 */
    public List<PBAnnotation> getAnnotations(String type) {
        return annotations.stream()
                .filter(e -> e.type.equals(type))
                .toList();
    }

    /** 获取可选项的值 */
    public String getOption(String name) {
        return options.get(name);
    }

    /** 获取去除掉双引号的可选项值 */
    public String getUnquoteOption(String name) {
        return Utils.unquote(options.get(name));
    }

    // endregion

    // region getter/setter

    public String getSimpleName() {
        return simpleName;
    }

    public PBElement setSimpleName(String simpleName) {
        this.simpleName = simpleName;
        return this;
    }

    public Line getSourceLine() {
        return sourceLine;
    }

    public PBElement setSourceLine(Line sourceLine) {
        this.sourceLine = sourceLine;
        return this;
    }

    public Line getSourceEndLine() {
        return sourceEndLine;
    }

    public PBElement setSourceEndLine(Line sourceEndLine) {
        this.sourceEndLine = sourceEndLine;
        return this;
    }

    public List<String> getComments() {
        return comments;
    }

    public PBElement getEnclosingElement() {
        return enclosingElement;
    }

    public Map<String, String> getOptions() {
        return options;
    }

    public List<PBElement> getEnclosedElements() {
        return enclosedElements;
    }

    public List<PBAnnotation> getAnnotations() {
        return annotations;
    }

    // endregion

    @Override
    public final String toString() {
        StringBuilder stringBuilder = new StringBuilder()
                .append(getKind()).append("{")
                .append("simpleName='").append(simpleName).append('\'')
                .append(", enclosingElement=").append(enclosingElement == null ? null : enclosingElement.getSimpleName());

        toString(stringBuilder);
        return stringBuilder
                .append('}').toString();
    }

    protected void toString(StringBuilder sb) {

    }

}