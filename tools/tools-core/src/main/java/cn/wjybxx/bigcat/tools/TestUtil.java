package cn.wjybxx.bigcat.tools;

/**
 * @author wjybxx
 * date - 2023/12/24
 */
public class TestUtil {

    /** bigcat总文件目录 */
    public static final String bigcatPath = Utils.findProjectDir("BigCat").getPath();
    /** 总仓库的文档目录地址 -- 部分示例表格 */
    public static final String docPath = bigcatPath + "/doc";

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