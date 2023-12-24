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

package cn.wjybxx.bigcat.reload;

import cn.wjybxx.base.CaseMode;
import cn.wjybxx.common.Md5Utils;

import java.io.File;
import java.io.IOException;

/**
 * @author wjybxx
 * date - 2023/5/21
 */
class ReloadUtils {

    /** 计算文件的状态信息 */
    static FileStat statOfFile(File file) throws IOException {
        final long newLength = file.length();
        final String newMd5 = md5Hex(file);
        return new FileStat(newLength, newMd5);
    }

    /** 计算文件的状态信息 */
    static FileStat statOfFileBytes(byte[] bytesOfFile) {
        return new FileStat(bytesOfFile.length, md5Hex(bytesOfFile));
    }

    private static String md5Hex(File file) throws IOException {
        return Md5Utils.md5Hex(file, CaseMode.UPPER_CASE);
    }

    private static String md5Hex(byte[] bytesOfFile) {
        return Md5Utils.md5Hex(bytesOfFile, CaseMode.UPPER_CASE);
    }

}