package io.github.motelume.ddlcard

import android.Manifest
import android.app.AlarmManager
import android.content.ClipData
import android.content.ClipboardManager
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import io.github.motelume.ddlcard.core.CountdownFormatter
import io.github.motelume.ddlcard.data.BackupCodec
import io.github.motelume.ddlcard.data.BackupPreview
import io.github.motelume.ddlcard.data.PresetCodec
import io.github.motelume.ddlcard.model.*
import io.github.motelume.ddlcard.reminders.ReminderScheduler
import io.github.motelume.ddlcard.widget.updateAllDDLWidgets
import kotlinx.coroutines.launch
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.*
import java.time.format.DateTimeFormatter
import java.time.format.DateTimeParseException

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        AppContainer.initialize(this)
        setContent { DDLCardApp(this) }
    }

    fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT >= 33) requestPermissions(arrayOf(Manifest.permission.POST_NOTIFICATIONS), 100)
    }

    fun openExactAlarmSettings() {
        startActivity(Intent(Settings.ACTION_REQUEST_SCHEDULE_EXACT_ALARM, Uri.parse("package:$packageName")))
    }
}

@Composable
private fun DDLCardApp(activity: MainActivity) {
    val repository = AppContainer.tasks
    val preferences = AppContainer.preferences
    val tasks by repository.tasks.collectAsState()
    val scope = rememberCoroutineScope()
    var editing by remember { mutableStateOf<DeadlineTask?>(null) }
    var creating by remember { mutableStateOf(false) }
    var showTools by remember { mutableStateOf(false) }
    var pendingRestore by remember { mutableStateOf<BackupPreview?>(null) }
    val exportLauncher = rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/json")) { uri ->
        if (uri != null) scope.launch {
            runCatching {
                val backup = BackupCodec.encode(repository.getAll(), preferences.defaultReminderOffsetsMinutes)
                withContext(Dispatchers.IO) { activity.contentResolver.openOutputStream(uri)?.bufferedWriter()?.use { it.write(backup) } ?: error("无法写入文件") }
            }.onSuccess { Toast.makeText(activity, "备份已导出", Toast.LENGTH_SHORT).show() }
                .onFailure { Toast.makeText(activity, "导出失败：${it.message}", Toast.LENGTH_LONG).show() }
        }
    }
    val importLauncher = rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) scope.launch {
            runCatching {
                val json = withContext(Dispatchers.IO) { activity.contentResolver.openInputStream(uri)?.bufferedReader()?.use { it.readText() } ?: error("无法读取文件") }
                BackupCodec.decode(json)
            }.onSuccess { pendingRestore = it }
                .onFailure { Toast.makeText(activity, "备份无效：${it.message}", Toast.LENGTH_LONG).show() }
        }
    }
    val notificationGranted = Build.VERSION.SDK_INT < 33 || activity.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED
    val exactAlarmGranted = (activity.getSystemService(AlarmManager::class.java)).canScheduleExactAlarms()

    MaterialTheme(colorScheme = darkColorScheme(
        primary = Color(0xFF7C8CFF), background = Color(0xFF0B1020), surface = Color(0xFF151C31), error = Color(0xFFFF6B7A)
    )) {
        Scaffold(
            topBar = {
                Surface(color = MaterialTheme.colorScheme.background) {
                    Row(Modifier.fillMaxWidth().padding(20.dp), verticalAlignment = Alignment.CenterVertically) {
                        Column(Modifier.weight(1f)) {
                            Text("DDLCard", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
                            Text("截止事项一眼可见", color = Color(0xFF96A1BE))
                        }
                        Column(horizontalAlignment = Alignment.End) {
                            Button(onClick = { creating = true }) { Text("添加 DDL") }
                            TextButton(onClick = { showTools = true }) { Text("预设与备份") }
                        }
                    }
                }
            }
        ) { padding ->
            LazyColumn(Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp), verticalArrangement = Arrangement.spacedBy(10.dp), contentPadding = PaddingValues(bottom = 28.dp)) {
                if (!notificationGranted || !exactAlarmGranted) {
                    item {
                        PermissionCard(
                            notificationGranted = notificationGranted,
                            exactAlarmGranted = exactAlarmGranted,
                            onNotifications = activity::requestNotificationPermission,
                            onExactAlarm = activity::openExactAlarmSettings,
                        )
                    }
                }
                val active = tasks.filter { it.state == TaskState.ACTIVE }.sortedWith(compareBy<DeadlineTask> { it.dueAtUtc }.thenByDescending { it.priority })
                if (active.isEmpty()) item { EmptyCard() }
                items(active, key = { it.id }) { task ->
                    TaskCard(
                        task = task,
                        onEdit = { editing = task },
                        onComplete = { scope.launch { repository.complete(task.id); updateAllDDLWidgets(activity) } },
                        onPostpone = { scope.launch {
                            repository.postpone(task.id, 60 * 60 * 1000L)?.let { ReminderScheduler.scheduleTask(activity, it) }
                            updateAllDDLWidgets(activity)
                        } },
                    )
                }
            }
        }

        if (creating || editing != null) {
            TaskEditorDialog(
                original = editing,
                defaultReminders = preferences.defaultReminderOffsetsMinutes,
                onDismiss = { creating = false; editing = null },
                onSave = { task ->
                    scope.launch {
                        repository.save(task)
                        ReminderScheduler.scheduleTask(activity, task)
                        updateAllDDLWidgets(activity)
                    }
                    creating = false; editing = null
                },
            )
        }

        if (showTools) {
            ToolsDialog(
                activity = activity,
                reminders = preferences.defaultReminderOffsetsMinutes,
                onDismiss = { showTools = false },
                onApplyPreset = {
                    preferences.defaultReminderOffsetsMinutes = it
                    Toast.makeText(activity, "预设已应用；新任务将使用新的默认提醒", Toast.LENGTH_SHORT).show()
                },
                onExport = { showTools = false; exportLauncher.launch("DDLCard-backup.json") },
                onImport = { showTools = false; importLauncher.launch(arrayOf("application/json", "text/plain")) },
            )
        }

        pendingRestore?.let { preview ->
            AlertDialog(
                onDismissRequest = { pendingRestore = null },
                title = { Text("确认恢复备份？") },
                text = { Text("备份包含 ${preview.tasks.size} 个任务。恢复会替换手机当前的全部任务；电脑数据不会受到影响。") },
                confirmButton = { Button(onClick = {
                    scope.launch {
                        repository.replaceAll(preview.tasks)
                        if (preview.reminderOffsets.isNotEmpty()) preferences.defaultReminderOffsetsMinutes = preview.reminderOffsets
                        ReminderScheduler.rescheduleAll(activity)
                        updateAllDDLWidgets(activity)
                        Toast.makeText(activity, "备份恢复完成", Toast.LENGTH_SHORT).show()
                    }
                    pendingRestore = null
                }) { Text("恢复") } },
                dismissButton = { TextButton(onClick = { pendingRestore = null }) { Text("取消") } },
            )
        }
    }
}

