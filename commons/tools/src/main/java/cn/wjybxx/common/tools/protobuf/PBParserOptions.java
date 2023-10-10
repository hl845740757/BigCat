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

import cn.wjybxx.common.ObjectUtils;
import org.apache.commons.lang3.StringUtils;

import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.Objects;
import java.util.Set;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.stream.Collectors;

/**
 * pb文件解析器选项
 *
 * @author wjybxx
 * date - 2023/10/7
 */
public class PBParserOptions {

    /** 预处理期间的临时文件夹 */
    private String tempDir = "./temp";
    /** 语法类型 */
    private String syntax = "proto3";

    /** java文件输出目录 */
    private String javaOut;
    /** java文件输出包名 */
    private String javaPackage;
    /** java外部类类名生成方式 - 参数为{@link PBFile#getSimpleName()} */
    private Function<String, String> outerClassNameFunc = PBParserOptions::outerClassName;

    /** csharp文件输出目录 */
    private String csharpOut;
    /** csharp文件命名空间 */
    private String csharpNamespace;

    /**
     * 公共协议文件，不含proto文件后缀
     * 1. 第一个文件为Root文件，Root不会依赖任何其它文件
     * 2. 其它文件只依赖第一个公共文件，而不会互相依赖
     * 3. 非commons文件则依赖这里的所有文件
     */
    private final LinkedHashSet<String> commons = new LinkedHashSet<>();
    /** 方法的默认模式 */
    private int methodDefMode = PBMethod.MODE_NORMAL;

    /**
     * 服务预处理的拦截器
     * 1.根据service的名字或id，计算是否生成proxy等。
     * 2.根据service的名字或id，计算方法的mode等；
     * <p>
     * 1. 相比于每个Service都手动配置，通过命名或id进行区分更符合一般工程设计；
     * 2. 拦截器在读取Service完成后调用。
     */
    private Consumer<? super PBService> serviceInterceptor = (service) -> {};
    /**
     * 消息预处理拦截器
     */
    private Consumer<? super PBMessage> messageInterceptor = (message) -> {};
    /**
     * 方法参数名的生成方式 - 参数为{@link PBMethod#getArgType()}
     * 如果沿用Protobuf格式，方法参数仅类型无名字，可通过该函数根据类型名生成变量名。
     */
    private Function<String, String> argNameFunc = ObjectUtils::firstCharToLowerCase;

    //

    /** 获取根依赖文件 */
    public String getRoot() {
        if (commons.size() == 0) {
            return null;
        }
        return commons.getFirst();
    }

    //

    public String getSyntax() {
        return syntax;
    }

    public PBParserOptions setSyntax(String syntax) {
        this.syntax = syntax;
        return this;
    }

    public String getJavaOut() {
        return javaOut;
    }

    public PBParserOptions setJavaOut(String javaOut) {
        this.javaOut = javaOut;
        return this;
    }

    public String getJavaPackage() {
        return javaPackage;
    }

    public PBParserOptions setJavaPackage(String javaPackage) {
        this.javaPackage = javaPackage;
        return this;
    }

    public String getCsharpOut() {
        return csharpOut;
    }

    public PBParserOptions setCsharpOut(String csharpOut) {
        this.csharpOut = csharpOut;
        return this;
    }

    public String getCsharpNamespace() {
        return csharpNamespace;
    }

    public PBParserOptions setCsharpNamespace(String csharpNamespace) {
        this.csharpNamespace = csharpNamespace;
        return this;
    }

    public Set<String> getCommons() {
        return commons;
    }

    public Function<String, String> getOuterClassNameFunc() {
        return outerClassNameFunc;
    }

    public PBParserOptions setOuterClassNameFunc(Function<String, String> outerClassNameFunc) {
        this.outerClassNameFunc = outerClassNameFunc;
        return this;
    }

    public String getTempDir() {
        return tempDir;
    }

    public PBParserOptions setTempDir(String tempDir) {
        this.tempDir = tempDir;
        return this;
    }

    public Consumer<? super PBService> getServiceInterceptor() {
        return serviceInterceptor;
    }

    public PBParserOptions setServiceInterceptor(Consumer<? super PBService> serviceInterceptor) {
        this.serviceInterceptor = serviceInterceptor;
        return this;
    }

    public Consumer<? super PBMessage> getMessageInterceptor() {
        return messageInterceptor;
    }

    public PBParserOptions setMessageInterceptor(Consumer<? super PBMessage> messageInterceptor) {
        this.messageInterceptor = messageInterceptor;
        return this;
    }

    public int getMethodDefMode() {
        return methodDefMode;
    }

    public PBParserOptions setMethodDefMode(int methodDefMode) {
        this.methodDefMode = methodDefMode;
        return this;
    }

    public Function<String, String> getArgNameFunc() {
        return argNameFunc;
    }

    public PBParserOptions setArgNameFunc(Function<String, String> argNameFunc) {
        Objects.requireNonNull(argNameFunc);
        this.argNameFunc = argNameFunc;
        return this;
    }

    /**
     * eg: bag_shop => MsgBagShop
     */
    private static String outerClassName(String simpleName) {
        return Arrays.stream(StringUtils.split(simpleName, '_'))
                .map(ObjectUtils::firstCharToUpperCase)
                .collect(Collectors.joining("", "Msg", ""));
    }

}