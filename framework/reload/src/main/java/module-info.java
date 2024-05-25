/**
 * @author houlei
 * date - 2024/5/21
 */
module wjybxx.bigcat.reload {
    requires jsr305;
    requires org.slf4j;
    requires com.google.common;
    requires org.apache.commons.io;

    requires wjybxx.bigcat.core;
    requires wjybxx.commons.base;
    requires wjybxx.commons.concurrent;

    exports cn.wjybxx.bigcat.reload;

    opens cn.wjybxx.bigcat.reload;
}