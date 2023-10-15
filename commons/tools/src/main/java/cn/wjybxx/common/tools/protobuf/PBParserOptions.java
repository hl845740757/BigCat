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

import java.util.*;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
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

    /** proto文件目录 */
    private String protoDir;
    /** 预处理期间生成的临时文件的文件夹 -- 生成的临时proto文件在这里 */
    private String tempDir = "./temp";
    /**
     * protoc的路径
     * 如果不指定，则直接使用protoc命令；如果指定路径，则使用给定路径的protoc编译;
     * 以Windows为例，需要指定到'protoc.exe'
     */
    private String protocPath;

    /** 语法类型 -- 如果文件指定了语法，则使用文件自身的 */
    private String syntax = "proto3";
    /** 为文件追加的默认选项 -- 会自动追加到临时文件 */
    private Map<String, String> defOptions = new LinkedHashMap<>();
    /**
     * 公共协议文件，不含proto后缀
     * 1. 第一个文件为Root文件，Root不会依赖任何其它文件
     * 2. 其它文件只依赖第一个公共文件，而不会互相依赖
     * 3. 非commons文件则依赖这里的所有文件
     */
    private final LinkedHashSet<String> commons = new LinkedHashSet<>();
    /** 换行符 - 生成临时文件用 */
    private String lineSeparator = "\n";
    /**
     * 导出临时文件时保持前n行为文件头
     * 如果import和option行数不足将进行填充空白行，有助于减少临时文件与原文件的差异
     */
    private int headerLineCount = 0;

    /** java文件输出目录 */
    private String javaOut;
    /** java文件输出包名 */
    private String javaPackage;
    /** java外部类类名生成方式 - 参数为{@link PBFile#getSimpleName()} */
    private Function<String, String> outerClassNameFunc = PBParserOptions::outerClassName;
    /** 生成java文件时是否不使用外部类包装 */
    private boolean javaMultipleFiles = true;
    /** 在导出java文件时，是否先清除package中的文件 */
    private boolean cleanJavaPackage = true;

    /** csharp文件输出目录 */
    private String csharpOut;
    /** csharp文件命名空间 */
    private String csharpNamespace;

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

    /** 方法的默认模式 */
    private int methodDefMode = PBMethod.MODE_NORMAL;
    /** 方法ctx选项的默认值 */
    private boolean methodDefCtx = false;
    /** 异步方法返回值是否类型声明为{@link CompletionStage}，如果为false，则声明为{@link CompletableFuture} */
    private boolean useCompleteStage = false;
    /**
     * 方法参数名的生成方式 - 参数为{@link PBMethod#getArgType()}
     * 如果沿用Protobuf格式，方法参数仅类型无名字，可通过该函数根据类型名生成变量名。
     */
    private Function<String, String> argNameFunc = ObjectUtils::firstCharToLowerCase;

    //

    public boolean isProto2() {
        return syntax.equals("proto2");
    }

    /** 获取根依赖文件 */
    public String getRoot() {
        if (commons.size() == 0) {
            return null;
        }
        return commons.getFirst();
    }

    /** 获取文件名对应的外部类类名 */
    public String getOuterClassName(String fileSimpleName) {
        return outerClassNameFunc.apply(fileSimpleName);
    }

    /** 获取方法参数的默认名 */
    public String getArgName(String argType) {
        return argNameFunc.apply(argType);
    }

    /**
     * 添加默认的选项
     *
     * @param value 写入临时文件时会自动处理双引号问题
     */
    public PBParserOptions addDefOption(String name, String value) {
        defOptions.put(name, value);
        return this;
    }

    /** @param simpleName 要引入的公共文件，不包含proto后缀 */
    public PBParserOptions addCommon(String simpleName) {
        Objects.requireNonNull(simpleName);
        if (simpleName.endsWith(".proto")) {
            throw new IllegalArgumentException();
        }
        commons.add(simpleName);
        return this;
    }

    //

    public String getSyntax() {
        return syntax;
    }

    public PBParserOptions setSyntax(String syntax) {
        this.syntax = syntax;
        return this;
    }

    public String getProtocPath() {
        return protocPath;
    }

    public PBParserOptions setProtocPath(String protocPath) {
        this.protocPath = protocPath;
        return this;
    }

    public int getHeaderLineCount() {
        return headerLineCount;
    }

    public PBParserOptions setHeaderLineCount(int headerLineCount) {
        this.headerLineCount = headerLineCount;
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

    public boolean isJavaMultipleFiles() {
        return javaMultipleFiles;
    }

    public PBParserOptions setJavaMultipleFiles(boolean javaMultipleFiles) {
        this.javaMultipleFiles = javaMultipleFiles;
        return this;
    }

    public boolean isCleanJavaPackage() {
        return cleanJavaPackage;
    }

    public PBParserOptions setCleanJavaPackage(boolean cleanJavaPackage) {
        this.cleanJavaPackage = cleanJavaPackage;
        return this;
    }

    public String getProtoDir() {
        return protoDir;
    }

    public PBParserOptions setProtoDir(String protoDir) {
        this.protoDir = protoDir;
        return this;
    }

    public String getTempDir() {
        return tempDir;
    }

    public PBParserOptions setTempDir(String tempDir) {
        this.tempDir = tempDir;
        return this;
    }

    public String getLineSeparator() {
        return lineSeparator;
    }

    public PBParserOptions setLineSeparator(String lineSeparator) {
        this.lineSeparator = lineSeparator;
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

    public boolean isMethodDefCtx() {
        return methodDefCtx;
    }

    public PBParserOptions setMethodDefCtx(boolean methodDefCtx) {
        this.methodDefCtx = methodDefCtx;
        return this;
    }

    public boolean isUseCompleteStage() {
        return useCompleteStage;
    }

    public PBParserOptions setUseCompleteStage(boolean useCompleteStage) {
        this.useCompleteStage = useCompleteStage;
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

    public Map<String, String> getDefOptions() {
        return defOptions;
    }

    public PBParserOptions setDefOptions(Map<String, String> defOptions) {
        this.defOptions = defOptions;
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