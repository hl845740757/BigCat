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

package cn.wjybxx.common.apt;

import com.squareup.javapoet.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.processing.AbstractProcessor;
import javax.annotation.processing.Filer;
import javax.annotation.processing.Generated;
import javax.annotation.processing.Messager;
import javax.lang.model.SourceVersion;
import javax.lang.model.element.*;
import javax.lang.model.type.*;
import javax.lang.model.util.Elements;
import javax.lang.model.util.SimpleAnnotationValueVisitor8;
import javax.lang.model.util.SimpleTypeVisitor8;
import javax.lang.model.util.Types;
import javax.tools.Diagnostic;
import java.io.IOException;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/6
 */
public class AptUtils {

    public static final SourceVersion SOURCE_VERSION = SourceVersion.RELEASE_17;
    public static final Modifier[] PUBLIC_STATIC = {Modifier.PUBLIC, Modifier.STATIC};
    public static final Modifier[] PUBLIC_STATIC_FINAL = {Modifier.PUBLIC, Modifier.STATIC, Modifier.FINAL};
    public static final Modifier[] PRIVATE_STATIC_FINAL = {Modifier.PRIVATE, Modifier.STATIC, Modifier.FINAL};
    /**
     * 由于生成的代码不能很好的处理泛型等信息，因此需要抑制警告
     */
    public static final AnnotationSpec SUPPRESS_UNCHECKED_ANNOTATION = AnnotationSpec.builder(SuppressWarnings.class)
            .addMember("value", "{\"unchecked\", \"rawtypes\"}")
            .build();

    public static final AnnotationSpec NONNULL_ANNOTATION = AnnotationSpec.builder(Nonnull.class)
            .build();

    public static final ClassName CLASS_NAME_SOURCE_REF = ClassName.get("cn.wjybxx.common.annotations", "SourceFileRef");

    private AptUtils() {

    }

    /**
     * 为生成代码的注解处理器创建一个通用注解
     *
     * @param processorType 注解处理器
     * @return 代码生成信息注解
     */
    public static AnnotationSpec newProcessorInfoAnnotation(Class<? extends AbstractProcessor> processorType) {
        return AnnotationSpec.builder(Generated.class)
                .addMember("value", "$S", processorType.getCanonicalName())
                .build();
    }

    public static AnnotationSpec newSourceFileRefAnnotation(TypeName sourceFileTypeName) {
        return AnnotationSpec.builder(CLASS_NAME_SOURCE_REF)
                .addMember("value", "$T.class", sourceFileTypeName)
                .build();
    }

    /**
     * 筛选出java源文件 - 去除带有注解的class文件
     *
     * @param typeElementSet 带有注解的所有元素
     * @param elementUtils   用于获取元素的完整类名
     * @return 过滤后只有java源文件的元素
     */
    public static Set<TypeElement> selectSourceFile(Set<TypeElement> typeElementSet, Elements elementUtils) {
        return typeElementSet.stream()
                .filter(e -> isSourceFile(elementUtils, e))
                .collect(Collectors.toSet());
    }

    private static boolean isSourceFile(Elements elementUtils, TypeElement typeElement) {
        try {
            // 如果注解的保留策略是runtime，则会把已经编译成class的文件再统计进来，这里需要过滤。
            // 不能使用getSystemClassLoader()，会加载不到。
            Class.forName(elementUtils.getBinaryName(typeElement).toString(), false, null);
            return false;
        } catch (Exception ignore) {
            return true;
        }
    }

    // ----------------------------------------------------- 分割线 -----------------------------------------------

    /**
     * 复制一个方法信息，当然不包括代码块。
     *
     * @param method 方法信息
     * @return builder
     */
    public static MethodSpec.Builder copyMethod(@Nonnull ExecutableElement method) {
        final String methodName = method.getSimpleName().toString();
        final MethodSpec.Builder builder = MethodSpec.methodBuilder(methodName);

        // 访问修饰符
        copyModifiers(builder, method);
        // 泛型变量
        copyTypeVariables(builder, method);
        // 返回值类型
        copyReturnType(builder, method);
        // 方法参数
        copyParameters(builder, method);
        // 异常信息
        copyExceptionsTable(builder, method);
        // 是否是变长参数类型
        builder.varargs(method.isVarArgs());

        return builder;
    }

