package io.github.motelume.ddlcard.data

import android.content.Context

class AppPreferences(context: Context) {
    private val values = context.getSharedPreferences("ddlcard_settings", Context.MODE_PRIVATE)

    var defaultReminderOffsetsMinutes: List<Int>
        get() = values.getString(KEY_REMINDERS, null)
            ?.split(',')?.mapNotNull(String::toIntOrNull)?.filter { it >= 0 }
            ?.takeIf(List<Int>::isNotEmpty)
            ?: listOf(10080, 4320, 1440, 60, 10)
        set(value) {
            values.edit().putString(KEY_REMINDERS, value.distinct().sortedDescending().joinToString(",")).apply()
        }

    private companion object { const val KEY_REMINDERS = "default_reminder_offsets_minutes" }
}
