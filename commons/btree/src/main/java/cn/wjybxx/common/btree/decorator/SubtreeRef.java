package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 子树引用
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class SubtreeRef<E> extends Decorator<E> {

    private String treeName;

    @Override
    protected void enter(int reentryId) {
        if (child == null) {
            Task<E> rootTask = getTaskEntry().getTreeLoader().loadRootTask(treeName);
            addChild(rootTask);
        }
    }

    @Override
    protected void execute() {
        template_runChild(child);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        setCompleted(child.getStatus(), true);
    }

    public String getTreeName() {
        return treeName;
    }

    public void setTreeName(String treeName) {
        this.treeName = treeName;
    }
}