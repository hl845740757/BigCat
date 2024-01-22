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

package cn.wjybxx.bigcat.reload;

import cn.wjybxx.base.Preconditions;
import cn.wjybxx.concurrent.DefaultThreadFactory;
import org.apache.commons.io.FileUtils;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.Set;
import java.util.StringJoiner;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2023/5/22
 */
public class FileReloadTest {

    private static final FilePathPool POOL = FilePathPool.newPool();
    private static final FilePath<List<String>> HelloWorld = POOL.newPath("temp-HelloWorld.txt");
    private static final FilePath<String> HelloWorldCache = POOL.newVirtualPath("HelloWorldCache.txt");

    private static final String resDir = new File(System.getProperty("user.dir")).getParent() + "/testres/";
    private static final File file = new File(resDir + "/" + HelloWorld.getPath());

    private static TestFileDataMgr fileDataMgr;
    private static FileReloadMgr fileReloadMgr;

    /** cacheBuilder访问到的引用 */
    private static List<String> cacheVisitList;
    /** 监听器访问到的引用 */
    private static String listenerVisitString;

    @BeforeAll
    static void beforeAll() throws IOException {
        ThreadPoolExecutor executor = new ThreadPoolExecutor(2, 2,
                5, TimeUnit.SECONDS,
                new ArrayBlockingQueue<>(64),
                new DefaultThreadFactory("common", true));
        executor.allowCoreThreadTimeOut(true);

        fileDataMgr = new TestFileDataMgr();
        fileReloadMgr = new FileReloadMgr(resDir, fileDataMgr, executor);

        generateFileContent(file);
        fileReloadMgr.registerReaders(Set.of(new HelloWorldReader(), new HelloWorldCacheBuilder()));
        fileReloadMgr.registerListener(Set.of(HelloWorldCache), new CacheReloadListener());
    }

    private static void generateFileContent(File file) throws IOException {
        if (!file.exists()) {
            Preconditions.checkState(file.createNewFile(), "createNewFile");
        }
        StringBuilder builder = new StringBuilder(120);
        for (int i = 0; i < 3; i++) {
            if (i != 0) {
                builder.append("\n");
            }
            builder.append(RandomStringUtils.random(16, true, true));
        }
        FileUtils.writeStringToFile(file, builder.toString(), StandardCharsets.UTF_8);
    }

    @Test
    void test() throws IOException {
        fileReloadMgr.loadAll(1000, 5000);
        assertFileContent(file);

        generateFileContent(file);
        fileReloadMgr.reloadAll(1000, 5000);
        assertFileContent(file);

        Assertions.assertSame(cacheVisitList, fileDataMgr.helloWorldList, "cacheBuilder not executed");
        Assertions.assertSame(listenerVisitString, fileDataMgr.helloWorldCacheString, "listener not executed");
    }

    private static void assertFileContent(File file) throws IOException {
        List<String> lines = FileUtils.readLines(file, StandardCharsets.UTF_8);
        String cacheString = join(lines);
        Assertions.assertEquals(lines, fileDataMgr.helloWorldList);
        Assertions.assertEquals(cacheString, fileDataMgr.helloWorldCacheString);
    }

    private static String join(List<String> stringList) {
        StringJoiner joiner = new StringJoiner(",");
        stringList.forEach(joiner::add);
        return joiner.toString();
    }

    private static class TestFileDataMgr implements FileDataMgr {

        private FileDataProvider provider;
        private List<String> helloWorldList;
        private String helloWorldCacheString;

        @Override
        public FileDataMgr newInstance() {
            return new TestFileDataMgr();
        }

        @Override
        public void assignFrom(FileDataProvider provider) {
            this.provider = provider;
            helloWorldList = provider.get(HelloWorld);
            helloWorldCacheString = provider.get(HelloWorldCache);
        }
    }

    private static class HelloWorldReader implements FileReader<List<String>> {

        @Nonnull
        @Override
        public FilePath<List<String>> filePath() {
            return HelloWorld;
        }

        @Override
        public List<String> read(File file, FileDataProvider provider) throws Exception {
            return FileUtils.readLines(file, StandardCharsets.UTF_8);
        }

    }

    private static class HelloWorldCacheBuilder implements FileReader<String> {

        @Nonnull
        @Override
        public FilePath<String> filePath() {
            return HelloWorldCache;
        }

        @Nonnull
        @Override
        public Set<FilePath<?>> dependents() {
            return Set.of(HelloWorld);
        }

        @Override
        public String read(File file, FileDataProvider provider) throws Exception {
            cacheVisitList = provider.get(HelloWorld);
            return join(cacheVisitList);
        }
    }

    private static class CacheReloadListener implements FileReloadListener {

        @Override
        public void afterReload(FileDataMgr fileDataMgr, Set<FilePath<?>> changedFilePathSet) throws Exception {
            listenerVisitString = ((TestFileDataMgr) fileDataMgr).helloWorldCacheString;
        }
    }

}