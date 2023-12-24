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

package cn.wjybxx.bigcat.reload;

import cn.wjybxx.base.CollectionUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.io.File;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import java.util.stream.Collectors;

/**
 * 方法对象，以避免大量传参和减少{@link FileReloadMgr}行数
 *
 * @author wjybxx
 * date - 2023/5/21
 */
class FileReloadHelper {

    private final FileReloadMgr reloadMgr;
    private final FileDataContainer sandboxDataContainer; // 沙盒
    private final Map<FilePath<?>, FileStat> fileStatMap;

    private final Set<FilePath<?>> scope;
    private final boolean reStatistics;
    private final long timeoutFindChangedFiles;
    private final long timeoutReadFiles;

    FileReloadHelper(FileReloadMgr reloadMgr,
                     Set<FilePath<?>> scope, boolean reStatistics,
                     long timeoutFindChangedFiles, long timeoutReadFiles) {
        this.reloadMgr = reloadMgr;
        this.sandboxDataContainer = new FileDataContainer(reloadMgr.getPathSet().size());
        this.fileStatMap = CollectionUtils.newIdentityHashMap(scope.size());

        this.scope = Set.copyOf(scope);
        this.reStatistics = reStatistics;
        this.timeoutFindChangedFiles = timeoutFindChangedFiles;
        this.timeoutReadFiles = timeoutReadFiles;
    }

    FileDataContainer getSandboxDataContainer() {
        return sandboxDataContainer;
    }

    FileStat getFileStat(FilePath<?> filePath) {
        return fileStatMap.get(filePath);
    }

    /** 统计文件的状态信息 */
    void statisticFileStats() {
        // 如果文件不是很多，单线程就可以
        List<CompletableFuture<?>> futureList = new ArrayList<>(scope.size());
        List<StatisticFileStatTask> taskList = new ArrayList<>(scope.size());
        for (FilePath<?> filePath : scope) {
            if (filePath.isVirtual()) {
                continue;
            }
            StatisticFileStatTask statisticTask = new StatisticFileStatTask(filePath, reloadMgr.getMetadata(filePath).file);
            CompletableFuture<?> future = CompletableFuture.runAsync(statisticTask, reloadMgr.getExecutor());

            taskList.add(statisticTask);
            futureList.add(future);
        }
        CompletableFuture.allOf(futureList.toArray(CompletableFuture[]::new))
                .orTimeout(timeoutFindChangedFiles, TimeUnit.MILLISECONDS)
                .join();
        // 存储在当前对象上
        taskList.forEach(task -> fileStatMap.put(task.filePath, task.fileStat));
    }

    /**
     * 读取受影响的文件
     * 1.并行读取无依赖的文件
     * 2.串行读取有依赖的文件
     *
     * @return 所有变更的文件
     */
    Set<FilePath<?>> readAffectedFiles() {
        List<FileMetadata<?>> affectedFiles = calAffectedFiles();
        initSandbox(affectedFiles);

        final List<CompletableFuture<?>> allFutureList = new ArrayList<>(affectedFiles.size());
        CompletableFuture<?> tail = parallelReadIndependents(affectedFiles, allFutureList);
        tail = sequentialReadDependents(affectedFiles, allFutureList, tail);

        try {
            tail.get(timeoutReadFiles, TimeUnit.MILLISECONDS);
            return affectedFiles.stream()
                    .map(e -> e.reader.filePath())
                    .collect(Collectors.toSet());
        } catch (Exception e) {
            // 取消可能未执行的任务
            cancelAllFuture(allFutureList);
            return ExceptionUtils.rethrow(e);
        }
    }

    private void cancelAllFuture(List<CompletableFuture<?>> allFutureList) {
        for (CompletableFuture<?> future : allFutureList) {
            future.cancel(false);
        }
    }

    private List<FileMetadata<?>> calAffectedFiles() {
        // 先计算真实改变的文件
        Set<FilePath<?>> changedFiles = CollectionUtils.newIdentityHashSet(fileStatMap.size());
        if (reStatistics) {
            changedFiles.addAll(scope);
        } else {
            fileStatMap.forEach((filePath, fileStat) -> {
                if (!Objects.equals(fileStat, reloadMgr.getFileStat(filePath))) {
                    changedFiles.add(filePath);
                }
            });
        }

        // 根据真实改变的文件计算受影响的文件
        List<FileMetadata<?>> metadataList = new ArrayList<>(changedFiles.size());
        for (FilePath<?> filePath : reloadMgr.getPathSet()) {
            FileMetadata<?> metadata = reloadMgr.getMetadata(filePath);
            if (changedFiles.contains(filePath) || CollectionUtils.joint(metadata.allDependents, changedFiles)) {
                metadataList.add(metadata);
            }
        }
        // 需要按照顺序读取
        metadataList.sort(Comparator.comparingInt(e -> e.priority));
        return metadataList;
    }

