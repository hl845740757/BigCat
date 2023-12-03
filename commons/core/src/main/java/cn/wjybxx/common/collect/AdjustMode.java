package cn.wjybxx.common.collect;

/**
 * 双端队列在容量变化时的调整方式
 *
 * @author wjybxx
 * date - 2023/12/3
 */
public enum AdjustMode {
    /** 抛出异常 */
    ABORT,
    /** 丢弃头部 */
    DISCARD_HEAD,
    /** 丢弃尾部 */
    DISCARD_TAIL,
}