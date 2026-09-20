package io.github.motelume.ddlcard.reminders

import android.Manifest
import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import io.github.motelume.ddlcard.MainActivity
import io.github.motelume.ddlcard.R
import io.github.motelume.ddlcard.AppContainer
import io.github.motelume.ddlcard.core.CountdownFormatter
import io.github.motelume.ddlcard.model.TaskState
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class ReminderReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val taskId = intent.getStringExtra("taskId") ?: return
        val scheduledDueAt = intent.getLongExtra("dueAtUtc", -1L)
        val pendingResult = goAsync()
        CoroutineScope(Dispatchers.IO).launch {
            try {
                AppContainer.initialize(context)
                val task = AppContainer.tasks.getById(taskId)
                if (task == null || task.state != TaskState.ACTIVE || task.dueAtUtc != scheduledDueAt) return@launch
                if (android.os.Build.VERSION.SDK_INT >= 33 && context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) return@launch
                NotificationChannels.ensureCreated(context)
                val open = PendingIntent.getActivity(context, taskId.hashCode(), Intent(context, MainActivity::class.java), PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
                val notification = NotificationCompat.Builder(context, NotificationChannels.DEADLINES)
                    .setSmallIcon(R.drawable.ic_ddlcard).setContentTitle(task.title)
                    .setContentText(CountdownFormatter.format(task.dueAtUtc)).setPriority(NotificationCompat.PRIORITY_HIGH)
                    .setAutoCancel(true).setContentIntent(open).build()
                NotificationManagerCompat.from(context).notify((taskId + task.dueAtUtc).hashCode(), notification)
            } finally {
                pendingResult.finish()
            }
        }
    }
}
