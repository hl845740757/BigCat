/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.bigcat.fx;


import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

import javax.annotation.Nonnull;
import java.util.HashMap;
import java.util.Map;

/**
 * 如果一个rpc服务实现了该接口，将自动导出这两个扩展方法
 * 实现类
 *
 * @author wjybxx
 * date 2023/4/1
 */
@SuppressWarnings("unused")
public interface ExtensibleService {

    /**
     * 获取对象的扩展黑板（用于临时存储属性）
     * <p>
     * 1.必须是对象的一个属性字段；
     * 2.该方法不导出，仅用于提醒留一个备用黑板；
     */
    @Nonnull
    Map<String, Object> getExtBlackboard();

    /** 黑盒Rpc方法 */
    @RpcMethod(methodId = 9999)
    ExecuteResult execute(ExecuteRequest request);

    @DsonSerializable
    class ExecuteRequest {

        private String cmd;
        private Map<String, Object> params = new HashMap<>();

        public String getCmd() {
            return cmd;
        }

        public ExecuteRequest setCmd(String cmd) {
            this.cmd = cmd;
            return this;
        }

        public Map<String, Object> getParams() {
            return params;
        }

        public ExecuteRequest setParams(Map<String, Object> params) {
            this.params = params;
            return this;
        }
    }

    @DsonSerializable
    class ExecuteResult {

        private int code;
        private Object result;

        public ExecuteResult() {
        }

        public ExecuteResult(int code, Object result) {
            this.code = code;
            this.result = result;
        }

        public int getCode() {
            return code;
        }

        public ExecuteResult setCode(int code) {
            this.code = code;
            return this;
        }

        public Object getResult() {
            return result;
        }

        public ExecuteResult setResult(Object result) {
            this.result = result;
            return this;
        }
    }
}