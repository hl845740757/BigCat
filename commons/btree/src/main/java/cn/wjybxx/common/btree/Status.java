/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package cn.wjybxx.common.btree;

/**
 * 行为树的Task的状态
 * 1. 我们不再使用枚举值，使其能表达失败的原因；我们不封装为Result对象，以减少开销。
 * 2. 0~10为保留状态码，用户扩展时从 11 开始。
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public final class Status {

    /** 初始状态 */
    public static final int NEW = 0;
    /** 执行中 */
    public static final int RUNNING = 1;
    /** 执行成功 -- 最小的完成状态 */
    public static final int SUCCESS = 2;

    /** 被取消 -- 需要放在所有失败码的前面，用户可以可以通过比较大小判断；向上传播时要小心 */
    public static final int CANCELLED = 3;
    /** 默认失败码 -- 是最小的失败码 */
    public static final int ERROR = 4;
    /** 前置条件检查失败 -- 未运行的情况下直接失败；注意！该错误码不能向父节点传播 */
    public static final int GUARD_FAILED = 5;
    /** 没有子节点 */
    public static final int CHILDLESS = 6;
    /** 子节点不足 */
    public static final int INSUFFICIENT_CHILD = 7;
    /** 执行超时 */
    public static final int TIMEOUT = 8;

    /** 这是Task类能捕获的最大前一个状态的值，超过该值时将被修正该值 */
    public static final int MAX_PREV_STATUS = 63;

    //
    public static boolean isRunning(int status) {
        return status == Status.RUNNING;
    }

    public static boolean isCompleted(int status) {
        return status >= Status.SUCCESS;
    }

    public static boolean isSucceeded(int status) {
        return status == Status.SUCCESS;
    }

    public static boolean isCancelled(int status) {
        return status == Status.CANCELLED;
    }

    public static boolean isFailed(int status) {
        return status > Status.CANCELLED;
    }

    public static boolean isFailedOrCancelled(int status) {
        return status >= Status.CANCELLED;
    }

    //

    /** 将给定状态码归一化，所有的失败码将被转为{@link #ERROR} */
    public static int normalize(int status) {
        if (status < 0) return 0;
        //noinspection ManualMinMaxCalculation
        if (status > ERROR) return ERROR;
        return status;
    }

    /** 如果给定状态是失败码，则返回参数，否则返回默认失败码 */
    public static int ToFailure(int status) {
        //noinspection ManualMinMaxCalculation
        return status < ERROR ? ERROR : status;
    }

}