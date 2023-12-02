package cn.wjybxx.common.btree.leaf;

import cn.wjybxx.common.btree.LeafTask;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;

/**
 * 等待N帧
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class WaitFrame<E> extends LeafTask<E> {

    private int required = 1;

    public WaitFrame() {
    }

    public WaitFrame(int required) {
        this.required = required;
    }

    @Override
    protected void execute() {
        if (getRunFrames() >= required) {
            setSuccess();
        } else {
            setRunning();
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {

    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }
}