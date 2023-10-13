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

import java.util.*;

/**
 * pb文件仓库，用于建立索引，提供查询
 *
 * @author wjybxx
 * date - 2023/10/12
 */
public class PBRepository {

    private final LinkedHashMap<String, PBFile> fileMap = new LinkedHashMap<>();
    private final Map<String, PBElement> topElementNameMap = new LinkedHashMap<>();

    public PBRepository addFile(PBFile pbFile) {
        String simpleName = pbFile.getSimpleName();
        // 检查重复
        PBFile exist = fileMap.get(simpleName);
        if (exist != null && exist != pbFile) {
            throw new IllegalArgumentException("duplicate fileName " + simpleName);
        }
        fileMap.put(simpleName, pbFile);

        // 移除旧配置，重新添加，可避免元素变化后新旧元素分散
        if (exist != null) {
            topElementNameMap.remove(simpleName);
            for (PBElement element : pbFile.getEnclosedElements()) {
                topElementNameMap.remove(element.getSimpleName());
            }
        }
        topElementNameMap.put(simpleName, pbFile);
        for (PBElement element : pbFile.getEnclosedElements()) {
            topElementNameMap.put(element.getSimpleName(), element);
        }

        return this;
    }

    /** 获取所有的文件 -- 不可修改 */
    public SequencedCollection<PBFile> getFiles() {
        return Collections.unmodifiableSequencedCollection(fileMap.sequencedValues());
    }

    /** 获取排序后的所有的文件 -- 根据文件名排序，有助于逻辑的稳定性 */
    public List<PBFile> getSortedFiles() {
        ArrayList<PBFile> result = new ArrayList<>(fileMap.sequencedValues());
        result.sort(Comparator.comparing(PBFile::getSimpleName));
        return result;
    }

    /** 获取指定文件 */
    public PBFile getFile(String fileSimpleName) {
        return fileMap.get(fileSimpleName);
    }

    /**
     * 获取顶层元素
     *
     * @param elementName 顶层元素名，或文件名
     */
    public PBElement getTopElement(String elementName) {
        return topElementNameMap.get(elementName);
    }

    /**
     * 获取顶层元素关联的文件
     *
     * @param elementName 顶层元素名，或文件名
     */
    public PBFile getFileOfTopElement(String elementName) {
        PBElement pbElement = topElementNameMap.get(elementName);
        if (pbElement == null) {
            return null;
        }
        if (pbElement.getKind() == PBElementKind.FILE) {
            return (PBFile) pbElement;
        }
        return (PBFile) pbElement.getEnclosingElement();
    }

}