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

package cn.wjybxx.common.mongo;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.time.TimeUtils;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.types.ObjectRef;
import cn.wjybxx.dson.types.OffsetTimestamp;
import org.bson.*;

import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/9/27
 * @deprecated 转换规则与项目相关
 */
@Deprecated
public class Bson2DsonUtils {

    public static DsonObject<String> toDsonObject(BsonDocument document) {
        BsonValue clsName = document.get("_class");
        DsonObject<String> dsonObject = new DsonObject<>(CollectionUtils.capacity(document.size()));
        if (clsName != null && clsName.getBsonType() == BsonType.STRING) {
            dsonObject.getHeader().append(DsonHeader.NAMES_CLASS_NAME, new DsonString(clsName.asString().getValue()));
        }
        for (Map.Entry<String, BsonValue> entry : document.entrySet()) {
            String key = entry.getKey();
            BsonValue value = entry.getValue();
            if (value.getBsonType() == BsonType.STRING && key.equals("_class")) {
                continue;
            }
            dsonObject.put(key, toDsonValue(value));
        }
        return dsonObject;
    }

    public static DsonArray<String> toDsonArray(BsonArray bsonArray) {
        DsonArray<String> dsonArray = new DsonArray<>(bsonArray.size());
        for (BsonValue bsonValue : bsonArray) {
            dsonArray.add(toDsonValue(bsonValue));
        }
        return dsonArray;
    }

    public static BsonDocument toBsonDocument(DsonObject<String> dsonObject) {
        BsonDocument document = new BsonDocument(CollectionUtils.capacity(dsonObject.size() + 1));
        DsonValue clsName = dsonObject.getHeader().get(DsonHeader.NAMES_CLASS_NAME);
        if (clsName != null) {
            document.append("_class", new BsonString(clsName.asString()));
        }
        for (Map.Entry<String, DsonValue> entry : dsonObject.entrySet()) {
            document.put(entry.getKey(), toBsonValue(entry.getValue()));
        }
        return document;
    }

    public static BsonArray toBsonArray(DsonArray<String> dsonArray) {
        BsonArray bsonArray = new BsonArray(dsonArray.size());
        for (DsonValue dsonValue : dsonArray) {
            bsonArray.add(toBsonValue(dsonValue));
        }
        return bsonArray;
    }

    // region 内部实现
    private static BsonValue toBsonValue(DsonValue dsonValue) {
        return switch (dsonValue.getDsonType()) {
            case NULL -> BsonNull.VALUE;
            case INT32 -> new BsonInt32(dsonValue.asInt32());
            case INT64 -> new BsonInt64(dsonValue.asInt64());
            case FLOAT -> new BsonDouble(dsonValue.asFloat());
            case DOUBLE -> new BsonDouble(dsonValue.asDouble());
            case BOOLEAN -> BsonBoolean.valueOf(dsonValue.asBool());
            case STRING -> new BsonString(dsonValue.asString());
            case BINARY -> {
                DsonBinary binary = dsonValue.asBinary();
                yield new BsonBinary((byte) binary.getType(), binary.getData());
            }
            case EXT_INT32 -> {
                var extInt32 = dsonValue.asExtInt32();
                yield new BsonDocument(4)
                        .append("_class", new BsonString("ei"))
                        .append("type", new BsonInt32(extInt32.getType()))
                        .append("value", new BsonInt32(extInt32.getValue()));
            }
            case EXT_INT64 -> {
                var extInt64 = dsonValue.asExtInt64();
                yield new BsonDocument(4)
                        .append("_class", new BsonString("eL"))
                        .append("type", new BsonInt32(extInt64.getType()))
                        .append("value", new BsonInt64(extInt64.getValue()));
            }
            case EXT_STRING -> {
                var extString = dsonValue.asExtString();
                BsonDocument document = new BsonDocument(6)
                        .append("_class", new BsonString("es"))
                        .append("type", new BsonInt32(extString.getType()));
                if (extString.getValue() == null) {
                    document.append("value", BsonNull.VALUE);
                } else {
                    document.append("value", new BsonString(extString.getValue()));
                }
                yield document;
            }
            case REFERENCE -> {
                ObjectRef reference = dsonValue.asReference();
                BsonDocument document = new BsonDocument(6)
                        .append("_class", new BsonString("ref"))
                        .append(ObjectRef.NAMES_LOCAL_ID, new BsonString(reference.getLocalId()))
                        .append(ObjectRef.NAMES_TYPE, new BsonInt32(reference.getType()))
                        .append(ObjectRef.NAMES_POLICY, new BsonInt32(reference.getPolicy()));
                if (reference.hasNamespace()) {
                    document.append(ObjectRef.NAMES_NAMESPACE, new BsonString(reference.getNamespace()));
                }
                yield document;
            }
            case TIMESTAMP -> {
                OffsetTimestamp timestamp = dsonValue.asTimestamp();
                long millis = TimeUtils.toMillis(timestamp.getSeconds(), timestamp.convertNanosToMillis());
                yield new BsonDateTime(millis);
            }
            case ARRAY -> toBsonArray(dsonValue.asArray());
            case OBJECT -> toBsonDocument(dsonValue.asObject());
            default -> throw new IllegalArgumentException("unsupported dsonType " + dsonValue.getDsonType());
        };
    }

