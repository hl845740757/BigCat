/**
 * @author houlei
 * date - 2024/5/21
 */
module wjybxx.bigcat.core {
    requires jsr305;
    requires org.slf4j;
    requires it.unimi.dsi.fastutil.core;
    requires org.apache.commons.lang3;
    requires org.apache.commons.codec;
//    requires com.google.protobuf; // 4.26
    requires protobuf.java;
    requires com.google.guice;
    requires static java.compiler; // 生成代码的注解引用

    requires transitive wjybxx.commons.base;
    requires transitive wjybxx.commons.disruptor;
    requires transitive wjybxx.commons.concurrent;
    requires transitive wjybxx.dson.core;
    requires transitive wjybxx.dson.codec;

    exports cn.wjybxx.bigcat;
    exports cn.wjybxx.bigcat.annotation;
    exports cn.wjybxx.bigcat.eventbus;
    exports cn.wjybxx.bigcat.fx;
    exports cn.wjybxx.bigcat.rpc;
    exports cn.wjybxx.bigcat.util;

    opens cn.wjybxx.bigcat;
    opens cn.wjybxx.bigcat.annotation;
    opens cn.wjybxx.bigcat.eventbus;
    opens cn.wjybxx.bigcat.fx;
    opens cn.wjybxx.bigcat.rpc;
    opens cn.wjybxx.bigcat.util;
}