@Composable
private fun PermissionCard(notificationGranted: Boolean, exactAlarmGranted: Boolean, onNotifications: () -> Unit, onExactAlarm: () -> Unit) {
    Card(colors = CardDefaults.cardColors(containerColor = Color(0xFF242E4A))) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text("让提醒可靠送达", fontWeight = FontWeight.Bold)
            Text("ColorOS 可能限制后台活动。请允许通知和精确闹钟；之后可在系统电池设置中允许 DDLCard 后台运行。", color = Color(0xFFB7C0D8))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                if (!notificationGranted) OutlinedButton(onClick = onNotifications) { Text("允许通知") }
                if (!exactAlarmGranted) OutlinedButton(onClick = onExactAlarm) { Text("允许准时提醒") }
            }
        }
    }
}

@Composable
private fun EmptyCard() {
    Card(Modifier.fillMaxWidth(), colors = CardDefaults.cardColors(containerColor = Color(0xFF151C31))) {
        Column(Modifier.fillMaxWidth().padding(36.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Text("✓", style = MaterialTheme.typography.displaySmall, color = Color(0xFF63BEA0))
            Text("没有进行中的 DDL", fontWeight = FontWeight.SemiBold)
        }
    }
}

@Composable
private fun TaskCard(task: DeadlineTask, onEdit: () -> Unit, onComplete: () -> Unit, onPostpone: () -> Unit) {
    val priorityColor = when (task.priority) {
        TaskPriority.LOW -> Color(0xFF63BEA0); TaskPriority.NORMAL -> Color(0xFF6C8CFF)
        TaskPriority.HIGH -> Color(0xFFFFB257); TaskPriority.URGENT -> Color(0xFFFF6B7A)
    }
    Card(onClick = onEdit, colors = CardDefaults.cardColors(containerColor = Color(0xFF151C31)), shape = RoundedCornerShape(18.dp)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(task.title, Modifier.weight(1f), style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                if (task.category.isNotBlank()) Text(task.category, color = Color(0xFF96A1BE), style = MaterialTheme.typography.labelMedium)
            }
            Text(CountdownFormatter.format(task.dueAtUtc), color = priorityColor, fontWeight = FontWeight.SemiBold)
            LinearProgressIndicator(progress = { task.progressPercent / 100f }, Modifier.fillMaxWidth(), color = priorityColor)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = onComplete) { Text("完成") }
                OutlinedButton(onClick = onPostpone) { Text("延后 1 小时") }
            }
        }
    }
}

