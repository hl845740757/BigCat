/**
 * @author wjybxx
 * date - 2024/5/21
 */
module wjybxx.bigcattools.common {
    requires jsr305;
    requires it.unimi.dsi.fastutil.core;
    requires org.apache.commons.lang3;
    requires org.apache.commons.io;
    requires com.google.guice;

    requires com.squareup.javapoet;
    requires java.compiler;

    requires wjybxx.commons.base;
    requires wjybxx.commons.concurrent;
    requires wjybxx.dson.core;
    requires wjybxx.dson.codec;

    exports cn.wjybxx.bigcattools.common;
    exports cn.wjybxx.bigcattools.common.io;
    exports cn.wjybxx.bigcattools.common.props;
}