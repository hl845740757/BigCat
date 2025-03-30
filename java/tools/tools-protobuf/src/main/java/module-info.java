/**
 * @author wjybxx
 * date - 2024/5/21
 */
module wjybxx.bigcattools.protobuf {
    requires jsr305;
    requires it.unimi.dsi.fastutil.core;
    requires org.apache.commons.io;
    requires org.apache.commons.lang3;

    requires java.compiler;
    requires com.squareup.javapoet;

    requires wjybxx.bigcattools.common;
    requires wjybxx.commons.base;
    requires wjybxx.commons.concurrent;
    requires wjybxx.dson.core;

    exports cn.wjybxx.bigcattools.protobuf;
    exports cn.wjybxx.bigcattools.protobuf.gen;
}