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

/**
 * 1.该字段应该作为final字段
 * 2.该对象的线程安全性依赖于发布的安全性
 * 3.除了链接时，其它时候不建议访问id
 * <p>
 * Q：为什么要将id放在这个类里？
 * A：如果这里不支持id，外部数据就需要定义两个字段：xxxId 和 xxxRef，命名是一个费脑的活，
 * 虽然这个类看起来有点不自然，但外部会更加干净。
 *
 * @author wjybxx
 * date - 2023/5/25
 */
public final class FileDataRef<T> {

    private final long numId;
    private final String stringId;
    private T value;

    private FileDataRef(long numId, String stringId) {
        this.numId = numId;
        this.stringId = stringId;
    }

    public static <T> FileDataRef<T> create(int id) {
        return new FileDataRef<>(id, null);
    }

    public static <T> FileDataRef<T> create(long id) {
        return new FileDataRef<>(id, null);
    }

    public static <T> FileDataRef<T> create(String id) {
        return new FileDataRef<>(0, id);
    }

    public int getIntId() {
        return (int) numId;
    }

    public long getLongId() {
        return numId;
    }

    public String getStringId() {
        return stringId;
    }

    public void set(T value) {
        this.value = value;
    }

    public T get() {
        return value;
    }

}