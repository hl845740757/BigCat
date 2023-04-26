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

package cn.wjybxx.common.time;

import javax.annotation.concurrent.Immutable;
import java.time.DayOfWeek;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;

/**
 * 主要封装与时区相关的方法
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Immutable
public final class TimeHelper {

    /**
     * 中国时区对应的辅助类实例（不直接开放，避免造成混乱）
     */
    private static final TimeHelper CST = new TimeHelper(TimeUtils.ZONE_OFFSET_CST);

    /**
     * UTC时区对于的辅助类实例（不直接开放，避免造成混乱）
     */
    private static final TimeHelper UTC = new TimeHelper(TimeUtils.ZONE_OFFSET_UTC);

    /**
     * 系统时区对应的辅助类实例
     */
    public static final TimeHelper SYSTEM = new TimeHelper(TimeUtils.ZONE_OFFSET_SYSTEM);

    private final ZoneOffset zoneOffset;

    private TimeHelper(ZoneOffset zoneOffset) {
        this.zoneOffset = zoneOffset;
    }

    public static TimeHelper of(ZoneOffset zoneOffset) {
        if (zoneOffset.equals(CST.zoneOffset)) {
            return CST;
        }
        if (zoneOffset.equals(SYSTEM.zoneOffset)) {
            return SYSTEM;
        }
        if (zoneOffset.equals(UTC.zoneOffset)) {
            return UTC;
        }
        return new TimeHelper(zoneOffset);
    }

    /** 获取时区的毫秒偏移量 */
    public long getOffsetMilli() {
        return zoneOffset.getTotalSeconds() * 1000L;
    }

    /**
     * 将{@link LocalDateTime}转换为时区无关的毫秒时间戳。
     * 注意：如果不是该类返回的{@link LocalDateTime}，则可能在时区上出现故障。
     */
    public long toEpochMilli(LocalDateTime localDateTime) {
        final long millis = localDateTime.getNano() / TimeUtils.NANOS_PER_MILLI;
        return localDateTime.toEpochSecond(zoneOffset) * 1000L + millis;
    }

    /**
     * 计算在本地时区下的纪元天数
     *
     * @param epochMilli 毫秒时间
     * @return 本地时区下的天数
     */
    public int toLocalEpochDay(long epochMilli) {
        // 为节省开销，这段代码参考自{@link java.time.LocalDate#ofInstant(Instant, ZoneId)}
        final long localSecond = (epochMilli / 1000) + zoneOffset.getTotalSeconds();
        final long localEpochDay = Math.floorDiv(localSecond, TimeUtils.SECONDS_PER_DAY);
        return Math.toIntExact(localEpochDay); // 暂时仍使用int
    }

    /**
     * 将毫秒时间转换为{@link LocalDateTime}
     *
     * @param epochMilli 毫秒时间
     * @return LocalDateTime
     */
    public LocalDateTime toLocalDateTime(long epochMilli) {
        final long extraMilli = epochMilli % 1000;
        final int nanoOfSecond = (int) (extraMilli * TimeUtils.NANOS_PER_MILLI);
        return LocalDateTime.ofEpochSecond(epochMilli / 1000, nanoOfSecond, zoneOffset);
    }

    /**
     * 将毫秒时间转换为{@link LocalDateTime}，并忽略毫秒。
     *
     * @param epochMilli 毫秒时间
     * @return LocalDateTime
     */
    public LocalDateTime toLocalDateTimeIgnoreMs(long epochMilli) {
        return LocalDateTime.ofEpochSecond(epochMilli / 1000, 0, zoneOffset);
    }

    /**
     * 将 毫秒时间 格式化为 默认字符串格式{@link TimeUtils#DEFAULT_PATTERN}
     *
     * @param epochMilli 毫秒时间
     * @return 格式化后的字符串表示
     */
    public String formatTime(long epochMilli) {
        return formatTime(epochMilli, TimeUtils.DEFAULT_FORMATTER);
    }

    /**
     * 将 毫秒时间 格式化为 指定格式
     *
     * @param epochMilli 毫秒时间
     * @param formatter  时间格式器
     * @return 格式化后的字符串表示
     */
    public String formatTime(long epochMilli, DateTimeFormatter formatter) {
        LocalDateTime localDateTime = toLocalDateTime(epochMilli);
        return formatter.format(localDateTime);
    }

    public String formatTime(LocalDateTime localDateTime, DateTimeFormatter formatter) {
        return formatter.format(localDateTime);
    }

    /**
     * 将字符串格式的时间转换为本地时间
     */
    public LocalDateTime parseLocalDateTime(String dateString, DateTimeFormatter formatter) {
        return LocalDateTime.parse(dateString, formatter);
    }

    /**
     * 解析为毫秒时间戳
     *
     * @param dateString {@link TimeUtils#DEFAULT_PATTERN}格式的字符串
     * @return milli
     */
    public long parseTimeMillis(String dateString) {
        return toEpochMilli(LocalDateTime.parse(dateString, TimeUtils.DEFAULT_FORMATTER));
    }

    /**
     * 解析为毫秒时间戳
     *
     * @param dateString {@link TimeUtils#DEFAULT_PATTERN}格式的字符串
     * @return milli
     */
    public long parseTimeMillis(String dateString, DateTimeFormatter formatter) {
        return toEpochMilli(LocalDateTime.parse(dateString, formatter));
    }

    /**
     * 获取指定时间戳所在日期的00:00:00的毫秒时间戳
     *
     * @param epochMilli 指定时间戳，用于确定日期
     * @return midnight of special day
     */
    public long getTimeBeginOfToday(long epochMilli) {
        final LocalDateTime localDateTime = toLocalDateTimeIgnoreMs(epochMilli)
                .with(TimeUtils.START_OF_DAY);
        return toEpochMilli(localDateTime);
    }

    /**
     * 获取指定时间戳所在日期的23:59:59的毫秒时间戳
     *
     * @param epochMilli 指定时间戳，用于确定日期
     * @return end time of special day
     */
    public long getTimeEndOfToday(long epochMilli) {
        final LocalDateTime localDateTime = toLocalDateTimeIgnoreMs(epochMilli)
                .with(TimeUtils.END_OF_DAY);
        return toEpochMilli(localDateTime);
    }

    /**
     * 获取指定时间戳所在周的周一00:00:00的毫秒时间戳
     *
     * @param epochMilli 指定时间戳，用于确定所在的周
     */
    public long getTimeBeginOfWeek(long epochMilli) {
        final LocalDateTime startOfDay = toLocalDateTimeIgnoreMs(epochMilli)
                .with(TimeUtils.START_OF_DAY);
        final int deltaDay = startOfDay.getDayOfWeek().getValue() - DayOfWeek.MONDAY.getValue();
        return toEpochMilli(startOfDay) - (deltaDay * TimeUtils.DAY);
    }

    /**
     * 获取指定时间戳所在周的周日23:59:59.999的毫秒时间戳
     *
     * @param epochMilli 指定时间戳，用于确定所在的周
     */
    public long getTimeEndOfWeek(long epochMilli) {
        return getTimeBeginOfWeek(epochMilli) + TimeUtils.WEEK - 1;
    }

    /**
     * 获取本月的开始时间戳
     * 本月第一天的 00:00:00.000
     */
    public long getTimeBeginOfMonth(long epochMilli) {
        final LocalDateTime firstDayOfMonth = toLocalDateTimeIgnoreMs(epochMilli)
                .with(TimeUtils.START_OF_DAY)
                .withDayOfMonth(1);
        return toEpochMilli(firstDayOfMonth);
    }

    /**
     * 获取本月的结束时间戳
     * 本月最后一天的23:59:59.999
     */
    public long getTimeEndOfMonth(long epochMilli) {
        final LocalDateTime endOfDay = toLocalDateTimeIgnoreMs(epochMilli).with(TimeUtils.END_OF_DAY);
        final LocalDateTime lastDayOfMonth = endOfDay.withDayOfMonth(TimeUtils.lengthOfMonth(endOfDay));
        return toEpochMilli(lastDayOfMonth);
    }

    /**
     * 获取月的开始时间戳
     *
     * @param deltaMonth 月份差值
     */
    public long getTimeBeginOfMonth(long epochMilli, int deltaMonth) {
        final LocalDateTime firstDayOfNextMonth = toLocalDateTimeIgnoreMs(epochMilli)
                .with(TimeUtils.START_OF_DAY)
                .withDayOfMonth(1) // 放在加月份前面，可以避免奇怪的语义
                .plusMonths(deltaMonth);
        return toEpochMilli(firstDayOfNextMonth);
    }

    /**
     * 获取月的结束时间戳
     *
     * @param deltaMonth 月份差值
     */
    public long getTimeEndOfMonth(long epochMilli, int deltaMonth) {
        final LocalDateTime firstDayOfNextMonth = toLocalDateTimeIgnoreMs(epochMilli)
                .with(TimeUtils.END_OF_DAY)
                .withDayOfMonth(1) // 放在加月份前面，可以避免奇怪的语义
                .plusMonths(deltaMonth);
        // 需要使用指定月份的长度
        final LocalDateTime lastDayOfMonth = firstDayOfNextMonth.withDayOfMonth(TimeUtils.lengthOfMonth(firstDayOfNextMonth));
        return toEpochMilli(lastDayOfMonth);
    }

    /**
     * 判断两个时间是否是同一天
     * （同样的两个时间戳，在不同的时区，结果可能不同）
     *
     * @param time1 第一个时间戳
     * @param time2 第二个时间戳
     * @return true/false，如果是同一天则返回true，否则返回false。
     */
    public boolean isSameDay(long time1, long time2) {
        return toLocalEpochDay(time1) == toLocalEpochDay(time2);
    }

    /**
     * 计算两个时间戳相差的天数
     *
     * @param time1 第一个时间戳
     * @param time2 第二个时间戳
     * @return >=0,同一天返回0，否则返回值大于0
     */
    public int differDays(long time1, long time2) {
        return Math.abs(toLocalEpochDay(time1) - toLocalEpochDay(time2));
    }

    /**
     * 判断两个时间是否是同一周
     *
     * @param time1 第一个时间戳
     * @param time2 第二个时间戳
     * @return true/false，如果是同一周则返回true，否则返回false。
     */
    public boolean isSameWeek(long time1, long time2) {
        return getTimeBeginOfWeek(time1) == getTimeBeginOfWeek(time2);
    }

    /**
     * 计算两个时间戳相差的周数
     *
     * @param time1 第一个时间戳
     * @param time2 第二个时间戳
     * @return >=0,同一周返回0，否则返回值大于0
     */
    public int differWeeks(long time1, long time2) {
        final long deltaTime = getTimeBeginOfWeek(time1) - getTimeBeginOfWeek(time2);
        return (int) Math.abs(deltaTime / TimeUtils.WEEK);
    }

}