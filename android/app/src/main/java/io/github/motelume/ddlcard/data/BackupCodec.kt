package io.github.motelume.ddlcard.data

import io.github.motelume.ddlcard.model.DeadlineTask
import org.json.JSONArray
import org.json.JSONObject
import java.time.Instant

data class BackupPreview(val tasks: List<DeadlineTask>, val reminderOffsets: List<Int>)

object BackupCodec {
    fun encode(tasks: List<DeadlineTask>, reminders: List<Int>): String = JSONObject().apply {
        put("schemaVersion", 1)
        put("exportedAtUtc", Instant.now().toString())
        put("sourcePlatform", "android")
        put("tasks", JSONArray().apply { tasks.forEach { put(JSONObject(TaskJsonCodec.encode(it))) } })
        put("settings", JSONObject().apply {
            put("schemaVersion", 1); put("themeId", "midnight"); put("accentColor", "#7C8CFF")
            put("cardOpacity", 0.94); put("cornerRadius", 22.0); put("fontScale", 1.0)
            put("cardWidth", 420.0); put("cardHeight", 560.0); put("cardLeft", 80.0); put("cardTop", 80.0)
            put("alwaysOnTop", false); put("edgeCollapseEnabled", false); put("startWithWindows", true)
            put("defaultReminderOffsetsMinutes", JSONArray(reminders))
        })
    }.toString(2)

    fun decode(json: String): BackupPreview {
        val root = JSONObject(json)
        require(root.optInt("schemaVersion") == 1) { "不支持此备份版本" }
        val taskArray = root.optJSONArray("tasks") ?: JSONArray()
        val tasks = (0 until taskArray.length()).map { TaskJsonCodec.decode(taskArray.getJSONObject(it).toString()) }
        val reminders = root.optJSONObject("settings")?.optJSONArray("defaultReminderOffsetsMinutes") ?: JSONArray()
        return BackupPreview(tasks, (0 until reminders.length()).map { reminders.getInt(it) }.filter { it >= 0 })
    }
}
