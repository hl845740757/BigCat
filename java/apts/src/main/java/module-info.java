
import cn.wjybxx.bigcat.apt.AutoFieldsProcessor;
import cn.wjybxx.bigcat.apt.SubscribeProcessor;
import cn.wjybxx.bigcat.apt.rpc.RpcServiceProcessor;

import javax.annotation.processing.Processor;

/**
 * @author houlei
 * date - 2024/5/20
 */
module wjybxx.bigcat.apt {
    requires jsr305;
    requires com.squareup.javapoet;
    requires com.google.auto.service;
    requires wjybxx.commons.aptbase;

    exports cn.wjybxx.bigcat.apt;
    exports cn.wjybxx.bigcat.apt.rpc;

    provides Processor with SubscribeProcessor,
            AutoFieldsProcessor,
            RpcServiceProcessor;

}