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

package cn.wjybxx.common.config;

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * 用于从json等格式中读取sheet
 *
 * @author wjybxx
 * date - 2023/4/17
 */
@SuppressWarnings("unused")
public class SheetCodec implements DocumentPojoCodecImpl<Sheet> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "Sheet";
    }

    @Nonnull
    @Override
    public Class<Sheet> getEncoderClass() {
        return Sheet.class;
    }

    @Override
    public Sheet readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        String fileName = reader.readString("fileName");
        String sheetName = reader.readString("sheetName");
        int sheetIndex = reader.readInt("sheetIndex");
        Map<String, Header> headerMap = readHeaderMap(reader);
        List<SheetRow> valueRowList = readValueRowList(reader, headerMap);
        return new Sheet(fileName, sheetName, sheetIndex, headerMap, valueRowList);
    }

    @Override
    public void writeObject(Sheet sheet, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        writer.writeString("fileName", sheet.getFileName());
        writer.writeString("sheetName", sheet.getSheetName());
        writer.writeInt("sheetIndex", sheet.getSheetIndex());
        writeHeaderMap(writer, sheet.getHeaderMap());
        writeValueRowList(writer, sheet.getValueRowList());
    }

    //

    private static void writeHeaderMap(DocumentObjectWriter writer, Map<String, Header> headerMap) {
        // 只写values
        writer.writeStartArray("headerMap", headerMap.values(), TypeArgInfo.ARRAYLIST);
        for (Header header : headerMap.values()) {
            writer.writeStartObject(header, TypeArgInfo.OBJECT);
            writer.writeString("args", header.getArgs());
            writer.writeString("name", header.getName());
            writer.writeString("type", header.getType());
            writer.writeString("comment", header.getComment());
            writer.writeInt("rowIndex", header.getRowIndex());
            writer.writeInt("colIndex", header.getColIndex());
            writer.writeEndObject();
        }
        writer.writeEndArray();
    }

    private static Map<String, Header> readHeaderMap(DocumentObjectReader reader) {
        Map<String, Header> headerMap = new LinkedHashMap<>();
        // headerMap写的是数组格式
        reader.readStartArray("headerMap", TypeArgInfo.ARRAYLIST);
        while (!reader.isAtEndOfObject()) {
            reader.readStartObject(TypeArgInfo.OBJECT);
            Header header = new Header(reader.readString("args"),
                    reader.readString("name"),
                    reader.readString("type"),
                    reader.readString("comment"),
                    reader.readInt("rowIndex"),
                    reader.readInt("colIndex"));
            headerMap.put(header.getName(), header);
            reader.readEndObject();
        }
        reader.readEndArray();
        return headerMap;
    }

    //
    private static void writeValueRowList(DocumentObjectWriter writer, List<SheetRow> valueRowList) {
        // 其实将rowIndex看做key写成对象会更有效，但兼容性可能不好
        writer.writeStartArray("valueRowList", valueRowList, TypeArgInfo.ARRAYLIST);
        for (SheetRow valueRow : valueRowList) {
            writer.writeStartObject(valueRow, TypeArgInfo.OBJECT);
            writer.writeInt("rowIndex", valueRow.getRowIndex());
            {
                // name2CellMap写成kv的pair数组
                writer.writeStartArray("name2CellMap", valueRow.getName2CellMap(), TypeArgInfo.ARRAYLIST);
                for (SheetCell cell : valueRow.getName2CellMap().values()) {
                    writer.writeStartObject(cell, TypeArgInfo.OBJECT);
                    writer.writeString("name", cell.getName());
                    writer.writeString("value", cell.getValue());
                    writer.writeEndObject();
                }
                writer.writeEndArray();
            }
            writer.writeEndObject();
        }
        writer.writeEndArray();
    }

    private static List<SheetRow> readValueRowList(DocumentObjectReader reader, Map<String, Header> headerMap) {
        // valueRowList写的是数组格式
        ArrayList<SheetRow> valueRowList = new ArrayList<>();
        reader.readStartArray("valueRowList", TypeArgInfo.ARRAYLIST);
        while (!reader.isAtEndOfObject()) {
            reader.readStartObject(TypeArgInfo.OBJECT);
            int rowIndex = reader.readInt("rowIndex");
            Map<String, SheetCell> name2CellMap = new LinkedHashMap<>();
            {
                // name2CellMap是一个嵌套数组
                reader.readStartArray("name2CellMap", TypeArgInfo.ARRAYLIST);
                while (!reader.isAtEndOfObject()) {
                    reader.readStartObject(TypeArgInfo.OBJECT);
                    String name = reader.readString("name");
                    String value = reader.readString("value");
                    reader.readEndObject();

                    Header header = headerMap.get(name);
                    name2CellMap.put(name, new SheetCell(value, header));
                }
                reader.readEndArray();
            }
            reader.readEndObject();
            valueRowList.add(new SheetRow(rowIndex, name2CellMap));
        }
        reader.readEndArray();
        valueRowList.trimToSize();
        return valueRowList;
    }
    //
}