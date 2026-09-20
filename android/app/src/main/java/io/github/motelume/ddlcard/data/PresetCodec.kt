package io.github.motelume.ddlcard.data

import org.json.JSONArray
import org.json.JSONObject
import java.util.Base64

object PresetCodec {
    private const val PREFIX = "DDLCARD-PRESET-1:"

    fun encode(reminders: List<Int>): String {
        val json = JSONObject().apply {
            put("schemaVersion", 1)
            put("themeId", "midnight")
            put("accentColor", "#7C8CFF")
            put("cardOpacity", 0.94)
            put("cornerRadius", 22.0)
            put("fontScale", 1.0)
            put("alwaysOnTop", false)
            put("edgeCollapseEnabled", false)
            put("defaultReminderOffsetsMinutes", JSONArray(reminders))
        }.toString().toByteArray(Charsets.UTF_8)
        return PREFIX + Base64.getUrlEncoder().withoutPadding().encodeToString(json)
    }

    fun decode(code: String): List<Int> {
        require(code.trim().startsWith(PREFIX)) { "不是有效的 DDLCard 预设码" }
        val encoded = code.trim().removePrefix(PREFIX)
        val root = runCatching {
            JSONObject(String(Base64.getUrlDecoder().decode(encoded), Charsets.UTF_8))
        }.getOrElse { throw IllegalArgumentException("预设码损坏或版本不受支持") }
        require(root.optInt("schemaVersion") == 1) { "不支持此预设码版本" }
        val array = root.optJSONArray("defaultReminderOffsetsMinutes") ?: JSONArray()
        return (0 until array.length()).map { array.getInt(it) }.filter { it >= 0 }.distinct().sortedDescending()
            .ifEmpty { listOf(10080, 4320, 1440, 60, 10) }
    }
}
