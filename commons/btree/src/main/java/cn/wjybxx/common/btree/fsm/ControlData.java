package cn.wjybxx.common.btree.fsm;

/**
 * State的控制数据
 * 1.不建议复用对象
 * 2.用户慎直接使用
 */
public class ControlData {

    public static final int CMD_NONE = 0;
    public static final int CMD_UNDO = 1;
    public static final int CMD_REDO = 2;

    public final int cmd;
    public final int delayMode;
    public final int frame;

    public ControlData(int cmd, int delayMode) {
        checkCmd(cmd);
        this.delayMode = delayMode;
        this.cmd = cmd;
        this.frame = 0;
    }

    public ControlData(int cmd, int delayMode, int frame) {
        checkCmd(cmd);
        this.delayMode = delayMode;
        this.cmd = cmd;
        this.frame = frame;
    }

    public ControlData withDelayMode(int delayMode) {
        if (delayMode == this.delayMode) {
            return this;
        }
        return new ControlData(cmd, delayMode, frame);
    }

    private static void checkCmd(int cmd) {
        if (cmd < CMD_NONE || cmd > CMD_REDO) {
            throw new IllegalArgumentException("cmd: " + cmd);
        }
    }

    protected static final ControlData NONE = new ControlData(0, 0);


}