@Composable
private fun TaskEditorDialog(original: DeadlineTask?, defaultReminders: List<Int>, onDismiss: () -> Unit, onSave: (DeadlineTask) -> Unit) {
    val formatter = remember { DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm") }
    var title by remember(original) { mutableStateOf(original?.title.orEmpty()) }
    var dueText by remember(original) { mutableStateOf(original?.let { Instant.ofEpochMilli(it.dueAtUtc).atZone(ZoneId.systemDefault()).format(formatter) } ?: LocalDateTime.now().plusDays(1).withHour(23).withMinute(59).format(formatter)) }
    var category by remember(original) { mutableStateOf(original?.category.orEmpty()) }
    var description by remember(original) { mutableStateOf(original?.description.orEmpty()) }
    var subtasks by remember(original) { mutableStateOf(original?.subtasks?.joinToString("\n") { (if (it.isCompleted) "[x] " else "") + it.title }.orEmpty()) }
    var reminders by remember(original) { mutableStateOf((original?.reminderOffsetsMinutes ?: defaultReminders).joinToString(",") { "${it}m" }) }
    var priority by remember(original) { mutableStateOf(original?.priority ?: TaskPriority.NORMAL) }
    var priorityMenu by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (original == null) "添加截止事项" else "编辑截止事项") },
        text = {
            Column(Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(9.dp)) {
                OutlinedTextField(title, { title = it }, label = { Text("标题") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(dueText, { dueText = it }, label = { Text("截止时间 yyyy-MM-dd HH:mm") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(category, { category = it }, label = { Text("分类") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(description, { description = it }, label = { Text("备注") }, minLines = 2, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(subtasks, { subtasks = it }, label = { Text("子任务（每行一个）") }, minLines = 2, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(reminders, { reminders = it }, label = { Text("提醒：7d,3d,1h,10m") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                Box {
                    OutlinedButton(onClick = { priorityMenu = true }) { Text("优先级：${priority.name}") }
                    DropdownMenu(priorityMenu, onDismissRequest = { priorityMenu = false }) {
                        TaskPriority.entries.forEach { value -> DropdownMenuItem(text = { Text(value.name) }, onClick = { priority = value; priorityMenu = false }) }
                    }
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            }
        },
        confirmButton = {
            Button(onClick = {
                try {
                    require(title.isNotBlank()) { "标题不能为空" }
                    val due = LocalDateTime.parse(dueText.trim(), formatter).atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
                    onSave((original ?: DeadlineTask(title = title, dueAtUtc = due)).copy(
                        title = title.trim(), dueAtUtc = due, category = category.trim(), description = description.trim(), priority = priority,
                        subtasks = subtasks.lines().filter { it.isNotBlank() }.mapIndexed { index, line -> DeadlineSubtask(title = line.removePrefix("[x] ").trim(), isCompleted = line.startsWith("[x] ", true), position = index) },
                        reminderOffsetsMinutes = parseReminderOffsets(reminders),
                    ))
                } catch (ex: DateTimeParseException) { error = "截止时间格式应为 2026-09-21 23:59" }
                catch (ex: IllegalArgumentException) { error = ex.message }
            }) { Text("保存") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } },
    )
}

@Composable
private fun ToolsDialog(
    activity: MainActivity,
    reminders: List<Int>,
    onDismiss: () -> Unit,
    onApplyPreset: (List<Int>) -> Unit,
    onExport: () -> Unit,
    onImport: () -> Unit,
) {
    var code by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    var preview by remember { mutableStateOf<List<Int>?>(null) }
    val clipboard = activity.getSystemService(ClipboardManager::class.java)
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("预设与备份") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(9.dp)) {
                Text("预设码只复制显示偏好与默认提醒，不包含任务。Android 会应用其中的默认提醒。", color = Color(0xFFB7C0D8))
                OutlinedTextField(code, { code = it; error = null; preview = null }, label = { Text("DDLCard 预设码") }, minLines = 3, modifier = Modifier.fillMaxWidth())
                Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                    OutlinedButton(onClick = {
                        code = PresetCodec.encode(reminders)
                        clipboard.setPrimaryClip(ClipData.newPlainText("DDLCard preset", code))
                        Toast.makeText(activity, "当前预设码已复制", Toast.LENGTH_SHORT).show()
                    }) { Text("生成并复制") }
                    OutlinedButton(onClick = {
                        code = clipboard.primaryClip?.getItemAt(0)?.coerceToText(activity)?.toString().orEmpty()
                    }) { Text("粘贴") }
                }
                OutlinedButton(onClick = {
                    runCatching { PresetCodec.decode(code) }
                        .onSuccess { preview = it; error = null }
                        .onFailure { error = it.message ?: "预设码无效" }
                }) { Text("预览预设") }
                preview?.let { values ->
                    Text("默认提醒将改为：${values.joinToString("、") { formatReminderOffset(it) }}")
                    Button(onClick = { onApplyPreset(values); onDismiss() }) { Text("确认应用") }
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                HorizontalDivider()
                Text("JSON 备份用于手动迁移数据，不会自动同步。")
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedButton(onClick = onExport) { Text("导出备份") }
                    OutlinedButton(onClick = onImport) { Text("恢复备份") }
                }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("关闭") } },
    )
}

private fun parseReminderOffsets(value: String): List<Int> = value.split(',').mapNotNull { token ->
    val text = token.trim().lowercase(); val number = text.dropLast(1).toDoubleOrNull() ?: return@mapNotNull null
    when (text.lastOrNull()) { 'd' -> (number * 1440).toInt(); 'h' -> (number * 60).toInt(); 'm' -> number.toInt(); else -> null }
}.filter { it >= 0 }.distinct().sortedDescending()

private fun formatReminderOffset(minutes: Int): String = when {
    minutes % 1440 == 0 -> "${minutes / 1440}天"
    minutes % 60 == 0 -> "${minutes / 60}小时"
    else -> "${minutes}分钟"
}