    private void initSandbox(List<FileMetadata<?>> affectedFiles) {
        sandboxDataContainer.clear();
        sandboxDataContainer.putAll(reloadMgr.getDataProvider());
        // 移除需要重新读取的文件
        for (FileMetadata<?> fileMetadata : affectedFiles) {
            sandboxDataContainer.remove(fileMetadata.reader.filePath());
        }
    }

    private CompletableFuture<?> parallelReadIndependents(List<FileMetadata<?>> affectedFiles, List<CompletableFuture<?>> allFutureList) {
        List<CompletableFuture<?>> parallelFutrureList = new ArrayList<>(affectedFiles.size());
        List<ReadFileDataTask> taskList = new ArrayList<>(affectedFiles.size());
        for (FileMetadata<?> fileMetadata : affectedFiles) {
            if (fileMetadata.allDependents.size() > 0) {
                break; // 后续都是有依赖的文件
            }
            ReadFileDataTask readTask = new ReadFileDataTask(fileMetadata.reader, fileMetadata.file, FileDataProviders.empty());
            CompletableFuture<Void> future = CompletableFuture.runAsync(readTask, reloadMgr.getExecutor());

            taskList.add(readTask);
            parallelFutrureList.add(future);
            allFutureList.add(future);
        }
        CompletableFuture<Void> tail = CompletableFuture.allOf(parallelFutrureList.toArray(CompletableFuture[]::new))
                .thenRun(() -> {
                    for (ReadFileDataTask task : taskList) {
                        @SuppressWarnings("unchecked") FilePath<Object> filePath = (FilePath<Object>) task.reader.filePath();
                        sandboxDataContainer.put(filePath, task.fileData);
                    }
                });
        allFutureList.add(tail);
        return tail;
    }

    private CompletableFuture<?> sequentialReadDependents(List<FileMetadata<?>> affectedFiles, List<CompletableFuture<?>> allFutureList,
                                                          CompletableFuture<?> tail) {
        for (FileMetadata<?> fileMetadata : affectedFiles) {
            if (fileMetadata.allDependents.size() == 0) {
                continue;
            }
            // 无需runAsync，因为tail就在executor线程中
            tail = tail.thenRun(() -> {
                FileDataProvider limitedProvider = FileDataProviders.limited(sandboxDataContainer, fileMetadata.allDependents);
                ReadFileDataTask readTask = new ReadFileDataTask(fileMetadata.reader, fileMetadata.file, limitedProvider);
                readTask.run();

                @SuppressWarnings("unchecked") FilePath<Object> filePath = (FilePath<Object>) fileMetadata.reader.filePath();
                sandboxDataContainer.put(filePath, readTask.fileData);
            });
            allFutureList.add(tail);
        }
        return tail;
    }

    private static class StatisticFileStatTask implements Runnable {

        final FilePath<?> filePath;
        final File file;

        FileStat fileStat;

        private StatisticFileStatTask(FilePath<?> filePath, File file) {
            this.filePath = filePath;
            this.file = file;
        }

        @Override
        public void run() {
            try {
                fileStat = ReloadUtils.statOfFile(file);
            } catch (Exception | AssertionError e) {
                throw new RuntimeException("filePath: " + filePath, e);
            }
        }

    }

    private static class ReadFileDataTask implements Runnable {

        final FileReader<?> reader;
        final File file;
        final FileDataProvider provider;

        Object fileData;

        public ReadFileDataTask(FileReader<?> reader, File file, FileDataProvider provider) {
            this.reader = reader;
            this.file = file;
            this.provider = provider;
        }

        @Override
        public void run() {
            try {
                fileData = Objects.requireNonNull(reader.read(file, provider));
            } catch (Exception | AssertionError e) {
                throw new RuntimeException("filePath: " + reader.filePath(), e);
            }
        }

    }
}