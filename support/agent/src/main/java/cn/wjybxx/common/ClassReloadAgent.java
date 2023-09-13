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

package cn.wjybxx.common;

import com.sun.tools.attach.VirtualMachine;

import java.lang.instrument.ClassDefinition;
import java.lang.instrument.Instrumentation;
import java.lang.instrument.UnmodifiableClassException;
import java.util.Objects;
import java.util.jar.JarFile;

/**
 * Instrumentation开发指南 - https://www.ibm.com/developerworks/cn/java/j-lo-jse61/index.html
 * <h3>热更新限制</h3>
 * 1. 热更新只可以更改方法体和常量池，不可以增删属性和方法，不可以修改继承关系。
 * 2. 已初始化的类不会再次进行初始化（注意静态代码块）。
 * 3. 热更的方法，只有再次进入时才会生效。
 * 4. 被内联的方法可能提示热更新成功，却永远得不到执行（注意热点代码）。
 * 5. class对象引用不会改变，即不需要向其它热更新方式那样迁移数据（这是很大的优势）。
 * 6. 不可增删lambda表达式，不可以增删方法引用。
 * 7. 内部类和外部类必须一起热更新。
 *
 * <h3>违背直觉的情况</h3>
 * 1. lambda表达式：如果lambda表达式捕获的变量变更，将无法热更（因为编译时会生成特殊的粘合类，粘合类的成员属性会变更）。
 * 2. 内部类：如果需要访问另一个类的private字段，将无法热更（因为编译时会为其生成特殊的桥接方法，新增了静态方法）。
 * 3. switch：大型的switch语句无法热更（大型switch语句建议使用map进行映射）。
 *
 * <h3>奇巧淫技</h3>
 * 1. 每个manager额外定义一个通用方法 {@code Object execute(String cmd, Object params)}，当需要某个manager的功能和属性时，可以迂回救国。
 * 2. 每个manager额外定义一个黑板，比如就一个Map，这样当需要新增属性时，可以添加到map中。加上上一条，最好有个manager基类？
 * 3. 每个玩家额外定义两个黑板，比如两个map，一个存库，一个不存库。这样当需要在玩家身上新增属性时，可以存储到map中。
 * 4. 玩家与服务器之间可以预留几条通用协议，用于救急。
 * 5. 内部类的属性尽量声明为包级（默认权限），尽量少使用private，或提供getter/setter方法。
 *
 * <h3>使用方式</h3>
 * 1. 由于代理必须以jar包形式存在，因此文件检测，执行更新等逻辑，请写在自己的业务逻辑包中，不要写在这里，方便扩展。
 * 2. 热更新时不要一次更新太多类，否则可能导致停顿时间过长。
 * 3. 由于该API于IDE热更新是一套API，因此必须在本机上进行热更新测试，本机能通过，基本上运行环境也能通过。
 * 4. 每次启服后，需要进行一次热更流程，避免使用的是旧的class文件。
 * 5. 除非重启服务器，否则热更的代码不可删除。因此，只有在更版本的时候才可以删除热更代码，替换为正式的代码（这也是启服后必须执行一次热更的原因）。
 * 6. 热更只应该用于修改重大bug，不建议动不动就热更，平时要保证代码质量。
 *
 * <p>
 * debug下使用{@link #agentmain(String, Instrumentation)}比较方便，直接在ide中添加启动参数就可以。
 * 线上使用{@link #premain(String, Instrumentation)}方式比较方便。
 *
 * @author wjybxx
 * date 2023/9/9
 */
public class ClassReloadAgent {

    private static volatile Instrumentation instrumentation;

    private static void setInstrumentation(Instrumentation instrumentation) {
        ClassReloadAgent.instrumentation = Objects.requireNonNull(instrumentation);
    }

    /**
     * 这是instrument开发规范规定的固定格式的方法，当java程序启动时，如果指定了javaagent参数(classpath下的jar包名字)，则会自动调用到这个方法。
     * 注意：
     * 1. 需要在启动参数中指定 javaagent参数。eg:
     * -javaagent:game-classreloadagent-1.0.jar=test
     * 则agentArgs为test(若无等号则为null)
     * 2. 不能方便的调试，必须在命令行中启动。
     *
     * @param agentArgs       启动参数
     * @param instrumentation 我们需要的实例，需要将其保存下来
     */
    public static void premain(String agentArgs, Instrumentation instrumentation) {
        System.out.println("premain invoked, agentArgs: " + agentArgs);
        ClassReloadAgent.setInstrumentation(instrumentation);
    }

    /**
     * 使用动态attach的方式获取{@link Instrumentation}。
     * 这是instrument开发规范规定的固定格式的方法，当使用{@link VirtualMachine#loadAgent(String, String)}连接到JVM时，会触发该方法。
     * <p>
     * 注意
     * 1. 如果要attach到自身所在JVM，需要添加启动参数 -Djdk.attach.allowAttachSelf=true 否则会抛出异常。
     * 2. 它的参数来自{@link VirtualMachine#loadAgent(String, String)}的第二个参数(options)
     * 3. 可直接在debug环境下使用（在debug参数中指定vm options即可）。
     *
     * @param agentArgs       {@link VirtualMachine#loadAgent(String, String)}中的options
     * @param instrumentation 我们需要的实例，需要将其保存下来
     */
    public static void agentmain(String agentArgs, Instrumentation instrumentation) {
        System.out.println("agentmain invoked, agentArgs: " + agentArgs);
        ClassReloadAgent.setInstrumentation(instrumentation);
    }

    /**
     * 查询是否启用了重定义类功能。
     * 注意：该返回值与{@link #isModifiableClass(Class)}返回值没有关系。
     */
    public static boolean isRedefineClassesSupported() {
        return instrumentation.isRedefineClassesSupported();
    }

    /**
     * 查询一个类是否可以被修改（被重定义），是否可以用于{@link #redefineClasses(ClassDefinition...)}方法。
     * 注意：该返回值与{@link #isRedefineClassesSupported()}的返回值没有关系。
     * 所以：要热更新类时需要保证{@link #isRedefineClassesSupported()}为true，且要更新的类调用当前方法返回值为true。
     * 基本上：你在ide中能热更新，那么这里就能热更新，所以最好现在ide上测试一下。
     *
     * @param theClass 要热更新的类
     * @return true/false
     */
    public static boolean isModifiableClass(Class<?> theClass) {
        return instrumentation.isModifiableClass(theClass);
    }

    /**
     * 重定义类文件（热更新类文件）
     * 注意：请保证{@link #isRedefineClassesSupported()}为true，且所有要更新的类{@link #isModifiableClass(Class)}为true。
     *
     * @param definitions 要热更新的类（要重定义的类）
     *                    注意：该方法的参数是数组，如果类之间有关系的话，最好一起更新（原子方式更新）。
     */
    public static void redefineClasses(ClassDefinition... definitions) throws ClassNotFoundException, UnmodifiableClassException {
        instrumentation.redefineClasses(definitions);
    }

    /**
     * 添加指定jar包到<b>根类加载器</b>路径中
     *
     * @param jarfile 要加载的jar包
     */
    public static void appendToBootstrapClassLoaderSearch(JarFile jarfile) {
        instrumentation.appendToBootstrapClassLoaderSearch(jarfile);
    }

    /**
     * 添加指定jar包到<b>系统类加载器</b>路径中
     *
     * @param jarfile 要加载的jar包
     */
    public static void appendToSystemClassLoaderSearch(JarFile jarfile) {
        instrumentation.appendToSystemClassLoaderSearch(jarfile);
    }
}
