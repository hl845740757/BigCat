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

package cn.wjybxx.bigcat.excel;

/**
 * 导表工具选项
 *
 * @author wjybxx
 * date - 2023/10/14
 */
public class ExcelExporterOptions {

    /** excel文件夹 */
    private String excelDir;
    /** 导出文件夹（输出文件夹） */
    private String outDir;
    /** 在导出excel前是否可清理输出文件夹 */
    private boolean cleanOutDir = true;
    /** 是否写入空白单元格 -- 仅对普通表格有效 */
    private boolean writeEmptyCell = true;
    /** 读表选项 */
    private ExcelReaderOptions readerOptions = ExcelReaderOptions.DEFAULT;
    /** 表格校验 */
    private ExcelValidator validator;

    public String getExcelDir() {
        return excelDir;
    }

    public ExcelExporterOptions setExcelDir(String excelDir) {
        this.excelDir = excelDir;
        return this;
    }

    public String getOutDir() {
        return outDir;
    }

    public ExcelExporterOptions setOutDir(String outDir) {
        this.outDir = outDir;
        return this;
    }

    public boolean isCleanOutDir() {
        return cleanOutDir;
    }

    public ExcelExporterOptions setCleanOutDir(boolean cleanOutDir) {
        this.cleanOutDir = cleanOutDir;
        return this;
    }

    public ExcelReaderOptions getReaderOptions() {
        return readerOptions;
    }

    public ExcelExporterOptions setReaderOptions(ExcelReaderOptions readerOptions) {
        this.readerOptions = readerOptions;
        return this;
    }

    public ExcelValidator getValidator() {
        return validator;
    }

    public ExcelExporterOptions setValidator(ExcelValidator validator) {
        this.validator = validator;
        return this;
    }

    public boolean isWriteEmptyCell() {
        return writeEmptyCell;
    }

    public ExcelExporterOptions setWriteEmptyCell(boolean writeEmptyCell) {
        this.writeEmptyCell = writeEmptyCell;
        return this;
    }
}