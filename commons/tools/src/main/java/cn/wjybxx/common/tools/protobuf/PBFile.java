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

import javax.annotation.Nonnull;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

/**
 * pb文件
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public class PBFile extends PBElement {

    /** 文件名 */
    private String fileName;
    /** 语法级别 */
    private String syntax;
    /** 导入的文件（显式声明的依赖和commons） */
    private final Set<String> imports = new LinkedHashSet<>(4);

    //

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.FILE;
    }

    public PBFile addImport(String fileName) {
        if (!fileName.endsWith(".proto")) {
            throw new IllegalArgumentException(); // protobuf语法规范
        }
        this.imports.add(fileName);
        return this;
    }

    public List<PBService> getServices() {
        return getEnclosedElements().stream()
                .filter(e -> e.getKind() == PBElementKind.SERVICE)
                .map(e -> (PBService) e)
                .toList();
    }

    public List<PBMessage> getMessages() {
        return getEnclosedElements().stream()
                .filter(e -> e.getKind() == PBElementKind.MESSAGE)
                .map(e -> (PBMessage) e)
                .toList();
    }

    //

    public String getFileName() {
        return fileName;
    }

    public PBFile setFileName(String fileName) {
        this.fileName = fileName;
        return this;
    }

    public String getSyntax() {
        return syntax;
    }

    public PBFile setSyntax(String syntax) {
        this.syntax = syntax;
        return this;
    }

    public Set<String> getImports() {
        return imports;
    }

}