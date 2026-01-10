using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 在屏幕上实时显示最近的日志信息（用于调试按钮点击等事件）
/// </summary>
public class OnScreenDebugLog : MonoBehaviour
{
    private static OnScreenDebugLog Instance;
    private static List<LogEntry> logLines = new List<LogEntry>();
    private static int maxLines = 50;

    private struct LogEntry
    {
        public string message;
        public LogType type;
        public Color color;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 过滤掉一些噪音
        if (logString.Contains("CEventSystem") || 
            logString.Contains("Waiting for debugger") ||
            logString.Contains("URP") ||
            logString.Contains("RenderPipeline"))
            return;

        // 对关键日志加色
        Color color = Color.white;
        if (type == LogType.Error)
            color = Color.red;
        else if (type == LogType.Warning)
            color = new Color(1f, 0.65f, 0f); // Orange
        else if (logString.Contains("✅"))
            color = Color.green;
        else if (logString.Contains("❌"))
            color = Color.red;
        else if (logString.Contains("👆"))
            color = new Color(1f, 1f, 0f); // Yellow
        else if (logString.Contains("🎬") || logString.Contains("📍") || logString.Contains("🔄"))
            color = new Color(0f, 1f, 1f); // Cyan

        LogEntry entry = new LogEntry { message = logString, type = type, color = color };
        logLines.Insert(0, entry);
        
        if (logLines.Count > maxLines)
        {
            logLines.RemoveAt(logLines.Count - 1);
        }
    }

    private void OnGUI()
    {
        // 绘制背景框
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.Box(new Rect(10, 10, 600, 750), "");
        
        // 绘制标题
        GUI.color = Color.yellow;
        GUI.Label(new Rect(20, 20, 580, 30), "=== 🔍 实时日志调试 (最近 " + logLines.Count + " 条) ===");
        
        // 绘制日志行
        int yPos = 60;
        for (int i = 0; i < logLines.Count && yPos < 750; i++)
        {
            GUI.color = logLines[i].color;
            string displayText = logLines[i].message;
            
            // 截断过长的文本
            if (displayText.Length > 80)
                displayText = displayText.Substring(0, 77) + "...";
            
            GUI.Label(new Rect(20, yPos, 580, 20), displayText);
            yPos += 18;
        }

        // 底部说明
        GUI.color = Color.gray;
        GUI.Label(new Rect(20, 730, 580, 20), "📌 提示: 如果日志没出现，检查 Console 是否有报错");
    }
}
