package io.github.motelume.ddlcard

import io.github.motelume.ddlcard.data.BackupCodec
import io.github.motelume.ddlcard.data.PresetCodec
import io.github.motelume.ddlcard.model.DeadlineTask
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class TransferCodecTest {
    @Test fun presetRoundTripPreservesReminderDefaults() {
        val expected = listOf(10080, 1440, 60, 10)
        val code = PresetCodec.encode(expected)

        assertTrue(code.startsWith("DDLCARD-PRESET-1:"))
        assertEquals(expected, PresetCodec.decode(code))
    }

    @Test fun backupRoundTripPreservesTasks() {
        val task = DeadlineTask(title = "提交课程论文", dueAtUtc = 1_800_000_000_000)
        val json = BackupCodec.encode(listOf(task), listOf(60, 10))
        val restored = BackupCodec.decode(json)

        assertEquals(1, restored.tasks.size)
        assertEquals(task.title, restored.tasks.single().title)
        assertEquals(task.dueAtUtc, restored.tasks.single().dueAtUtc)
        assertEquals(listOf(60, 10), restored.reminderOffsets)
    }
}
