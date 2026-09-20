package io.github.motelume.ddlcard.reminders

import android.app.AlarmManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import io.github.motelume.ddlcard.model.DeadlineTask

object ReminderScheduler {
    fun scheduleTask(context: Context, task: DeadlineTask) {
        val alarm = context.getSystemService(AlarmManager::class.java)
        task.reminderOffsetsMinutes.forEach { offset ->
            val triggerAt = task.dueAtUtc - offset * 60_000L
            if (triggerAt <= System.currentTimeMillis()) return@forEach
            val intent = Intent(context, ReminderReceiver::class.java).apply {
                putExtra("taskId", task.id); putExtra("title", task.title); putExtra("dueAtUtc", task.dueAtUtc)
            }
            val requestCode = (task.id + ":" + task.dueAtUtc + ":" + offset).hashCode()
            val pending = PendingIntent.getBroadcast(context, requestCode, intent, PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
            if (alarm.canScheduleExactAlarms()) alarm.setExactAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAt, pending)
            else alarm.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, triggerAt, pending)
        }
    }

    suspend fun rescheduleAll(context: Context) {
        io.github.motelume.ddlcard.AppContainer.initialize(context)
        io.github.motelume.ddlcard.AppContainer.tasks.getActive().forEach { scheduleTask(context, it) }
    }
}

