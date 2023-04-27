package cn.wjybxx.common.dson.codec;

/**
 * 类型id的写入策略
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public enum ClassIdPolicy {

    /**
     * 当对象的运行时类型和声明类型相同时不写入类型信息
     * 通常我们的字段类型定义是明确的，因此可以升到大量不必要的类型信息。
     * 如果仅用于java语言，建议使用该模式。
     */
    OPTIMIZED,

    /**
     * 总是写入对象的类型信息，无论运行时类型与声明类型是否相同
     * 这种方式有更好的兼容性，对跨语言友好，因为目标语言可能没有泛型，或没有注解处理器生成辅助代码等
     */
    ALWAYS,

    /**
     * 总是不写入对象的类型信息，无论运行时类型与声明类型是否相同
     * 注意：不写入的情况下只是会写为空id（比如空字符串或无效命名空间）
     */
    NONE,

    ;

    public boolean test(Class<?> declared, Class<?> encodeClass) {
        return switch (this) {
            case NONE -> false;
            case ALWAYS -> true;
            case OPTIMIZED -> encodeClass != declared;
        };
    }

}