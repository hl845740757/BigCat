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

package cn.wjybxx.common;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nullable;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class CloseableUtils {

    private static final Logger logger = LoggerFactory.getLogger(CloseableUtils.class);

    private CloseableUtils() {
    }

    /**
     * 安静地关闭一个资源
     */
    public static void closeQuietly(@Nullable AutoCloseable resource) {
        if (null != resource) {
            try {
                resource.close();
            } catch (Throwable ignore) {

            }
        }
    }

    /**
     * 安全的关闭一个资源 - 出现任何异常仅仅只记录一个日志
     */
    public static void closeSafely(@Nullable AutoCloseable resource) {
        if (null != resource) {
            try {
                resource.close();
            } catch (Throwable t) {
                logger.warn("close caught exception", t);
            }
        }
    }

}