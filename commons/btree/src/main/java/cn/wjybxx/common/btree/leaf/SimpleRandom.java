package cn.wjybxx.common.btree.leaf;

import cn.wjybxx.common.btree.LeafTask;
import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;
import java.util.Random;
import java.util.random.RandomGenerator;

/**
 * 简单随机任务
 * 在正式的项目中，Random应当从实体上获取。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class SimpleRandom<E> extends LeafTask<E> {

    /** 允许指定自己的random */
    public static RandomGenerator random = new Random();

    private float p;

    public SimpleRandom() {

    }

    public SimpleRandom(float p) {
        this.p = p;
    }

    @Override
    protected void execute() {
        if (random.nextDouble() <= p) {
            setSuccess();
        } else {
            setFailed(Status.ERROR);
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {

    }

    public float getP() {
        return p;
    }

    public void setP(float p) {
        this.p = p;
    }
}