    /**
     * 拷贝一个方法的修饰符
     */
    public static void copyModifiers(MethodSpec.Builder builder, @Nonnull ExecutableElement method) {
        builder.addModifiers(method.getModifiers());
    }

    /**
     * 拷贝一个方法的所有泛型参数
     */
    public static void copyTypeVariables(MethodSpec.Builder builder, ExecutableElement method) {
        for (TypeParameterElement typeParameterElement : method.getTypeParameters()) {
            TypeVariable var = (TypeVariable) typeParameterElement.asType();
            builder.addTypeVariable(TypeVariableName.get(var));
        }
    }

    /**
     * 拷贝返回值类型
     */
    public static void copyReturnType(MethodSpec.Builder builder, @Nonnull ExecutableElement method) {
        builder.returns(TypeName.get(method.getReturnType()));
    }

    /**
     * 拷贝一个方法的所有参数
     */
    public static void copyParameters(MethodSpec.Builder builder, ExecutableElement method) {
        copyParameters(builder, method.getParameters());
    }

    /**
     * 拷贝这些方法参数
     */
    public static void copyParameters(MethodSpec.Builder builder, List<? extends VariableElement> parameters) {
        for (VariableElement parameter : parameters) {
            builder.addParameter(ParameterSpec.get(parameter));
        }
    }

    /**
     * 拷贝一个方法的异常表
     */
    public static void copyExceptionsTable(MethodSpec.Builder builder, ExecutableElement method) {
        for (TypeMirror thrownType : method.getThrownTypes()) {
            builder.addException(TypeName.get(thrownType));
        }
    }

    // ----------------------------------------------------- 分割线 -----------------------------------------------

