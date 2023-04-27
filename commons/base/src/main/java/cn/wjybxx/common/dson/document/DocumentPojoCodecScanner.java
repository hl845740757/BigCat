/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.ClassScanner;
import org.apache.commons.lang3.ArrayUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;

import java.lang.reflect.Modifier;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/5
 */
public class DocumentPojoCodecScanner {

    public static List<? extends DocumentPojoCodecImpl<?>> scan(Set<String> packages) {
        return packages.stream()
                .map(scanPackage -> ClassScanner.findClasses(scanPackage, name -> true, DocumentPojoCodecScanner::isPojoCodecImpl))
                .flatMap(Collection::stream)
                .map(DocumentPojoCodecScanner::newInstance)
                .collect(Collectors.toList());
    }

    private static DocumentPojoCodecImpl<?> newInstance(Class<?> clazz) {
        try {
            return (DocumentPojoCodecImpl<?>) Arrays.stream(clazz.getDeclaredConstructors())
                    .filter(constructor -> constructor.getParameterCount() == 0)
                    .findFirst()
                    .orElseThrow()
                    .newInstance(ArrayUtils.EMPTY_OBJECT_ARRAY);
        } catch (Exception e) {
            return ExceptionUtils.rethrow(e);
        }
    }

    private static boolean isPojoCodecImpl(Class<?> clazz) {
        if (Modifier.isAbstract(clazz.getModifiers())) {
            return false;
        }
        if (!DocumentPojoCodecImpl.class.isAssignableFrom(clazz)) {
            return false;
        }
        if (clazz.isAnnotationPresent(DocumentPojoCodecScanIgnore.class)) {
            return false;
        }
        return hasNoArgsConstructor(clazz);
    }

    private static boolean hasNoArgsConstructor(Class<?> clazz) {
        return Arrays.stream(clazz.getDeclaredConstructors())
                .anyMatch(constructor -> constructor.getParameterCount() == 0);
    }

}