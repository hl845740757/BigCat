/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.common;

import cn.wjybxx.base.CaseMode;
import org.apache.commons.codec.binary.Hex;
import org.apache.commons.codec.digest.DigestUtils;

import javax.annotation.Nonnull;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class Md5Utils {

    /**
     * 计算字节数组的MD5，并返回一个32个字符的十六进制字符串
     *
     * @param data 待计算的字节数组
     * @return 32个字符的十六进制字符串
     */
    public static String md5Hex(@Nonnull byte[] data, CaseMode mode) {
        return new String(Hex.encodeHex(DigestUtils.md5(data), mode.isLowerCase()));
    }

    /**
     * 计算字符串的MD5，并返回一个32个字符的十六进制字符串
     *
     * @param data 待计算的字符串
     * @return 32个字符的十六进制字符串
     */
    public static String md5Hex(@Nonnull String data, CaseMode mode) {
        return new String(Hex.encodeHex(DigestUtils.md5(data), mode.isLowerCase()));
    }

    /**
     * 计算文件的MD5，并返回一个32个字符的十六进制字符串。
     *
     * @param file 要计算md5的文件
     * @return 32个字符的十六进制字符串
     */
    public static String md5Hex(@Nonnull File file, CaseMode mode) throws IOException {
        try (FileInputStream fileInputStream = new FileInputStream(file)) {
            return md5Hex(fileInputStream, mode);
        }
    }

    /**
     * 计算inputStream剩余内容的MD5，并返回一个32个字符的十六进制字符串
     *
     * @param data 待计算的输入流，注意：该输入流并不会自动关闭！
     * @return 32个字符的十六进制字符串
     */
    public static String md5Hex(@Nonnull InputStream data, CaseMode mode) throws IOException {
        return new String(Hex.encodeHex(DigestUtils.md5(data), mode.isLowerCase()));
    }

}