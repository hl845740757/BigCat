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

package cn.wjybxx.bigcattools.common;

/**
 * @author wjybxx
 * date - 2023/12/24
 */
public class TestUtil {

    /** bigcat总文件目录 */
    public static final String bigcatPath = Utils.findProjectDir("BigCat").getPath() + "/java";
    /** 总仓库的文档目录地址 -- 部分示例表格 */
    public static final String docPath = bigcatPath + "/docs";

    /** tools项目根目录 */
    public static final String toolsPath = bigcatPath + "/tools";
    /** 测试用资源目录 */
    public static final String testResPath = toolsPath + "/testres";

    /**
     * 当前运行模块的路径
     * 注意：不适用Main函数启动的用例，main函数启动的用例的工作路径为顶层路径，即bigcat目录
     */
    public static final String modulePath = Utils.getUserWorkerDir().getPath();
}