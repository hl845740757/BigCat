package cn.wjybxx.common.dson.document.codecs;

import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/4/27
 */
@SuppressWarnings("rawtypes")
public class MapAsObjectCodec implements DocumentPojoCodecImpl<Map> {

    @Nonnull
    @Override
    public String getTypeName() {
        return "Map";
    }

    @Nonnull
    @Override
    public Class<Map> getEncoderClass() {
        return Map.class;
    }

    @Override
    public boolean isWriteAsArray() {
        return false;
    }

    @Override
    public void writeObject(Map instance, DocumentObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        @SuppressWarnings("unchecked") Set<Map.Entry<?, ?>> entrySet = instance.entrySet();

        for (Map.Entry<?, ?> entry : entrySet) {
            String keyString = writer.encodeKey(entry.getKey());
            Object value = entry.getValue();
            if (value == null) {
                // map写为普通的Object的时候，必须要写入Null，否则containsKey会异常；要强制写入Null必须先写入Name
                writer.writeName(keyString);
                writer.writeNull(keyString);
            } else {
                writer.writeObject(keyString, value, valueArgInfo);
            }
        }
    }

    @Override
    public Map readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        Map<Object, Object> result;
        if (typeArgInfo.factory != null) {
            result = (Map<Object, Object>) typeArgInfo.factory.get();
        } else {
            result = new LinkedHashMap<>();
        }

        TypeArgInfo<?> valueArgInfo = TypeArgInfo.of(typeArgInfo.typeArg2);
        while (!reader.isAtEndOfObject()) {
            String keyString = reader.readName();
            Object key = reader.decodeKey(keyString, typeArgInfo.typeArg1);
            Object value = reader.readObject(keyString, valueArgInfo);
            result.put(key, value);
        }
        return result;
    }
}