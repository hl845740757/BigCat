package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * @author wjybxx
 * date - 2023/12/5
 */
@ClassImpl(singleton = "getInstance")
@BinarySerializable
@DocumentSerializable
public class SingletonTest {

    private final String name;
    private final int age;

    private SingletonTest(String name, int age) {
        this.name = name;
        this.age = age;
    }

    private static final SingletonTest INST = new SingletonTest("wjybxx", 29);

    public static SingletonTest getInstance() {
        return INST;
    }

    public String getName() {
        return name;
    }

    public int getAge() {
        return age;
    }
}
