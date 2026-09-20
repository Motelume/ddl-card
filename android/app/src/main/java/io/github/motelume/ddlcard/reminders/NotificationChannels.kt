package io.github.motelume.ddlcard.reminders

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context

object NotificationChannels {
    const val DEADLINES = "deadline_reminders"
    fun ensureCreated(context: Context) {
        context.getSystemService(NotificationManager::class.java).createNotificationChannel(
            NotificationChannel(DEADLINES, "DDL 截止提醒", NotificationManager.IMPORTANCE_HIGH).apply {
                description = "在设定的时间提醒即将截止的事项"
            }
        )
    }
}

