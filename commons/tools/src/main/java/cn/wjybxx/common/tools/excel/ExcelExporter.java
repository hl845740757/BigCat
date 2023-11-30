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

package cn.wjybxx.common.tools.excel;

import cn.wjybxx.common.codec.ConvertOptions;
import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.TypeMeta;
import cn.wjybxx.common.codec.TypeMetaRegistries;
import cn.wjybxx.common.codec.document.DefaultDocumentConverter;
import cn.wjybxx.common.codec.document.DocumentConverter;
import cn.wjybxx.common.config.Sheet;
import cn.wjybxx.common.config.SheetCodec;
import cn.wjybxx.common.tools.util.NotTempFileFilter;
import cn.wjybxx.dson.text.DsonMode;
import cn.wjybxx.dson.text.ObjectStyle;
import org.apache.commons.io.FileUtils;
import org.apache.commons.io.filefilter.HiddenFileFilter;
import org.apache.commons.io.filefilter.SuffixFileFilter;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.Executor;
import java.util.stream.Collectors;

/**
 * 表格导出工具
 *
 * @author wjybxx
 * date - 2023/10/14
 */
public class ExcelExporter {

    private static final String EXCEL_EXTENSION = "xlsx";
    private static final String DSON_EXTENSION = "dson";

    /** 导表选项 */
    private final ExcelExporterOptions options;
    /** 线程池 -- 表格会被并发读取和并发导出 */
    private final Executor executor;
    /** 编解码器 */
    private final DocumentConverter converter;

    private Map<String, Sheet> sheetMap;

    public ExcelExporter(ExcelExporterOptions options, Executor executor) {
        this.options = options;
        this.executor = executor;
        this.converter = DefaultDocumentConverter.newInstance(
                List.of(new SheetCodec()),
                TypeMetaRegistries.fromMetas(TypeMeta.of(Sheet.class, ObjectStyle.INDENT, "Sheet")),
                ConvertOptions.DEFAULT);
    }

    /**
     * 获取所有的表格数据，可用于生成其它数据
     * 在{@link #build()}之后可调用
     */
    public Map<String, Sheet> getSheetMap() {
        if (sheetMap == null) {
            throw new IllegalStateException();
        }
        return sheetMap;
    }

    public void build() throws IOException {
        cleanOutDir();
        Collection<File> excels = FileUtils.listFiles(new File(options.getExcelDir()),
                HiddenFileFilter.VISIBLE
                        .and(new SuffixFileFilter("." + EXCEL_EXTENSION))
                        .and(NotTempFileFilter.INSTANCE),
                HiddenFileFilter.VISIBLE);
        // 并发读取所有文件
        sheetMap = readAllFile(excels);

        // 主线程检查文件（检查主键重复等）
        if (options.getValidator() != null) {
            options.getValidator().validate(sheetMap);
        }

        // 并发导出文件
        exportAllFile(sheetMap);
    }

    private void cleanOutDir() throws IOException {
        if (options.isCleanOutDir()) {
            File outDir = new File(options.getOutDir());
            if (outDir.exists()) {
                FileUtils.cleanDirectory(outDir);
            }
        }
    }

    private Map<String, Sheet> readAllFile(Collection<File> excels) {
        List<CompletableFuture<Collection<Sheet>>> readTasks = new ArrayList<>(excels.size());
        for (File file : excels) {
            readTasks.add(CompletableFuture.supplyAsync(() -> readExcel(file), executor));
        }
        CompletableFuture.allOf(readTasks.toArray(CompletableFuture[]::new))
                .join();
        return readTasks.stream()
                .flatMap(e -> e.resultNow().stream())
                .collect(Collectors.toUnmodifiableMap(Sheet::getSheetName, e -> e));
    }

    private Collection<Sheet> readExcel(File file) {
        return ExcelUtils.readExcel(file, options.getReaderOptions())
                .values();
    }

    private void exportAllFile(Map<String, Sheet> sheetMap) {
        File outDir = new File(options.getOutDir());
        if (!outDir.exists()) {
            outDir.mkdirs();
        }

        List<CompletableFuture<?>> writeTasks = new ArrayList<>(sheetMap.size());
        for (Sheet sheet : sheetMap.values()) {
            writeTasks.add(CompletableFuture.runAsync(() -> writeSheet(sheet), executor));
        }
        CompletableFuture.allOf(writeTasks.toArray(CompletableFuture[]::new))
                .join();
    }

    private void writeSheet(Sheet sheet) {
        try {
            File outFile = new File(options.getOutDir(), sheet.getSheetName() + ".dson");
            FileWriter fileWriter = new FileWriter(outFile, false);
            converter.writeAsDson(sheet, DsonMode.RELAXED, TypeArgInfo.of(Sheet.class), fileWriter);
        } catch (Exception e) {
            ExceptionUtils.rethrow(e);
        }
    }
}