    /**
     * 通过名字查找对应的方法
     */
    public static ExecutableElement findMethodByName(TypeElement typeElement, String methodName) {
        return typeElement.getEnclosedElements()
                .stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getSimpleName().toString().equals(methodName))
                .findFirst()
                .orElse(null);
    }

    /**
     * 通过名字查找对应的字段
     */
    public static VariableElement findFieldByName(TypeElement typeElement, String field) {
        return typeElement.getEnclosedElements()
                .stream()
                .filter(e -> e.getKind() == ElementKind.FIELD)
                .map(e -> (VariableElement) e)
                .filter(e -> e.getSimpleName().toString().equals(field))
                .findFirst()
                .orElse(null);
    }

    /**
     * 将继承体系展开，不包含实现的接口。
     * （超类在后）
     */
    public static List<TypeElement> flatInherit(TypeElement typeElement) {
        assert typeElement.getKind() == ElementKind.CLASS;
        final List<TypeElement> result = new ArrayList<>();
        result.add(typeElement);

        for (TypeMirror typeMirror = typeElement.getSuperclass(); typeMirror.getKind() != TypeKind.NONE; ) {
            final DeclaredType declaredType = (DeclaredType) typeMirror;
            final TypeElement parentTypeElement = (TypeElement) (declaredType.asElement());
            result.add(parentTypeElement);
            typeMirror = parentTypeElement.getSuperclass();
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前）
     */
    public static List<TypeElement> flatInheritAndReverse(TypeElement typeElement) {
        final List<TypeElement> result = flatInherit(typeElement);
        Collections.reverse(result);
        return result;
    }

    /** 注意：无法保证顺序 -- 这个方法还不太稳定 */
    public static List<TypeMirror> findAllInterfaces(Types typeUtil, Elements elementUtil, TypeElement typeElement) {
        // 避免每次都查找
        TypeMirror objectTypeMirror = getTypeMirrorOfClass(elementUtil, Object.class);
        List<TypeMirror> result = new ArrayList<>();
        for (TypeMirror sup : typeElement.getInterfaces()) { // 这里不能遍历result，迭代的时候会变化
            result.add(sup); // 直接接口一定是不重复的
            recursiveFindInterfaces(typeUtil, objectTypeMirror, result, sup);
        }
        return result;
    }

    private static void recursiveFindInterfaces(Types typeUtil, TypeMirror objectMirror, List<TypeMirror> typeMirrors, TypeMirror current) {
        // 这里要过滤Object
        for (TypeMirror sup : typeUtil.directSupertypes(current)) {
            if (isSameTypeIgnoreTypeParameter(typeUtil, objectMirror, sup)) {
                continue;
            }
            if (containsTypeMirror(typeUtil, typeMirrors, sup)) {
                continue;
            }
            typeMirrors.add(sup);
            recursiveFindInterfaces(typeUtil, objectMirror, typeMirrors, sup);
        }
    }

    private static boolean containsTypeMirror(Types typeUtil, List<TypeMirror> typeMirrors, TypeMirror typeMirror) {
        for (TypeMirror exist : typeMirrors) {
            if (isSameTypeIgnoreTypeParameter(typeUtil, typeMirror, exist)) {
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------- 分割线 -----------------------------------------------

    /**
     * 查找指定注解是否出现
     */
    public static boolean isAnnotationPresent(Types typeUtils, Element element, TypeMirror targetAnnotationMirror) {
        return findAnnotation(typeUtils, element, targetAnnotationMirror)
                .isPresent();
    }

    /**
     * 查找出现的第一个注解，不包含继承的部分
     */
    public static Optional<? extends AnnotationMirror> findAnnotation(Types typeUtils,
                                                                      Element element, TypeMirror targetAnnotationMirror) {
        // 查找该字段上的注解
        return element.getAnnotationMirrors().stream()
                .filter(annotationMirror -> typeUtils.isSameType(annotationMirror.getAnnotationType(), targetAnnotationMirror))
                .findFirst();
    }

    /**
     * 查找出现的第一个注解，包含继承的注解
     */
    public static Optional<? extends AnnotationMirror> findAnnotationWithInherit(Types typeUtils, Elements elementUtils,
                                                                                 Element element, TypeMirror targetAnnotationMirror) {
        // 查找该字段上的注解
        return elementUtils.getAllAnnotationMirrors(element).stream()
                .filter(annotationMirror -> typeUtils.isSameType(annotationMirror.getAnnotationType(), targetAnnotationMirror))
                .findFirst();
    }

    /**
     * 获取注解上的某一个属性的值，不包含default值
     */
    @Nullable
    public static AnnotationValue getAnnotationValue(AnnotationMirror annotationMirror, String propertyName) {
        return annotationMirror.getElementValues().entrySet().stream()
                .filter(entry -> entry.getKey().getSimpleName().toString().equals(propertyName))
                .map(Map.Entry::getValue)
                .findFirst()
                .orElse(null);
    }

    /**
     * 获取注解上的某一个属性的值，包含default值
     */
    @Nonnull
    public static AnnotationValue getAnnotationValueWithDefaults(Elements elementUtils, AnnotationMirror annotationMirror, String propertyName) {
        return elementUtils.getElementValuesWithDefaults(annotationMirror).entrySet().stream()
                .filter(entry -> entry.getKey().getSimpleName().toString().equals(propertyName))
                .map(Map.Entry::getValue)
                .findFirst()
                .orElseThrow();
    }

    /**
     * 获取注解上的某一个属性的值，不包含default值
     * <pre>{@code
     *      @SuppressWarnings({"unchecked", "rawtypes"}) => {"unchecked", "rawtypes"}
     * }
     * </pre>
     *
     * @param annotationMirror 注解编译信息
     * @param propertyName     属性的名字
     * @return object
     */
    @SuppressWarnings("unchecked")
    @Nullable
    public static <T> T getAnnotationValueValue(AnnotationMirror annotationMirror, String propertyName) {
        return (T) Optional.ofNullable(getAnnotationValue(annotationMirror, propertyName))
                .map(AnnotationValue::getValue)
                .orElse(null);
    }

    @SuppressWarnings("unchecked")
    public static <T> T getAnnotationValueValue(AnnotationMirror annotationMirror, String propertyName, T def) {
        return (T) Optional.ofNullable(getAnnotationValue(annotationMirror, propertyName))
                .map(AnnotationValue::getValue)
                .orElse(def);
    }

    /**
     * 获取注解上的某一个属性的值，包含default值
     *
     * @param annotationMirror 注解编译信息
     * @param propertyName     属性的名字
     * @return object
     */
    @SuppressWarnings("unchecked")
    @Nonnull
    public static <T> T getAnnotationValueValueWithDefaults(Elements elementUtils, AnnotationMirror annotationMirror, String propertyName) {
        return (T) Optional.of(getAnnotationValueWithDefaults(elementUtils, annotationMirror, propertyName))
                .map(AnnotationValue::getValue)
                .orElseThrow();
    }

    /** 将注解属性转换为 name -> AnnotationValue 的Map */
    public static Map<String, AnnotationValue> getAnnotationValuesMap(AnnotationMirror annotationMirror) {
        final HashMap<String, AnnotationValue> r = new HashMap<>();
        for (var entry : annotationMirror.getElementValues().entrySet()) {
            final String name = entry.getKey().getSimpleName().toString();
            final AnnotationValue value = entry.getValue();
            r.put(name, value);
        }
        return r;
    }

    /**
     * 获取注解中引用的class对象的类型
     * eg:
     * <pre>{@code
     *      @AutoService(Processor.class) => AnnotationMirror(javax.annotation.processing.Processor)
     * }
     * </pre>
     */
    public static TypeMirror getAnnotationValueTypeMirror(AnnotationValue annotationValue) {
        return annotationValue.accept(new SimpleAnnotationValueVisitor8<>() {
            @Override
            public TypeMirror visitType(TypeMirror t, Object o) {
                return t;
            }
        }, null);
    }

    /**
     * 忽略两个类型的泛型参数，判断第一个是否是第二个的子类型。
     * 如果考虑泛型的话，会存在区别
     */
    public static boolean isSubTypeIgnoreTypeParameter(Types typeUtil, TypeMirror first, TypeMirror second) {
        return typeUtil.isSubtype(typeUtil.erasure(first), typeUtil.erasure(second));
    }

    /**
     * 忽略两个类型的泛型参数，判断第一个和第二个类型是否相同
     * 如果考虑泛型的话，会存在区别
     */
    public static boolean isSameTypeIgnoreTypeParameter(Types typeUtil, TypeMirror first, TypeMirror second) {
        return typeUtil.isSameType(typeUtil.erasure(first), typeUtil.erasure(second));
    }

    /**
     * 获取一个元素的声明类型
     */
    @Nullable
    public static DeclaredType findDeclaredType(TypeMirror typeMirror) {
        return typeMirror.accept(new SimpleTypeVisitor8<>() {
            @Override
            public DeclaredType visitDeclared(DeclaredType t, Object o) {
                return t;
            }

            @Override
            protected DeclaredType defaultAction(TypeMirror e, Object o) {
                return null;
            }
        }, null);
    }

    public static DeclaredType getRawDeclaredType(Types tpeUtils, Elements elementUtils, Class<?> clazz) {
        return (DeclaredType) tpeUtils.erasure(elementUtils.getTypeElement(clazz.getCanonicalName()).asType());
    }

    public static TypeElement getTypeElementOfClass(Elements elementUtils, Class<?> clazz) {
        return elementUtils.getTypeElement(clazz.getCanonicalName());
    }

    public static TypeMirror getTypeMirrorOfClass(Elements elementUtils, Class<?> clazz) {
        return elementUtils.getTypeElement(clazz.getCanonicalName()).asType();
    }

    /**
     * 是否是基本类型的boolean
     */
    public static boolean isPrimitiveBoolean(TypeMirror typeMirror) {
        return typeMirror.getKind() == TypeKind.BOOLEAN;
    }

    public static boolean isArrayType(TypeMirror typeMirror) {
        return typeMirror.getKind() == TypeKind.ARRAY;
    }

    public static TypeMirror getComponentType(TypeMirror typeMirror) {
        assert isArrayType(typeMirror);
        ArrayType arrayType = (ArrayType) typeMirror;
        return arrayType.getComponentType();
    }

    /**
     * 判断一个对象是否是字节数组
     */
    public static boolean isByteArray(TypeMirror typeMirror) {
        if (typeMirror.getKind() == TypeKind.ARRAY) {
            ArrayType arrayType = (ArrayType) typeMirror;
            return arrayType.getComponentType().getKind() == TypeKind.BYTE;
        }
        return false;
    }

    /**
     * 是否是指定基本类型数组
     *
     * @param typeMirror    类型信息
     * @param primitiveType 基本类型
     * @return true/false
     */
    public static boolean isTargetPrimitiveArrayType(TypeMirror typeMirror, TypeKind primitiveType) {
        return typeMirror.accept(new SimpleTypeVisitor8<Boolean, Void>() {

            @Override
            public Boolean visitArray(ArrayType t, Void aVoid) {
                return t.getComponentType().getKind() == primitiveType;
            }

            @Override
            protected Boolean defaultAction(TypeMirror e, Void aVoid) {
                return false;
            }

        }, null);
    }

    /**
     * 获取第一个泛型参数
     */
    @Nullable
    public static TypeMirror findFirstTypeParameter(TypeMirror typeMirror) {
        return typeMirror.accept(new SimpleTypeVisitor8<TypeMirror, Void>() {
            @Override
            public TypeMirror visitDeclared(DeclaredType t, Void aVoid) {
                if (t.getTypeArguments().size() == 0) {
                    // 未声明泛型参数
                    return null;
                } else {
                    return t.getTypeArguments().get(0);
                }
            }

            @Override
            protected TypeMirror defaultAction(TypeMirror e, Void aVoid) {
                return null;
            }

        }, null);
    }

    /**
     * 将当前类型转型到目标超类型
     * 用于获取当前类型传递给超类型的一些信息
     *
     * @param self   当前类型
     * @param target 目标类型
     */
    public static DeclaredType upwardToSuperTypeMirror(Types typeUtils, TypeMirror self, TypeMirror target) {
        if (isSameTypeIgnoreTypeParameter(typeUtils, self, target)) {
            return (DeclaredType) self;
        }
        final List<? extends TypeMirror> directSupertypes = typeUtils.directSupertypes(self);
        for (TypeMirror typeMirror : directSupertypes) {
            if (isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, target)) {
                return (DeclaredType) typeMirror;
            }
            if (isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, target)) {
                return upwardToSuperTypeMirror(typeUtils, typeMirror, target);
            }
        }
        throw new IllegalArgumentException(String.format("self: %s, target: %s", self, target));
    }
    // ------------------------------------------ 分割线 ------------------------------------------------

    /**
     * 根据原类型，生成获得对应的辅助类的类名
     * 对于内部类，生成的类为：外部类名_内部类名
     *
     * @param suffix 后缀
     */
    public static String getProxyClassName(Elements elementUtils, TypeElement typeElement, final String suffix) {
        if (typeElement.getEnclosingElement().getKind() == ElementKind.PACKAGE) {
            return typeElement.getSimpleName().toString() + suffix;
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            // Q: 为什么不使用$符合?
            // A: 因为生成的工具类都是外部类，不是内部类。
            final String packageName = elementUtils.getPackageOf(typeElement).getQualifiedName().toString();
            final String fullName = typeElement.getQualifiedName().toString();
            final String uniqueName = fullName.substring(packageName.length() + 1).replace(".", "_");
            return uniqueName + suffix;
        }
    }

    /**
     * @param originTypeElement 原始类文件，用于获取包名，以及打印错误
     */
    public static void writeToFile(final TypeElement originTypeElement, final TypeSpec.Builder typeBuilder,
                                   final Elements elementUtils, final Messager messager, final Filer filer) {
        final TypeSpec typeSpec = typeBuilder.build();
        final JavaFile javaFile = JavaFile
                .builder(getPackageName(originTypeElement, elementUtils), typeSpec)
                // 不用导入java.lang包
                .skipJavaLangImports(true)
                // 4空格缩进
                .indent("    ")
                .build();
        try {
            // 输出到processingEnv.getFiler()会立即参与编译
            // 如果自己指定路径，可以生成源码到指定路径，但是可能无法被编译器检测到，本轮无法参与编译，需要再进行一次编译
            javaFile.writeTo(filer);
        } catch (IOException e) {
            messager.printMessage(Diagnostic.Kind.ERROR, getStackTrace(e), originTypeElement);
        }
    }

    /**
     * 获取一个类或接口所属的包名
     */
    private static String getPackageName(TypeElement typeElement, Elements elementUtils) {
        return elementUtils.getPackageOf(typeElement).getQualifiedName().toString();
    }

    //

    /**
     * 获取堆栈信息，避免引入commons-lang3
     */
    public static String getStackTrace(Throwable throwable) {
        final StringWriter sw = new StringWriter();
        final PrintWriter pw = new PrintWriter(sw, true);
        throwable.printStackTrace(pw);
        return sw.getBuffer().toString();
    }

    public static boolean isEmpty(String v) {
        return v == null || v.isEmpty();
    }

    public static boolean isBlank(String v) {
        return v == null || v.isBlank();
    }

}