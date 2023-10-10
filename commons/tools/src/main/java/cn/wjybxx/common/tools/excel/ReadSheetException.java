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

/**
 * @author wjybxx
 * date - 2023/4/16
 */
public class ReadSheetException extends RuntimeException {

    private final String fileName;
    private final String sheetName;
    private final int sheetIndex;

    public ReadSheetException(String fileName, String sheetName, int sheetIndex, Exception e) {
        super(formatMsg(fileName, sheetName, sheetIndex), e);
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
    }

    public ReadSheetException(String fileName, String sheetName, int sheetIndex, String message) {
        super(formatMsg(fileName, sheetName, sheetIndex) + ", " + message);
        this.fileName = fileName;
        this.sheetName = sheetName;
        this.sheetIndex = sheetIndex;
    }

    private static String formatMsg(String fileName, String sheetName, int sheetIndex) {
        return String.format("fileName: %s, sheetName: %s, sheetIndex: %d", fileName, sheetName, sheetIndex);
    }

    public String getFileName() {
        return fileName;
    }

    public String getSheetName() {
        return sheetName;
    }

    public int getSheetIndex() {
        return sheetIndex;
    }
}