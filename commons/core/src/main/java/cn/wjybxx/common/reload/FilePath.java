/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.common.reload;

import cn.wjybxx.common.AbstractConstant;
import cn.wjybxx.common.Constant;
import cn.wjybxx.common.ConstantPool;
import jodd.io.FileNameUtil;

/**
 * 1. path的含义取决于应用自身，可能是绝对路径，也可能是相对路径，也可能是其它的格式。
 * 2. 不直接使用String，可提供额外的扩展。
 *
 * @author wjybxx
 * date - 2023/5/19
 */
public final class FilePath<T> extends AbstractConstant<FilePath<?>> {

    /** 如果虚拟属性为true，则表示文件并不真实存在，只是在逻辑上用于存储数据用 */
    private final boolean virtual;

    private final String fileName;
    private final String fileNameWithoutExt;

    public FilePath(Builder builder) {
        super(builder);
        this.virtual = builder.virtual;

        // 暂时先采用缓存的方式
        this.fileName = FileNameUtil.getName(builder.getName());
        this.fileNameWithoutExt = FileNameUtil.removeExtension(fileName);
    }

    /** @return 绑定的文件路径 - 这是一个转义方法 */
    public String getPath() {
        return name();
    }

    /** @return 如果文件是虚拟的则返回true */
    public boolean isVirtual() {
        return virtual;
    }

    public String getFileName() {
        return fileName;
    }

    public String getFileNameWithoutExt() {
        return fileNameWithoutExt;
    }

    // region pool

    /** 全局文件路径池 - 你可以创建新的Pool实例实现隔离环境 */
    private static final FilePathPool POOL = FilePathPool.newPool();

    public static <T> FilePath<T> newPath(String path) {
        return POOL.newPath(path);
    }

    public static <T> FilePath<T> newVirtualPath(String path) {
        return POOL.newVirtualPath(path);
    }

    public static <T> FilePath<T> newPath(Builder builder) {
        return POOL.newPath(builder);
    }

    public static boolean exists(String path) {
        return POOL.exists(path);
    }

    public static FilePath<?> get(String path) {
        return POOL.get(path);
    }

    public static FilePath<?> getOrThrow(String path) {
        return POOL.getOrThrow(path);
    }
    // endregion

    public static Builder newBuilder(String name) {
        return new Builder(name);
    }

    public static class Builder extends Constant.Builder<FilePath<?>> {

        private boolean virtual = false;

        public Builder(String name) {
            super(name);
        }

        public boolean isVirtual() {
            return virtual;
        }

        public Builder setVirtual(boolean virtual) {
            this.virtual = virtual;
            return this;
        }

        @Override
        public FilePath<?> build() {
            return new FilePath<>(this);
        }
    }

}