    private static DsonValue toDsonValue(BsonValue bsonValue) {
        return switch (bsonValue.getBsonType()) {
            case NULL -> DsonNull.NULL;
            case INT32 -> new DsonInt32(bsonValue.asInt32().getValue());
            case INT64 -> new DsonInt64(bsonValue.asInt64().getValue());
            case DOUBLE -> new DsonDouble(bsonValue.asDouble().getValue());
            case BOOLEAN -> new DsonBool(bsonValue.asBoolean().getValue());
            case STRING -> new DsonString(bsonValue.asString().getValue());
            case BINARY -> {
                BsonBinary binary = bsonValue.asBinary();
                yield new DsonBinary(binary.getType(), binary.getData());
            }
            case DATE_TIME -> new DsonTimestamp(new OffsetTimestamp(bsonValue.asDateTime().getValue()));
            case DOCUMENT -> {
                BsonDocument document = bsonValue.asDocument();
                DsonValue extType = parseExtType(document);
                if (extType != null) {
                    yield extType;
                }
                yield toDsonObject(document);
            }
            case ARRAY -> toDsonArray(bsonValue.asArray());
            default -> throw new IllegalArgumentException("unsupported bsonType " + bsonValue.getBsonType());
        };
    }

    private static DsonValue parseExtType(BsonDocument document) {
        BsonValue clsName = document.get("_class");
        if (clsName == null || clsName.getBsonType() != BsonType.STRING) {
            return null;
        }
        return switch (clsName.asString().getValue()) {
            case "ei" -> {
                BsonInt32 type = document.get("type").asInt32();
                BsonInt32 value = document.get("value").asInt32();
                yield new DsonExtInt32(type.getValue(), value.getValue());
            }
            case "eL" -> {
                BsonInt32 type = document.get("type").asInt32();
                BsonInt64 value = document.get("value").asInt64();
                yield new DsonExtInt64(type.getValue(), value.getValue());
            }
            case "es" -> {
                BsonInt32 type = document.get("type").asInt32();
                BsonValue value = document.get("value");
                if (value.getBsonType() == BsonType.NULL) {
                    yield new DsonExtString(type.getValue(), null);
                } else {
                    yield new DsonExtString(type.getValue(), value.asString().getValue());
                }
            }
            case "ref" -> {
                BsonString localId = document.get(ObjectRef.NAMES_LOCAL_ID).asString();
                BsonInt32 type = document.get(ObjectRef.NAMES_TYPE).asInt32();
                BsonInt32 policy = document.get(ObjectRef.NAMES_POLICY).asInt32();
                BsonValue bsonNs = document.get(ObjectRef.NAMES_NAMESPACE);
                String ns = bsonNs == null ? null : bsonNs.asString().getValue();
                yield new DsonReference(new ObjectRef(localId.getValue(), ns, type.getValue(), policy.getValue()));
            }
            default -> null;
        };
    }
    // endregion
}
