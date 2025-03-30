/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.gameplay;

import cn.wjybxx.base.fx.IComponent;
import cn.wjybxx.bigcat.eventbus.EventHandler;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2025/3/17
 */
public interface IScriptBehavior extends IComponent, EventHandler<Object> {

    /** 是否处于激活状态 -- 对于数据组件来说，仅仅是个标识 */
    boolean isEnable();

    /** 设置为激活状态 */
    void setEnable(boolean enable);

    /**
     * 脚本被激活的时候调用
     *
     * @param first 是否是首次激活
     */
    void onEnable(boolean first);

    /**
     * 脚本被禁用的时候调用
     */
    void onDisable();

    /**
     * 脚本首次update前调用。
     * 在这里才可以解决与其它组件的依赖问题。
     */
    void start();

    /**
     * 脚本帧循环逻辑
     */
    void update();

    /**
     * 在所有组件Update之后调用
     */
    void lateUpdate();

    /**
     * 脚本在被销毁前调用。
     * 应当在这里释放引用的其它资源
     */
    void stop();

    /** 收到外部事件时调用 */
    @Override
    void onEvent(@Nonnull Object event);
}