package io.github.motelume.ddlcard

import android.content.Context
import io.github.motelume.ddlcard.data.AppPreferences
import io.github.motelume.ddlcard.data.TaskRepository

object AppContainer {
    lateinit var tasks: TaskRepository
        private set
    lateinit var preferences: AppPreferences
        private set

    fun initialize(context: Context) {
        if (!::tasks.isInitialized) {
            tasks = TaskRepository(context.applicationContext)
            preferences = AppPreferences(context.applicationContext)
        }
    }
}
