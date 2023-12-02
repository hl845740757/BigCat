package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 在子节点完成之后仍返回运行。
 * 注意：在运行期间只运行一次子节点
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class AlwaysRunning<E> extends Decorator<E> {

    @Override
    protected void execute() {
        if (isExecuteTriggeredByEnter()) {
            if (child != null) {
                int reentryId = getReentryId();
                template_runChild(child);
                if (isExited(reentryId)) { // 被取消
                    return;
                }
            }
            setRunning(); // 首帧设置running
        } else {
            if (child != null && child.isRunning()) {
                template_runChild(child);
            }
        }
    }

    @Override
    protected void onChildRunning(Task<E> child) {

    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        if (child.isCancelled()) { // 不响应其它状态，但还是需要响应取消...
            setCancelled();
        }
    }

}