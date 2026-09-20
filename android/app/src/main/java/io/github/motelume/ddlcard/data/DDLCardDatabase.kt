package io.github.motelume.ddlcard.data

import android.content.ContentValues
import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import io.github.motelume.ddlcard.model.DeadlineTask

class DDLCardDatabase(context: Context) : SQLiteOpenHelper(context, "ddlcard.db", null, 1) {
    override fun onCreate(db: SQLiteDatabase) {
        db.execSQL("""
            CREATE TABLE tasks(
                id TEXT PRIMARY KEY,
                due_at_utc INTEGER NOT NULL,
                priority INTEGER NOT NULL,
                state INTEGER NOT NULL,
                created_at_utc INTEGER NOT NULL,
                payload_json TEXT NOT NULL
            )
        """.trimIndent())
        db.execSQL("CREATE INDEX ix_tasks_due ON tasks(state, due_at_utc, priority DESC)")
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) = Unit

    fun getAll(): List<DeadlineTask> = readableDatabase.query(
        "tasks", arrayOf("payload_json"), null, null, null, null, "due_at_utc, priority DESC, created_at_utc"
    ).use { cursor -> buildList { while (cursor.moveToNext()) add(TaskJsonCodec.decode(cursor.getString(0))) } }

    fun upsert(task: DeadlineTask) {
        val values = ContentValues().apply {
            put("id", task.id); put("due_at_utc", task.dueAtUtc); put("priority", task.priority.ordinal)
            put("state", task.state.ordinal); put("created_at_utc", task.createdAtUtc); put("payload_json", TaskJsonCodec.encode(task))
        }
        writableDatabase.insertWithOnConflict("tasks", null, values, SQLiteDatabase.CONFLICT_REPLACE)
    }

    fun delete(id: String) { writableDatabase.delete("tasks", "id=?", arrayOf(id)) }

    fun replaceAll(tasks: List<DeadlineTask>) {
        writableDatabase.beginTransaction()
        try {
            writableDatabase.delete("tasks", null, null)
            tasks.forEach(::upsert)
            writableDatabase.setTransactionSuccessful()
        } finally {
            writableDatabase.endTransaction()
        }
    }
}
