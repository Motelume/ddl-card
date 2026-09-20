package io.github.motelume.ddlcard

import android.app.Application
import io.github.motelume.ddlcard.reminders.NotificationChannels
import io.github.motelume.ddlcard.widget.WidgetUpdateWorker

class DDLCardApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        AppContainer.initialize(this)
        NotificationChannels.ensureCreated(this)
        WidgetUpdateWorker.schedule(this)
    }
}
