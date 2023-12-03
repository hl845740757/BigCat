package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Status;

/**
 * 循环节点抽象
 * 如果{@link #execute()}方法是有循环体的，那么一定要注意：
 * 只有循环的尾部运行child才是安全的，如果在运行child后还读写其它数据，可能导致bug(小心递归)。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class LoopDecorator<E> extends Decorator<E> {

    /** 每帧最大循环次数 - 避免死循环和占用较多CPU；默认1 */
    protected int maxLoopPerFrame = 1;

    public LoopDecorator() {
    }

    public LoopDecorator(int maxLoopPerFrame) {
        this.maxLoopPerFrame = Math.max(0, maxLoopPerFrame);
    }

    @Override
    protected void execute() {
        if (maxLoopPerFrame < 1) {
            setFailed(Status.ERROR);
            return;
        }
        if (maxLoopPerFrame == 1) {
            template_runChild(child);
            return;
        }
        int reentryId = getReentryId();
        for (int _i = maxLoopPerFrame - 1; _i >= 0; _i--) {
            template_runChild(child);
            if (checkCancel(reentryId)) {
                return;
            }
            if (child.isRunning()) { // 子节点未完成
                return;
            }
        }
    }

    public int getMaxLoopPerFrame() {
        return maxLoopPerFrame;
    }

    public void setMaxLoopPerFrame(int maxLoopPerFrame) {
        this.maxLoopPerFrame = maxLoopPerFrame;
    }
}