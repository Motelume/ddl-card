package io.github.motelume.ddlcard.widget

import android.content.Context
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.glance.*
import androidx.glance.action.ActionParameters
import androidx.glance.action.actionParametersOf
import androidx.glance.action.clickable
import androidx.glance.action.actionStartActivity
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.GlanceAppWidgetReceiver
import androidx.glance.appwidget.action.ActionCallback
import androidx.glance.appwidget.action.actionRunCallback
import androidx.glance.appwidget.provideContent
import androidx.glance.appwidget.updateAll
import androidx.glance.background
import androidx.glance.layout.*
import androidx.glance.text.FontWeight
import androidx.glance.text.Text
import androidx.glance.text.TextStyle
import androidx.glance.unit.ColorProvider
import io.github.motelume.ddlcard.AppContainer
import io.github.motelume.ddlcard.MainActivity
import io.github.motelume.ddlcard.core.CountdownFormatter
import io.github.motelume.ddlcard.model.DeadlineTask

private val taskIdKey = ActionParameters.Key<String>("taskId")

open class DDLCardWidget(private val maxTasks: Int) : GlanceAppWidget() {
    override suspend fun provideGlance(context: Context, id: GlanceId) {
        AppContainer.initialize(context)
        val tasks = AppContainer.tasks.getActive().take(maxTasks)
        provideContent { WidgetContent(tasks) }
    }
}

class DDLCard4x4Widget : DDLCardWidget(3)
class DDLCard4x6Widget : DDLCardWidget(6)

class DDLCard4x4Receiver : GlanceAppWidgetReceiver() { override val glanceAppWidget: GlanceAppWidget = DDLCard4x4Widget() }
class DDLCard4x6Receiver : GlanceAppWidgetReceiver() { override val glanceAppWidget: GlanceAppWidget = DDLCard4x6Widget() }

@Composable
private fun WidgetContent(tasks: List<DeadlineTask>) {
    Column(
        modifier = GlanceModifier.fillMaxSize().background(ColorProvider(Color(0xFF151C31))).padding(16.dp)
            .clickable(actionStartActivity<MainActivity>()),
        verticalAlignment = Alignment.Top,
    ) {
        Text("DDLCard", style = TextStyle(color = ColorProvider(Color.White), fontSize = 20.sp, fontWeight = FontWeight.Bold))
        Spacer(GlanceModifier.height(8.dp))
        if (tasks.isEmpty()) {
            Text("没有进行中的 DDL", style = TextStyle(color = ColorProvider(Color(0xFF96A1BE)), fontSize = 14.sp))
        } else {
            tasks.forEach { task ->
                Row(GlanceModifier.fillMaxWidth().padding(vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
                    Column(GlanceModifier.defaultWeight()) {
                        Text(task.title, maxLines = 1, style = TextStyle(color = ColorProvider(Color.White), fontSize = 14.sp, fontWeight = FontWeight.Medium))
                        Text(CountdownFormatter.format(task.dueAtUtc), maxLines = 1, style = TextStyle(color = ColorProvider(priorityColor(task)), fontSize = 12.sp, fontWeight = FontWeight.Bold))
                    }
                    Button("完成", onClick = actionRunCallback<CompleteTaskAction>(actionParametersOf(taskIdKey to task.id)))
                }
            }
        }
    }
}

private fun priorityColor(task: DeadlineTask): Color = when (task.priority) {
    io.github.motelume.ddlcard.model.TaskPriority.LOW -> Color(0xFF63BEA0)
    io.github.motelume.ddlcard.model.TaskPriority.NORMAL -> Color(0xFF6C8CFF)
    io.github.motelume.ddlcard.model.TaskPriority.HIGH -> Color(0xFFFFB257)
    io.github.motelume.ddlcard.model.TaskPriority.URGENT -> Color(0xFFFF6B7A)
}

class CompleteTaskAction : ActionCallback {
    override suspend fun onAction(context: Context, glanceId: GlanceId, parameters: ActionParameters) {
        val taskId = parameters[taskIdKey] ?: return
        AppContainer.initialize(context)
        AppContainer.tasks.complete(taskId)
        updateAllDDLWidgets(context)
    }
}

suspend fun updateAllDDLWidgets(context: Context) {
    DDLCard4x4Widget().updateAll(context)
    DDLCard4x6Widget().updateAll(context)
}
