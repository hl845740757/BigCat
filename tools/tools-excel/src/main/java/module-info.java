/**
 * @author wjybxx
 * date - 2024/5/21
 */
module wjybxx.bigcattools.excel {
    requires jsr305;
    requires org.slf4j;
    requires it.unimi.dsi.fastutil.core;
    requires org.apache.commons.lang3;
    requires org.apache.commons.io;

    requires poi;
    requires xlsx.streamer;
    requires com.squareup.javapoet;
    requires java.compiler;

    requires wjybxx.bigcattools.common;
    requires wjybxx.bigcattools.config;
    requires wjybxx.commons.base;
    requires wjybxx.dson.core;
    requires wjybxx.dson.codec;

    exports cn.wjybxx.bigcattools.excel;
    exports cn.wjybxx.bigcattools.excel.gen;
}