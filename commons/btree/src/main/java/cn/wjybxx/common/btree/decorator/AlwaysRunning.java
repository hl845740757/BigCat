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

    /** 记录子节点上次的重入id，这样不论enter和execute是否分开执行都不影响 */
    private transient int childPrevReentryId;

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (child == null) {
            childPrevReentryId = 0;
        } else {
            childPrevReentryId = child.getReentryId();
        }
    }

    @Override
    protected void execute() {
        if (child != null) {
            final boolean started = child.isExited(childPrevReentryId);
            if (started && child.isCompleted()) {  // 勿轻易调整
                setRunning();
                return;
            }
            int reentryId = getReentryId();
            template_runChild(child);
            if (isExited(reentryId)) { // 被取消
                return;
            }
        }
        setRunning();
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