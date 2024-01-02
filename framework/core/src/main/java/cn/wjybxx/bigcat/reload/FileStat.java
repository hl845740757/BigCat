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

package cn.wjybxx.bigcat.reload;

/**
 * 文件的状态
 * 1.长度可简单判断文件是否改变。
 * 2.使用MD5而不是上次修改时间，是因为上次修改时间是一个不太靠谱的值。
 *
 * @author wjybxx
 * date - 2023/5/21
 */
class FileStat {

    final long length;
    final String md5;

    public FileStat(long length, String md5) {
        this.length = length;
        this.md5 = md5;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) {
            return true;
        }
        if (o == null || getClass() != o.getClass()) {
            return false;
        }
        final FileStat fileStat = (FileStat) o;
        return length == fileStat.length &&
                md5.equals(fileStat.md5);
    }

    @Override
    public int hashCode() {
        return 31 * Long.hashCode(length) + md5.hashCode();
    }

}