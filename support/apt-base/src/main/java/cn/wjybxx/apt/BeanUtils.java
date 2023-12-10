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

package cn.wjybxx.apt;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.lang.model.element.*;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Elements;
import javax.lang.model.util.Types;
import java.util.List;
import java.util.stream.Collectors;

/**
 * java bean 工具类
 *
 * @author wjybxx
 * date 2023/4/6
 */
public class BeanUtils {

    /**
     * 判断一个类是否包含无参构造方法
     */
    public static boolean containsNoArgsConstructor(TypeElement typeElement) {
        return getNoArgsConstructor(typeElement) != null;
    }

    public static boolean containsOneArgsConstructor(Types typeUtils, TypeElement typeElement, TypeMirror argType) {
        return getOneArgsConstructor(typeUtils, typeElement, argType) != null;
    }

    /**
     * 查找无参构造方法
     */
    @Nullable
    public static ExecutableElement getNoArgsConstructor(TypeElement typeElement) {
        return typeElement.getEnclosedElements().stream()
                .filter(e -> e.getKind() == ElementKind.CONSTRUCTOR)
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getParameters().size() == 0)
                .findFirst()
                .orElse(null);
    }

    /**
     * 查找单参数的构造方法
     */
    @Nullable
    public static ExecutableElement getOneArgsConstructor(Types typeUtils, TypeElement typeElement, TypeMirror argType) {
        return typeElement.getEnclosedElements().stream()
                .filter(e -> e.getKind() == ElementKind.CONSTRUCTOR)
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), argType))
                .findFirst()
                .orElse(null);
    }

    /**
     * 获取类的所有字段和方法，包含继承得到的字段和方法
     * 注意：不包含接口和静态字段。
     * <p>
     * {@link Elements#getAllMembers(TypeElement)}只包含父类的公共属性，不包含私有的东西 -- 因此不能使用。
     */
    public static List<Element> getAllFieldsAndMethodsWithInherit(TypeElement typeElement) {
        final List<TypeElement> flatInherit = AptUtils.flatInheritAndReverse(typeElement);
        return flatInherit.stream()
                .flatMap(e -> e.getEnclosedElements().stream())
                .filter(e -> e.getKind() == ElementKind.METHOD || e.getKind() == ElementKind.FIELD)
                .collect(Collectors.toList());
    }

    public static List<Element> getAllFieldsWithInherit(TypeElement typeElement) {
        final List<TypeElement> flatInherit = AptUtils.flatInheritAndReverse(typeElement);
        return flatInherit.stream()
                .flatMap(e -> e.getEnclosedElements().stream())
                .filter(e -> e.getKind() == ElementKind.FIELD)
                .collect(Collectors.toList());
    }

    /**
     * 获取类所有的方法，包含继承得到的方法
     * {@link Elements#getAllMembers(TypeElement)}只包含父类的公共属性，不包含私有的东西。
     * 注意：
     * 1. 不包含接口中的方法。
     * 2. 包含静态方法
     */
    public static List<ExecutableElement> getAllMethodsWithInherit(TypeElement typeElement) {
        final List<TypeElement> flatInherit = AptUtils.flatInheritAndReverse(typeElement);
        return flatInherit.stream()
                .flatMap(e -> e.getEnclosedElements().stream())
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .collect(Collectors.toList());
    }

    /**
     * 是否包含非private的setter方法
     */
    public static boolean containsNotPrivateSetter(Types typeUtils, VariableElement variableElement,
                                                   List<? extends Element> allFieldsAndMethodWithInherit) {
        return findNotPrivateSetter(typeUtils, variableElement, allFieldsAndMethodWithInherit) != null;
    }

    /**
     * 是否包含非private的getter方法
     */
    public static boolean containsNotPrivateGetter(Types typeUtils, VariableElement variableElement,
                                                   List<? extends Element> allFieldsAndMethodWithInherit) {
        return findNotPrivateGetter(typeUtils, variableElement, allFieldsAndMethodWithInherit) != null;
    }

    /**
     * 该方法会查询标准的setter命名，同时会查询{@code set + firstCharToUpperCase(fieldName)}格式的命名
     */
    public static ExecutableElement findNotPrivateSetter(Types typeUtils, VariableElement variableElement,
                                                         List<? extends Element> allFieldsAndMethodWithInherit) {
        final String fieldName = variableElement.getSimpleName().toString();
        final String setterMethodName = BeanUtils.setterMethodName(fieldName, AptUtils.isPrimitiveBoolean(variableElement.asType()));
        final String setterMethodName2 = "set" + BeanUtils.firstCharToUpperCase(fieldName);
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE))
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> {
                    String mName = e.getSimpleName().toString();
                    return mName.equals(setterMethodName) || mName.equals(setterMethodName2);
                })
                .findFirst()
                .orElse(null);
        // 我们去掉了参数类型测试，用户不应该出现setter同名却干别的事情的方法
        //    .anyMatch(e -> AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), variableElement.asType()));
    }

    /**
     * 该方法会查询标准的getter命名，同时会查询{@code get + firstCharToUpperCase(fieldName)}格式的命名
     */
    public static ExecutableElement findNotPrivateGetter(Types typeUtils, VariableElement variableElement,
                                                         List<? extends Element> allFieldsAndMethodWithInherit) {
        final String fieldName = variableElement.getSimpleName().toString();
        final String getterMethodName = BeanUtils.getterMethodName(fieldName, AptUtils.isPrimitiveBoolean(variableElement.asType()));
        final String getterMethodName2 = "get" + BeanUtils.firstCharToUpperCase(fieldName);
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE))
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getParameters().size() == 0)
                .filter(e -> {
                    String mName = e.getSimpleName().toString();
                    return mName.equals(getterMethodName) || mName.equals(getterMethodName2);
                })
                .findFirst()
                .orElse(null);
        // 我们去掉了参数类型测试，用户不应该出现getter同名却干别的事情的方法
        //       .anyMatch(e -> AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, e.getReturnType(), variableElement.asType()));
    }

    /**
     * 首字符大写
     *
     * @param str content
     * @return 首字符大写的字符串
     */
    public static String firstCharToUpperCase(@Nonnull String str) {
        if (str.length() == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        if (Character.isLowerCase(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb.setCharAt(0, Character.toUpperCase(firstChar));
            return sb.toString();
        }
        return str;
    }

    /**
     * 首字母小写
     *
     * @param str content
     * @return 首字符小写的字符串
     */
    public static String firstCharToLowerCase(@Nonnull String str) {
        if (str.length() == 0) {
            return str;
        }
        char firstChar = str.charAt(0);
        if (Character.isUpperCase(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb.setCharAt(0, Character.toLowerCase(firstChar));
            return sb.toString();
        }
        return str;
    }

    /**
     * 获取标准getter方法的名字
     *
     * @param filedName          字段名字
     * @param isPrimitiveBoolean 是否是bool值 - 坑太多了，只有基本类型的boolean才会变成is，包装类型的不会
     * @return 方法名
     */
    public static String getterMethodName(String filedName, boolean isPrimitiveBoolean) {
        if (isFirstOrSecondCharUpperCase(filedName)) {
            // 这里参数名一定不是is开头
            // 前两个字符任意一个大写，则参数名直接拼在get/is后面
            if (isPrimitiveBoolean) {
                return "is" + filedName;
            } else {
                return "get" + filedName;
            }
        }
        // 到这里前两个字符都是小写
        if (isPrimitiveBoolean) {
            // 如果参数名以 is 开头，则直接返回，否则 is + 首字母大写 - is 还要特殊处理
            if (filedName.length() > 2 && filedName.startsWith("is")) {
                return filedName;
            } else {
                return "is" + firstCharToUpperCase(filedName);
            }
        } else {
            return "get" + firstCharToUpperCase(filedName);
        }
    }

    /**
     * 获取标准setter方法的名字
     *
     * @param filedName 字段名字
     * @return 方法名
     */
    public static String setterMethodName(String filedName, boolean isPrimitiveBoolean) {
        if (isFirstOrSecondCharUpperCase(filedName)) {
            // 这里参数名一定不是is开头
            // 前两个字符任意一个大写，则参数名直接拼在set后面
            return "set" + filedName;

        }
        // 到这里前两个字符都是小写 - is 还要特殊处理。
        if (isPrimitiveBoolean) {
            if (filedName.length() > 2 && filedName.startsWith("is")) {
                return "set" + firstCharToUpperCase(filedName.substring(2));
            } else {
                return "set" + firstCharToUpperCase(filedName);
            }
        } else {
            return "set" + firstCharToUpperCase(filedName);
        }
    }

    /**
     * 查询名字的第一个或第二个字符是否是大写
     */
    private static boolean isFirstOrSecondCharUpperCase(String name) {
        if (name.length() < 1) {
            return false;
        }
        // 下划线开头的也需要特殊处理...
        if (Character.isUpperCase(name.charAt(0)) || name.charAt(0) == '_') {
            return true;
        }
        if (name.length() > 1 && Character.isUpperCase(name.charAt(1))) {
            return true;
        }
        return false;
    }

}