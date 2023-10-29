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

import cn.wjybxx.common.tools.util.Utils;

import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/10/9
 */
public class PBKeywords {

    /** 可选项 */
    public static final String OPTION = "option";
    /** 导入文件 */
    public static final String IMPORT = "import";
    /** 导入传递（依赖传递） */
    public static final String PUBLIC = "public";

    /** 可选字段 */
    public static final String OPTIONAL = "optional";
    /** 数组字段 */
    public static final String REPEATED = "repeated";
    /** 必要字段 */
    public static final String REQUIRED = "required";

    /** rpc服务 */
    public static final String SERVICE = "service";
    /** 结构体 */
    public static final String MESSAGE = "message";
    /** 枚举 */
    public static final String ENUM = "enum";
    /** rpc方法 */
    public static final String RPC = "rpc";

    // region file

    /** 语法 */
    public static final String SYNTAX = "syntax";
    /** 生成代码优化项 */
    public static final String OPTIMIZE_FOR = "optimize_for";

    /** 生成代码的包名 */
    public static final String PACKAGE = "package";
    /**
     * 生成的java文件的包名
     * 如果未配置，由解析器赋予默认值 -- 通常是固定值
     */
    public static final String JAVA_PACKAGE = "java_package";
    /**
     * 生成的java文件的外部类类名
     * 注意：
     * 1.如果未配置，由解析器赋予默认值 -- 通常建议根据文件名生成，eg：{@code bag.proto => MsgBag}
     * 2.Rpc服务生成类为顶层类，不使用该属性
     */
    public static final String JAVA_OUTER_CLASSNAME = "java_outer_classname";
    /**
     * 是否将顶级消息、枚举、和服务定义在包级，而不是在以 .proto 文件命名的外部类中
     */
    public static final String JAVA_MULTIPLE_FILES = "java_multiple_files";
    /**
     * 导出java时是否是导出rpc服务
     * 我们不使用protobuf的GRPC，因此不会导出 -- 我们在预处理文件时会关闭service的代码生成
     */
    public static final String JAVA_GENERIC_SERVICES = "java_generic_services";

    /**
     * csharp命名空间
     */
    public static final String CSHARP_NAMESPACE = "csharp_namespace";

    // endregion

    // region type

    /**
     * 是否允许不同的枚举常量指向同一个值
     */
    public static final String ALLOW_ALIAS = "allow_alias";
    /** 保留字段编号 */
    public static final String RESERVED = "reserved";

    // endregion

    private static final Map<String, Boolean> STRING_OPTION_VALUE_KEYWORDS = Map.of();

    /** 是否是字符串可选项值 */
    public static boolean isStringOptionValue(String name) {
        // 特殊值
        Boolean val = STRING_OPTION_VALUE_KEYWORDS.get(name);
        if (val != null) {
            return val;
        }
        // 规则值
        return name.endsWith("package")
                || name.endsWith("namespace")
                || name.endsWith("name") // 包含classname
                || name.endsWith("prefix")
                || name.endsWith("comments");
    }

    /** 纠正选项值的引号 */
    public static String correctQuoteForOptionValue(String name, String value) {
        return isStringOptionValue(name) ? Utils.quote(value) : Utils.unquote(value);
    }

}