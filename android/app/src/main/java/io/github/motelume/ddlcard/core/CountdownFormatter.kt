package io.github.motelume.ddlcard.core

import kotlin.math.abs

object CountdownFormatter {
    fun format(dueAtUtc: Long, nowUtc: Long = System.currentTimeMillis()): String {
        val overdue = dueAtUtc < nowUtc
        val totalMinutes = abs(dueAtUtc - nowUtc) / 60_000
        val days = totalMinutes / 1_440
        val hours = totalMinutes % 1_440 / 60
        val minutes = totalMinutes % 60
        val value = if (days > 0) "${days}天 ${hours}小时 ${minutes}分钟" else "${hours}小时 ${minutes}分钟"
        return if (overdue) "已逾期 $value" else "还剩 $value"
    }
}

