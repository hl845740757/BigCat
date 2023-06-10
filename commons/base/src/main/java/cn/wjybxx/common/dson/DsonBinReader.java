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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.annotation.Beta;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.Parser;

import javax.annotation.Nonnull;

/**
 * 编码格式可查看{@link Dsons}类文档
 * 解码流程为：type -> name -> value
 *
 * <p>
 * 1.读取数组内普通成员时，name传0，读取嵌套对象时使用无name参数的方法
 * 2.如果先调用了{@link #readName()}，name传0或之前读取的值
 *
 * @author wjybxx
 * date - 2023/4/20
 */
@SuppressWarnings("unused")
public interface DsonBinReader extends AutoCloseable {

    @Override
    void close();

    /** 当前是否处于应该读取type状态 */
    boolean isAtType();

    /**
     * 读取下一个值的类型
     * 如果到达对象末尾，则返回{@link DsonType#END_OF_OBJECT}
     * <p>
     * 循环的基本写法：
     * <pre>{@code
     *  DsonType dsonType;
     *  while((dsonType = readDsonType()) != DsonType.END_OF_OBJECT) {
     *      readName();
     *      readValue();
     *  }
     * }</pre>
     */
    DsonType readDsonType();

    /**
     * 读取下一个值的name
     */
    int readName();

    /**
     * 读取下一个值的name
     * 如果下一个name不等于期望的值，则抛出异常
     */
    void readName(int name);

    /**
     * 获取当前的数据类型
     * 1.该值在调用任意的读方法后将变化
     * 2.如果尚未执行过{@link #readDsonType()}则抛出异常
     */
    @Nonnull
    DsonType getCurrentDsonType();

    /**
     * 后去当前的字段编号
     * 1.该值在调用任意的读方法后将变化
     * 2.只有在读取值状态下才可访问
     *
     * @return 当前字段的name
     * @throws DsonCodecException 如果当前不是对象上下文
     */
    int getCurrentName();

    /**
     * 获取当前上下文的类型
     */
    DsonContextType getContextType();

    // region 简单值

    int readInt32(int name);

    long readInt64(int name);

    float readFloat(int name);

    double readDouble(int name);

    boolean readBoolean(int name);

    String readString(int name);

    void readNull(int name);

    DsonBinary readBinary(int name);

    DsonExtInt32 readExtInt32(int name);

    DsonExtInt64 readExtInt64(int name);

    DsonExtString readExtString(int name);

    ObjectRef readRef(int name);

    // endregion

    // region 容器

    void readStartArray();

    void readEndArray();

    void readStartObject();

    void readEndObject();

    /** 开始读取对象头，对象头属于对象的匿名属性 */
    void readStartHeader();

    void readEndHeader();

    /**
     * 回退到等待开始状态
     * 1.该方法只回退状态，不回退输入
     * 2.只有在等待读取下一个值的类型时才可以执行，即等待{@link #readDsonType()}时才可以执行
     * 3.通常用于在读取header之后回退，然后让业务对象的codec去解码
     */
    void backToWaitStart();

    default void readStartArray(int name) {
        readName(name);
        readStartArray();
    }

    default void readStartObject(int name) {
        readName(name);
        readStartObject();
    }

    // endregion

    // region 特殊支持

    /**
     * 如果当前是数组上下文，则不产生影响；
     * 如果当前是Object上下文，且处于读取Name状态则跳过name，否则抛出状态异常
     */
    void skipName();

    /**
     * 如果当前不处于读值状态则抛出状态异常
     */
    void skipValue();

    /**
     * 跳过当前容器对象(Array、Object、Header)的剩余内容
     * 调用该方法后，{@link #getCurrentDsonType()}将返回{@link DsonType#END_OF_OBJECT}
     */
    void skipToEndOfObject();

    /**
     * 读取一个protobuf消息
     * 只有当前数据是Binary的时候才合法
     */
    <T> T readMessage(int name, int binaryType, @Nonnull Parser<T> parser);

    /**
     * 将value的值读取为字节数组
     * 1.支持类型：String、Binary、Array、Object
     * 2.返回的bytes中去除了value的length信息，
     * 3.只在二进制流下生效
     * <p>
     * 该方法主要用于避免中间编解码过程，eg：
     * <pre>
     * A端：             B端         C端
     * object->bytes  bytes->bytes bytes->object
     * </pre>
     */
    byte[] readValueAsBytes(int name);

    @Beta
    DsonReaderGuide whatShouldIDo();

    // endregion

}