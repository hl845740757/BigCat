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


import cn.wjybxx.common.codec.TypeArgInfo;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;

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

    public SheetCodec() {
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
    public void writeObject(DocumentObjectWriter writer, Sheet sheet, TypeArgInfo<?> typeArgInfo, ObjectStyle style) {
        writer.writeString("fileName", sheet.getFileName());
        writer.writeString("sheetName", sheet.getSheetName());
        writer.writeInt("sheetIndex", sheet.getSheetIndex());

        boolean isParamSheet = sheet.isParamSheet();
        writeHeaderMap(writer, sheet.getHeaderMap(), isParamSheet);
        writeValueRowList(writer, sheet.getValueRowList(), isParamSheet);
    }

    //

    private void writeHeaderMap(DocumentObjectWriter writer, Map<String, Header> headerMap, boolean isParamSheet) {
        // 只写values
        writer.writeStartArray("headerMap", headerMap.values(), TypeArgInfo.ARRAYLIST);
        for (Header header : headerMap.values()) {
            writer.writeStartObject(header, TypeArgInfo.OBJECT, ObjectStyle.FLOW);
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

    private Map<String, Header> readHeaderMap(DocumentObjectReader reader) {
        Map<String, Header> headerMap = new LinkedHashMap<>();
        // headerMap写的是数组格式
        reader.readStartArray("headerMap", TypeArgInfo.ARRAYLIST);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
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
    private void writeValueRowList(DocumentObjectWriter writer, List<SheetRow> valueRowList, boolean isParamSheet) {
        // 其实将rowIndex看做key写成对象会更有效，但兼容性可能不好
        writer.writeStartArray("valueRowList", valueRowList, TypeArgInfo.ARRAYLIST);
        for (SheetRow valueRow : valueRowList) {
            writer.writeStartObject(valueRow, TypeArgInfo.OBJECT);
            writer.writeInt("rowIndex", valueRow.getRowIndex());
            {
                // cellMap写为Map
                ObjectStyle style = isParamSheet ? ObjectStyle.FLOW : ObjectStyle.INDENT;
                writer.writeStartObject("cellMap", valueRow.getName2CellMap(), TypeArgInfo.STRING_HASHMAP, style);
                for (SheetCell cell : valueRow.getName2CellMap().values()) {
                    writer.writeString(cell.getName(), cell.getValue(), StringStyle.QUOTE);
                }
                writer.writeEndObject();
            }
            writer.writeEndObject();
        }
        writer.writeEndArray();
    }

    private List<SheetRow> readValueRowList(DocumentObjectReader reader, Map<String, Header> headerMap) {
        // valueRowList写的是数组格式
        ArrayList<SheetRow> valueRowList = new ArrayList<>();
        reader.readStartArray("valueRowList", TypeArgInfo.ARRAYLIST);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            reader.readStartObject(TypeArgInfo.OBJECT);
            int rowIndex = reader.readInt("rowIndex");

            // name2CellMap是一个嵌套Object
            Map<String, SheetCell> name2CellMap = new LinkedHashMap<>();
            {
                reader.readStartObject("cellMap", TypeArgInfo.STRING_HASHMAP);
                while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                    String name = reader.readName();
                    String value = reader.readString(name);
                    Header header = headerMap.get(name);
                    name2CellMap.put(name, new SheetCell(value, header));
                }
                reader.readEndObject();
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