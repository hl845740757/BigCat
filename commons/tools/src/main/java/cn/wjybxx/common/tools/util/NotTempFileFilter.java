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

import org.apache.commons.io.filefilter.AbstractFileFilter;

import java.io.File;

/**
 * 排除常见的临时文件
 *
 * @author wjybxx
 * date - 2023/10/15
 */
public class NotTempFileFilter extends AbstractFileFilter {

    public static NotTempFileFilter INSTANCE = new NotTempFileFilter();

    @Override
    public boolean accept(File file) {
        return testName(file.getName());
    }

    @Override
    public boolean accept(File dir, String name) {
        return testName(name);
    }

    private boolean testName(String name) {
        return !name.startsWith("~&") // excel临时文件
                && !name.startsWith(".git") // git仓库
                && !name.startsWith(".svn"); // svn仓库
    }

}