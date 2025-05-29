#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDataReader;
using Wjybxx.BigCatEditor.Excel;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// Excel工具类
/// </summary>
public static class ExcelUtil
{
    /// <summary>
    /// 读取Excel文件中的数据页签
    /// </summary>
    /// <returns>有效表单页</returns>
    public static List<Sheet> Read(FileInfo fileInfo, ExcelReaderOptions options) {
        using var stream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration(options.codePage)
        {
            ReturnsRawValue = true // 定时版
        });

        List<Sheet> results = new List<Sheet>();
        int sheetIndex = -1;
        do {
            sheetIndex++;
            string? sheetName = options.sheetNameParser(fileInfo.Name, reader.Name);
            if (sheetName == null) { // 非业务表
                continue;
            }
            SheetReader sheetReader = new SheetReader(fileInfo.Name, sheetName, sheetIndex, options, reader);
            try {
                Sheet? sheet = sheetReader.Read();
                if (sheet == null) { // 非业务表
                    continue;
                }
                results.Add(sheet);
            }
            catch (Exception ex) {
                throw new IOException($"fileName: {fileInfo.Name}, sheetName: {sheetName}, sheetIndex: {sheetIndex}", ex);
            }
        } while (reader.NextResult());
        return results;
    }
}
}