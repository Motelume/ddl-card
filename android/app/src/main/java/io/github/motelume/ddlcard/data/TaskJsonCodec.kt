package io.github.motelume.ddlcard.data

import io.github.motelume.ddlcard.model.DeadlineSubtask
import io.github.motelume.ddlcard.model.DeadlineTask
import io.github.motelume.ddlcard.model.TaskPriority
import io.github.motelume.ddlcard.model.TaskState
import org.json.JSONArray
import org.json.JSONObject

object TaskJsonCodec {
    fun encode(task: DeadlineTask): String = JSONObject().apply {
        put("id", task.id)
        put("title", task.title)
        put("description", task.description)
        put("category", task.category)
        put("accentColor", task.accentColor)
        put("dueAtUtc", java.time.Instant.ofEpochMilli(task.dueAtUtc).toString())
        put("timeZoneId", task.timeZoneId)
        put("priority", task.priority.name.lowercase())
        put("state", task.state.name.lowercase())
        put("manualProgressPercent", task.manualProgressPercent)
        put("createdAtUtc", java.time.Instant.ofEpochMilli(task.createdAtUtc).toString())
        put("updatedAtUtc", java.time.Instant.ofEpochMilli(task.updatedAtUtc).toString())
        put("completedAtUtc", task.completedAtUtc?.let { java.time.Instant.ofEpochMilli(it).toString() })
        put("subtasks", JSONArray().apply {
            task.subtasks.forEach { subtask -> put(JSONObject().apply {
                put("id", subtask.id); put("title", subtask.title); put("isCompleted", subtask.isCompleted); put("position", subtask.position)
            }) }
        })
        put("reminderOffsetsMinutes", JSONArray(task.reminderOffsetsMinutes))
    }.toString()

    fun decode(json: String): DeadlineTask {
        val root = JSONObject(json)
        val subtasksJson = root.optJSONArray("subtasks") ?: JSONArray()
        val remindersJson = root.optJSONArray("reminderOffsetsMinutes") ?: JSONArray()
        return DeadlineTask(
            id = root.getString("id"),
            title = root.getString("title"),
            description = root.optString("description"),
            category = root.optString("category"),
            accentColor = root.optString("accentColor", "#6C8CFF"),
            dueAtUtc = java.time.Instant.parse(root.getString("dueAtUtc")).toEpochMilli(),
            timeZoneId = root.optString("timeZoneId", java.util.TimeZone.getDefault().id),
            priority = enumValueOrDefault(root.optString("priority"), TaskPriority.NORMAL),
            state = enumValueOrDefault(root.optString("state"), TaskState.ACTIVE),
            manualProgressPercent = root.optInt("manualProgressPercent"),
            createdAtUtc = parseInstant(root.optString("createdAtUtc"), System.currentTimeMillis()),
            updatedAtUtc = parseInstant(root.optString("updatedAtUtc"), System.currentTimeMillis()),
            completedAtUtc = root.optString("completedAtUtc").takeIf { it.isNotBlank() && it != "null" }?.let { java.time.Instant.parse(it).toEpochMilli() },
            subtasks = (0 until subtasksJson.length()).map { index -> subtasksJson.getJSONObject(index).let {
                DeadlineSubtask(it.getString("id"), it.getString("title"), it.optBoolean("isCompleted"), it.optInt("position", index))
            } },
            reminderOffsetsMinutes = (0 until remindersJson.length()).map { remindersJson.getInt(it) },
        )
    }

    private inline fun <reified T : Enum<T>> enumValueOrDefault(value: String, default: T): T =
        enumValues<T>().firstOrNull { it.name.equals(value, ignoreCase = true) } ?: default

    private fun parseInstant(value: String, fallback: Long): Long = runCatching { java.time.Instant.parse(value).toEpochMilli() }.getOrDefault(fallback)
}

