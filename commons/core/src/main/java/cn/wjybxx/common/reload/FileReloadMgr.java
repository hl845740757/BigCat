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

package cn.wjybxx.common.reload;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.Constant;
import cn.wjybxx.common.Preconditions;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.ex.BadImplementationException;
import cn.wjybxx.common.time.StepWatch;
import com.google.common.graph.GraphBuilder;
import com.google.common.graph.MutableGraph;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.io.File;
import java.util.*;
import java.util.concurrent.Executor;
import java.util.concurrent.ThreadPoolExecutor.CallerRunsPolicy;

/**
 * @author wjybxx
 * date - 2023/5/21
 */
@SuppressWarnings("UnstableApiUsage")
public class FileReloadMgr {

    private static final Logger logger = LoggerFactory.getLogger(FileReloadMgr.class);

    private final String projectResDir;
    private final FileDataMgr fileDataMgr;
    private final Executor executor;

    private final Map<FilePath<?>, FileMetadata<?>> metadataMap = new IdentityHashMap<>(200);
    private final Set<FileDataLinker> linkerSet = CollectionUtils.newIdentityHashSet(20);
    private final Set<FileDataValidator> validatorSet = CollectionUtils.newIdentityHashSet(20);

    private final FileDataContainer fileDataContainer = new FileDataContainer();
    private final Map<FilePath<?>, ListenerWrapper> listenerWrapperMap = new IdentityHashMap<>(50);

    /**
     * @param projectResDir 项目的资源文件路径
     * @param fileDataMgr   用于最后存储数据的DataMgr
     * @param executor      用于并发读取文件时的Executor
     *                      如果是共享线程池，建议线程池上不要有太多的阻塞认为
     *                      如果是独享线程池，建议拒绝策略为{@link CallerRunsPolicy}
     */
    public FileReloadMgr(String projectResDir, FileDataMgr fileDataMgr, Executor executor) {
        this.projectResDir = Objects.requireNonNull(projectResDir, "projectResDir");
        this.executor = Objects.requireNonNull(executor, "executor");
        this.fileDataMgr = Objects.requireNonNull(fileDataMgr, "fileDataMgr");

        File file = new File(projectResDir);
        if (!file.isDirectory()) {
            throw new IllegalArgumentException("the resource directory does not exist, absPath: " + file.getAbsolutePath());
        }
    }

    public FileDataProvider getDataProvider() {
        return fileDataContainer;
    }

    public Set<FilePath<?>> getPathSet() {
        return Collections.unmodifiableSet(metadataMap.keySet());
    }

    //
    public void registerReaders(Collection<? extends FileReader<?>> readers) {
        for (FileReader<?> reader : readers) {
            final FilePath<?> filePath = reader.filePath();
            if (metadataMap.containsKey(filePath)) {
                final String msg = String.format("filePath has more than one associated reader, filePath: %s, reader: %s",
                        filePath, reader.getClass().getName());
                throw new IllegalArgumentException(msg);
            }
            final File file = checkedFileOfPath(projectResDir, filePath);
            metadataMap.put(filePath, new FileMetadata<>(reader, file));
        }
    }

    public void unregisterReader(Collection<? extends FileReader<?>> readers) {
        for (FileReader<?> reader : readers) {
            final FilePath<?> filePath = reader.filePath();
            final FileMetadata<?> fileMetadata = metadataMap.get(filePath);
            if (fileMetadata == null) {
                continue;
            }
            if (fileMetadata.reader != reader) {
                final String msg = String.format("reader mismatch, fileName: %s, reader: %s", filePath, reader.getClass().getName());
                throw new IllegalArgumentException(msg);
            }
            metadataMap.remove(filePath);
        }
    }

    public void registerLinkers(Collection<? extends FileDataLinker> linkers) {
        Preconditions.checkNullElements(linkers);
        linkerSet.addAll(linkers);
    }

    public void unregisterLinkers(Collection<? extends FileDataLinker> linkers) {
        linkerSet.removeAll(linkers);
    }

