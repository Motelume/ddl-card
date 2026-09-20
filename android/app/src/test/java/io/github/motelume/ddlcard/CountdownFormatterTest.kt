package io.github.motelume.ddlcard

import io.github.motelume.ddlcard.core.CountdownFormatter
import org.junit.Assert.assertEquals
import org.junit.Test

class CountdownFormatterTest {
    @Test fun formatsDaysHoursMinutes() {
        val now = 1_000_000L
        val due = now + (2 * 1440 + 3 * 60 + 7) * 60_000L
        assertEquals("还剩 2天 3小时 7分钟", CountdownFormatter.format(due, now))
    }

    @Test fun formatsOverdueWithoutDays() {
        val now = 10_000_000L
        assertEquals("已逾期 2小时 15分钟", CountdownFormatter.format(now - 135 * 60_000L, now))
    }
}
