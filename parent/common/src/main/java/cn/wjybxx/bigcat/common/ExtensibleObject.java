/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common;

import javax.annotation.Nonnull;
import java.util.Map;

/**
 * 可扩展的对象。
 * 游戏内的主要组件尽量都实现该接口，比如：场景和场景内的对象，管理器，world。
 * 主要用于处理热更问题
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface ExtensibleObject {

    /**
     * 获取对象的黑板（用于扩展属性）
     * 注意：必须是对象的一个属性字段。
     * <p>
     * Q: 为什么修改方法名？
     * A: 这个黑板只是为了热更新使用的，直接命名为黑板会占用正常业务逻辑需要的命名。
     */
    @Nonnull
    Map<String, Object> getExtBlackboard();

    /**
     * 执行一个命令（用于扩展方法）。
     * 注意：不要在抽象类统一实现，必须每个具体类自己实现！
     *
     * @param cmd    命令（要做什么）
     * @param params 命令参数（依赖于具体的命令）
     * @return 执行结果
     */
    Object execute(@Nonnull String cmd, Object params);

}