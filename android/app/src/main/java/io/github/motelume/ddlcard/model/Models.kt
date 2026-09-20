package io.github.motelume.ddlcard.model

import java.util.UUID

enum class TaskPriority { LOW, NORMAL, HIGH, URGENT }
enum class TaskState { ACTIVE, COMPLETED, ARCHIVED }

data class DeadlineSubtask(
    val id: String = UUID.randomUUID().toString(),
    val title: String,
    val isCompleted: Boolean = false,
    val position: Int = 0,
)

data class DeadlineTask(
    val id: String = UUID.randomUUID().toString(),
    val title: String,
    val description: String = "",
    val category: String = "",
    val accentColor: String = "#6C8CFF",
    val dueAtUtc: Long,
    val timeZoneId: String = java.util.TimeZone.getDefault().id,
    val priority: TaskPriority = TaskPriority.NORMAL,
    val state: TaskState = TaskState.ACTIVE,
    val manualProgressPercent: Int = 0,
    val createdAtUtc: Long = System.currentTimeMillis(),
    val updatedAtUtc: Long = System.currentTimeMillis(),
    val completedAtUtc: Long? = null,
    val subtasks: List<DeadlineSubtask> = emptyList(),
    val reminderOffsetsMinutes: List<Int> = listOf(10080, 4320, 1440, 60, 10),
) {
    val progressPercent: Int
        get() = if (subtasks.isEmpty()) manualProgressPercent.coerceIn(0, 100)
        else (subtasks.count { it.isCompleted } * 100.0 / subtasks.size).toInt()
}

