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

using NUnit.Framework;
using Wjybxx.BigCatTool.Excel;

namespace Wjybxx.BigCatTool.Tests;

public class SheetTest
{
    private const int cmdRowIndex = 1;
    private const int typeRowIndex = 1;
    private const int nameRowIndex = 2;
    private const int commentRowIndex = 3;


    private Sheet sheet;

    [SetUp]
    public void SetUp() {
        sheet = new Sheet("Test.xlsx", "Test", 0, false);
        sheet.AddHeader(new Header("cs", "id", "id", "", nameRowIndex, 0));
        sheet.AddHeader(new Header("cs", "string", "name", "", nameRowIndex, 1));
    }

    [Test]
    public void TestInsert() {
        int rowIndex = 4;
        sheet.AddRow(new SheetRow(rowIndex));
        SheetRow sheetRow = sheet.GetRow(rowIndex);
        sheetRow.SetValue("id", "1002");
        sheetRow.SetValue("name", "物品1002");
        // 插入前部
        sheet.AddRow(new SheetRow(rowIndex));
        sheetRow = sheet.GetRow(rowIndex);
        sheetRow.SetValue("id", "1001");
        sheetRow.SetValue("name", "物品1001");
        // 插到末尾
        sheet.AddRow(new SheetRow(rowIndex + 2));
        sheetRow = sheet.GetRow(rowIndex);
        sheetRow.SetValue("id", "1003");
        sheetRow.SetValue("name", "物品1003");
    }
}