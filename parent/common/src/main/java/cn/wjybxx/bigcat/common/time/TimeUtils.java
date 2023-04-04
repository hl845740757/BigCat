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

package cn.wjybxx.bigcat.common.time;

import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;

/**
 * 时间工具类 -- 以毫秒为基本单位。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class TimeUtils {

    private TimeUtils() {

    }

    /**
     * 中国时区
     */
    public static final ZoneOffset ZONE_OFFSET_CST = ZoneOffset.ofHours(8);

    /**
     * UTC时间
     */
    public static final ZoneOffset ZONE_OFFSET_UTC = ZoneOffset.UTC;

    /**
     * 系统时区
     */
    public static final ZoneOffset ZONE_OFFSET_SYSTEM = ZoneOffset.systemDefault().getRules().getOffset(LocalDateTime.now());

    /**
     * 一秒的毫秒数
     */
    public static final long SEC = 1000;
    /**
     * 一分的毫秒数
     */
    public static final long MIN = 60 * SEC;
    /**
     * 一小时的毫秒数
     */
    public static final long HOUR = 60 * MIN;
    /**
     * 一天的毫秒数
     */
    public static final long DAY = 24 * HOUR;
    /**
     * 一周的毫秒数
     */
    public static final long WEEK = 7 * DAY;

    /**
     * 一天的秒数
     */
    public static final int SECONDS_PER_DAY = 86400;
    /**
     * 1毫秒多少纳秒
     */
    public static final long NANOS_PER_MILLI = 1000_000L;
    /**
     * 1秒多少纳秒
     */
    public static final long NANOS_PER_SECOND = 1000_000_000L;

    /**
     * 一天的开始：午夜 00:00:00
     * The time of midnight at the start of the day, '00:00'.
     */
    public static final LocalTime START_OF_DAY = LocalTime.MIN;
    /**
     * 一天的结束：午夜 23:59:59
     */
    public static final LocalTime END_OF_DAY = LocalTime.MAX;

    /**
     * 默认的时间格式
     */
    public static final String DEFAULT_PATTERN = "yyyy-MM-dd HH:mm:ss";
    /**
     * 默认时间格式器
     */
    public static final DateTimeFormatter DEFAULT_FORMATTER = DateTimeFormatter.ofPattern(DEFAULT_PATTERN);
    /**
     * 年月日的格式化器
     */
    public static final DateTimeFormatter YYYY_MM_DD = DateTimeFormatter.ofPattern("yyyy-MM-dd");
    /**
     * 时分秒的格式化器
     */
    public static final DateTimeFormatter HH_MM_SS = DateTimeFormatter.ofPattern("HH:mm:ss");
    /**
     * 时分的格式化器
     */
    public static final DateTimeFormatter HH_MM = DateTimeFormatter.ofPattern("HH:mm");

    /** {@link LocalTime}对应的毫秒时间 -- 常用API */
    public static long toMilliOfDay(LocalTime time) {
        return time.toSecondOfDay() * 1000L + time.getNano() / NANOS_PER_MILLI;
    }

    /** 获取月份的最后一天，总是忘记api... */
    public static int lengthOfMonth(LocalDateTime localDateTime) {
        return localDateTime.toLocalDate().lengthOfMonth();
    }

}