    public void registerValidators(Collection<? extends FileDataValidator> validators) {
        Preconditions.checkNullElements(validators);
        validatorSet.addAll(validators);
    }

    public void unregisterValidators(Collection<? extends FileDataValidator> validators) {
        validatorSet.removeAll(validators);
    }

    /** 清除文件的状态信息，使得reload时加入变化集合 */
    public void clearFileStat(FilePath<?> filePath) {
        FileMetadata<?> fileMetadata = metadataMap.get(filePath);
        if (fileMetadata != null) {
            fileMetadata.fileStat = null;
        }
    }

    /** 重新读取文件的状态信息，使得reload时不加入变化集合 */
    public void loadFileStat(FilePath<?> filePath) {
        if (filePath.isVirtual()) {
            return;
        }
        final FileMetadata<?> fileMetadata = getMetadata(filePath);
        try {
            fileMetadata.fileStat = ReloadUtils.statOfFile(fileMetadata.file);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }

    Executor getExecutor() {
        return executor;
    }

    FileStat getFileStat(FilePath<?> filePath) {
        return getMetadata(filePath).fileStat;
    }

    @SuppressWarnings("unchecked")
    <T> FileMetadata<T> getMetadata(FilePath<T> filePath) {
        FileMetadata<T> fileMetadata = (FileMetadata<T>) metadataMap.get(filePath);
        if (null == fileMetadata) {
            throw new IllegalArgumentException("unknown filePath: " + filePath);
        }
        return fileMetadata;
    }

    private static File checkedFileOfPath(String projectResDir, FilePath<?> filePath) {
        final String relativePath = filePath.getPath();
        final File file = new File(projectResDir + File.separator + relativePath);
        if (!filePath.isVirtual() && !file.exists()) {
            throw new IllegalArgumentException("file not found: " + relativePath);
        }
        return file;
    }

    private void ensureFileReaderExist(@Nonnull Collection<FilePath<?>> filePathSet) {
        for (FilePath<?> filePath : filePathSet) {
            Objects.requireNonNull(filePath, "filePath");
            final FileMetadata<?> fileMetadata = metadataMap.get(filePath);
            if (null == fileMetadata) {
                throw new IllegalArgumentException("unknown filePath: " + filePath);
            }
        }
    }
    //

    // region 读表

    /**
     * loadAll方法不通知监听器
     *
     * @param timeoutFindChangedFiles 统计文件信息的超时时间(毫秒)
     * @param timeoutReadFiles        读取文件的超时时间(毫秒)
     */
    public void loadAll(long timeoutFindChangedFiles, long timeoutReadFiles) {
        logger.info("loadAll started");
        final StepWatch stepWatch = StepWatch.createStarted("FileReloadMgr:loadAll");
        try {
            reloadImpl(metadataMap.keySet(), true,
                    timeoutFindChangedFiles, timeoutReadFiles, false,
                    stepWatch);

            logger.info("loadAll completed, fileCount {}, stepInfo {}", metadataMap.size(), stepWatch);
        } catch (Exception e) {
            logger.info("loadAll failure, fileCount {}, stepInfo {}", metadataMap.size(), stepWatch);
            throw ReloadException.wrap(FutureUtils.unwrapCompletionException(e));
        }
    }

    /**
     * 热更新所有变化的文件
     *
     * @param timeoutFindChangedFiles 统计文件变化的超时时间(毫秒)
     * @param timeoutReadFiles        读取文件内容的超时时间(毫秒)，设定超时时间有助于提前发现问题
     */
    public void reloadAll(long timeoutFindChangedFiles, long timeoutReadFiles) {
        reloadScopeImpl(metadataMap.keySet(), timeoutFindChangedFiles, timeoutReadFiles);
    }

    /**
     * 从指定范围检测变化的文件，变化的文件将被重新加载
     * 1.变化文件的下游文件也将热更，eg：B依赖A文件，当A文件变化时，A、B都会被更新
     * 2.reload方法将通知监听器
     *
     * @param scope 热加载范围
     */
    public void reloadScope(Set<FilePath<?>> scope, long timeoutFindChangedFiles, long timeoutReadFiles) {
        Objects.requireNonNull(scope, "scope");
        ensureFileReaderExist(scope);
        reloadScopeImpl(scope, timeoutFindChangedFiles, timeoutReadFiles);
    }

    private void reloadScopeImpl(Set<FilePath<?>> scope, long timeoutFindChangedFiles, long timeoutReadFiles) {
        logger.info("reloadScope started");
        final StepWatch stepWatch = StepWatch.createStarted("FileReloadMgr:reloadScope");
        try {
            reloadImpl(scope, false,
                    timeoutFindChangedFiles, timeoutReadFiles, true,
                    stepWatch);

            logger.info("reloadScope completed, fileNum {}, stepInfo {}", metadataMap.size(), stepWatch);
        } catch (Exception e) {
            logger.info("reloadScope failure, fileNum {}, stepInfo {}", metadataMap.size(), stepWatch);
            throw new ReloadException(FutureUtils.unwrapCompletionException(e));
        }
    }

    private void reloadImpl(Set<FilePath<?>> scope, boolean reStatistics,
                            long timeoutFindChangedFiles, long timeoutReadFiles,
                            boolean notifyListener, StepWatch stepWatch) throws Exception {
        // 读取文件的频率并不高，因此每次读取时更新依赖
        rebuildReaderDependents();
        stepWatch.logStep("rebuildReaderDependents");

        FileReloadHelper helper = new FileReloadHelper(this, scope, reStatistics,
                timeoutFindChangedFiles, timeoutReadFiles);
        // 统计文件状态
        helper.statisticFileStats();
        stepWatch.logStep("statisticFileStats");

        // 读取所有受影响的文件
        Set<FilePath<?>> affectedFiles = helper.readAffectedFiles();
        stepWatch.logStep("readAffectedFiles");

        // 初始化沙箱环境的FileDataMgr
        FileDataContainer sandBoxDataContainer = helper.getSandboxDataContainer();
        FileDataMgr sandboxDateMgr = createSandboxDateMgr();
        sandboxDateMgr.assignFrom(sandBoxDataContainer);
        try {
            link(sandboxDateMgr);
            // 验证数据合法性
            validate(sandboxDateMgr);
            stepWatch.logStep("validate");
        } catch (Throwable e) {
            // 错误恢复，由于linker会导致外部data链接到沙箱数据，我们通过再次执行link来纠正错误
            // 构建完全的沙盒开销极大，因此我们选择纠正错误的方式实现原子性
            if (!fileDataContainer.isEmpty()) {
                link(fileDataMgr);
            }
            ExceptionUtils.rethrow(e);
        }

        // 赋值到真实环境
        fileDataContainer.clear();
        fileDataContainer.putAll(sandBoxDataContainer);
        fileDataMgr.assignFrom(fileDataContainer);
        link(fileDataMgr);

        // 执行回调逻辑（这里可能产生异常）
        if (notifyListener) {
            notifyListeners(Collections.unmodifiableSet(affectedFiles));
            stepWatch.logStep("notifyListeners");
        }

        // 最后更新文件状态(这可以使得在前面发生异常后可以重试)
        for (FilePath<?> filePath : affectedFiles) {
            FileStat fileStat = helper.getFileStat(filePath);
            if (fileStat == null) { // 可能只是依赖的文件产生了变化
                continue;
            }
            getMetadata(filePath).fileStat = fileStat;
        }
    }

    /**
     * 重新构建Reader的依赖关系
     * 由于读表并不常发生，因此每次读表时重新构建即可
     */
    private void rebuildReaderDependents() {
        MutableGraph<FilePath<?>> graph = GraphBuilder.directed()
                .allowsSelfLoops(false)
                .expectedNodeCount(metadataMap.size())
                .build();
        for (FileMetadata<?> fileMetadata : metadataMap.values()) {
            FileReader<?> reader = fileMetadata.reader;
            graph.addNode(reader.filePath());
        }
        for (FileMetadata<?> fileMetadata : metadataMap.values()) {
            FileReader<?> reader = fileMetadata.reader;
            ensureFileReaderExist(reader.dependents()); // 确保依赖的文件存在
            for (FilePath<?> dependent : reader.dependents()) {
                graph.putEdge(dependent, reader.filePath());
            }
        }
        // 确定FileReader的执行优先级
        topSortReaders(graph);
    }

    private void topSortReaders(MutableGraph<FilePath<?>> graph) {
        // 重置数据
        for (FileMetadata<?> fileMetadata : metadataMap.values()) {
            fileMetadata.priority = -1;
            int dependentCount = fileMetadata.reader.dependents().size();
            if (dependentCount > 0) {
                fileMetadata.allDependents = CollectionUtils.newIdentityHashSet(dependentCount);
            }
        }

        // 避免迭代时删除问题，为保证结果一致，我们先按照FilePath定义顺序排序 - Graph本身可指定Node顺序的，但我认为这不是个好依赖
        List<FilePath<?>> nodeList = new ArrayList<>(graph.nodes());
        nodeList.sort(Constant::compareTo);

        int priority = 0;
        while (nodeList.size() > 0) {
            for (int idx = 0; idx < nodeList.size(); idx++) {
                FilePath<?> filePath = nodeList.get(idx);
                if (graph.inDegree(filePath) == 0) {
                    // 将自己的所有依赖和自身添加到后继节点中
                    FileMetadata<?> fileMetadata = metadataMap.get(filePath);
                    for (FilePath<?> successor : graph.successors(filePath)) {
                        FileMetadata<?> successorMetadata = metadataMap.get(successor);
                        successorMetadata.allDependents.addAll(fileMetadata.allDependents);
                        successorMetadata.allDependents.add(filePath);
                    }
                    fileMetadata.priority = priority++;
                    graph.removeNode(filePath);
                    nodeList.set(idx, null);
                }
            }
            if (!nodeList.removeIf(Objects::isNull)) {
                throw new IllegalStateException("Graph has a cycle, remain NodeList: " + nodeList);
            }
        }
    }

    private FileDataMgr createSandboxDateMgr() {
        final FileDataMgr sandboxFileDataMgr = fileDataMgr.newInstance();
        if (sandboxFileDataMgr == null) {
            throw new BadImplementationException("fileDataMgr.newInstance return null, class: " + fileDataMgr.getClass().getName());
        }
        if (sandboxFileDataMgr == fileDataMgr) {
            throw new BadImplementationException("fileDataMgr.newInstance return self, class: " + fileDataMgr.getClass().getName());
        }
        if (sandboxFileDataMgr.getClass() != fileDataMgr.getClass()) {
            final String msg = String.format("fileDataMgr.newInstance return bad type, protoType: %s, cloned: %s", fileDataMgr.getClass().getName(),
                    sandboxFileDataMgr.getClass().getName());
            throw new BadImplementationException(msg);
        }
        return sandboxFileDataMgr;
    }

    private void link(FileDataMgr fileDataMgr) {
        for (FileDataLinker linker : linkerSet) {
            linker.link(fileDataMgr);
        }
    }

    private void validate(FileDataMgr fileDataMgr) throws Exception {
        // validate通常不会有太大开销，因此总是全部执行
        for (FileDataValidator validator : validatorSet) {
            validator.validate(fileDataMgr);
        }
    }

    // endregion

    // region 监听

    /**
     * 注册文件热更新回调
     *
     * @param filePathSet 关注的文件
     * @param listener    监听器
     */
    public void registerListener(@Nonnull Set<FilePath<?>> filePathSet, @Nonnull FileReloadListener listener) {
        Preconditions.checkNotEmpty(filePathSet, "filePathSet is empty");
        Objects.requireNonNull(listener, "listener");

        // 拷贝以避免外部后续修改
        filePathSet = Set.copyOf(filePathSet);
        final DefaultListenerWrapper listenerWrapper = new DefaultListenerWrapper(filePathSet, listener);
        for (FilePath<?> filePath : filePathSet) {
            final ListenerWrapper existListenerWrapper = listenerWrapperMap.get(filePath);
            if (null == existListenerWrapper) {
                listenerWrapperMap.put(filePath, listenerWrapper);
                continue;
            }
            if (existListenerWrapper instanceof CompositeListenerWrapper compositeWrapper) {
                compositeWrapper.addChild(listenerWrapper);
            } else {
                listenerWrapperMap.put(filePath, new CompositeListenerWrapper(existListenerWrapper, listenerWrapper));
            }
        }
    }

    public void unregisterListener(@Nonnull Set<FilePath<?>> filePathSet, FileReloadListener listener) {
        Preconditions.checkNotEmpty(filePathSet, "filePathSet is empty");
        Objects.requireNonNull(listener, "listener");

        for (FilePath<?> filePath : filePathSet) {
            final ListenerWrapper existListenerWrapper = listenerWrapperMap.get(filePath);
            if (existListenerWrapper == null) {
                continue;
            }
            if (existListenerWrapper instanceof DefaultListenerWrapper wrapper) {
                if (wrapper.reloadListener == listener) {
                    listenerWrapperMap.remove(filePath);
                }
            } else {
                final CompositeListenerWrapper wrapper = (CompositeListenerWrapper) existListenerWrapper;
                CollectionUtils.removeRef(wrapper.children, listener);
                if (wrapper.children.isEmpty()) {
                    listenerWrapperMap.remove(filePath);
                }
            }
        }
    }

    private void notifyListeners(Set<FilePath<?>> allChangedFilePathSet) throws Exception {
        final Set<ListenerWrapper> notifiedListeners = CollectionUtils.newIdentityHashSet(allChangedFilePathSet.size() / 3);
        for (FilePath<?> filePath : allChangedFilePathSet) {
            final ListenerWrapper listenerWrapper = listenerWrapperMap.get(filePath);
            if (null == listenerWrapper || !notifiedListeners.add(listenerWrapper)) {
                continue;
            }
            listenerWrapper.afterReload(fileDataMgr, allChangedFilePathSet);
        }
    }

    private interface ListenerWrapper {

        void afterReload(FileDataMgr fileDataMgr, Set<FilePath<?>> allChangedFilePathSet) throws Exception;

    }

    private static class DefaultListenerWrapper implements ListenerWrapper {

        final Set<FilePath<?>> filePathSet;
        final FileReloadListener reloadListener;

        DefaultListenerWrapper(Set<FilePath<?>> filePathSet, FileReloadListener reloadListener) {
            this.filePathSet = filePathSet;
            this.reloadListener = reloadListener;
        }

        @Override
        public void afterReload(FileDataMgr fileDataMgr, Set<FilePath<?>> allChangedFilePathSet) throws Exception {
            final Set<FilePath<?>> changedFilePathSet = new HashSet<>(filePathSet);
            changedFilePathSet.retainAll(allChangedFilePathSet);
            reloadListener.afterReload(fileDataMgr, changedFilePathSet);
        }
    }

    private static class CompositeListenerWrapper implements ListenerWrapper {

        final List<ListenerWrapper> children;

        CompositeListenerWrapper(ListenerWrapper first, ListenerWrapper second) {
            this.children = new ArrayList<>(4);
            this.children.add(first);
            this.children.add(second);
        }

        @Override
        public void afterReload(FileDataMgr fileDataMgr, Set<FilePath<?>> allChangedFilePathSet) throws Exception {
            for (ListenerWrapper child : children) {
                child.afterReload(fileDataMgr, allChangedFilePathSet);
            }
        }

        public void addChild(ListenerWrapper child) {
            this.children.add(child);
        }
    }
    // endregion

    //

}