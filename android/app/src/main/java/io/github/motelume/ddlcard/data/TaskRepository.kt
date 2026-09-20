package io.github.motelume.ddlcard.data

import android.content.Context
import io.github.motelume.ddlcard.model.DeadlineTask
import io.github.motelume.ddlcard.model.TaskState
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.withContext

class TaskRepository(context: Context) {
    private val database = DDLCardDatabase(context.applicationContext)
    private val _tasks = MutableStateFlow(database.getAll())
    val tasks: StateFlow<List<DeadlineTask>> = _tasks.asStateFlow()

    suspend fun getAll(): List<DeadlineTask> = withContext(Dispatchers.IO) { database.getAll() }
    suspend fun getActive(): List<DeadlineTask> = getAll().filter { it.state == TaskState.ACTIVE }.sortedWith(compareBy<DeadlineTask> { it.dueAtUtc }.thenByDescending { it.priority })
    suspend fun getById(id: String): DeadlineTask? = withContext(Dispatchers.IO) { database.getAll().firstOrNull { it.id == id } }

    suspend fun save(task: DeadlineTask) = withContext(Dispatchers.IO) {
        require(task.title.isNotBlank()) { "任务标题不能为空" }
        database.upsert(task.copy(title = task.title.trim(), updatedAtUtc = System.currentTimeMillis()))
        _tasks.value = database.getAll()
    }

    suspend fun complete(id: String) = update(id) { task ->
        task.copy(state = TaskState.COMPLETED, completedAtUtc = System.currentTimeMillis(), manualProgressPercent = 100,
            subtasks = task.subtasks.map { it.copy(isCompleted = true) })
    }

    suspend fun postpone(id: String, millis: Long): DeadlineTask? = update(id) { it.copy(dueAtUtc = it.dueAtUtc + millis) }

    suspend fun delete(id: String) = withContext(Dispatchers.IO) {
        database.delete(id); _tasks.value = database.getAll()
    }

    suspend fun replaceAll(tasks: List<DeadlineTask>) = withContext(Dispatchers.IO) {
        require(tasks.all { it.title.isNotBlank() }) { "备份中存在空标题任务" }
        database.replaceAll(tasks)
        _tasks.value = database.getAll()
    }

    private suspend fun update(id: String, transform: (DeadlineTask) -> DeadlineTask): DeadlineTask? = withContext(Dispatchers.IO) {
        val current = database.getAll().firstOrNull { it.id == id } ?: return@withContext null
        val updated = transform(current).copy(updatedAtUtc = System.currentTimeMillis())
        database.upsert(updated)
        _tasks.value = database.getAll()
        updated
    }
}
