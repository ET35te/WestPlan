using UnityEngine;
using System;

/// <summary>
/// 条件判定系统
/// 支持格式：
/// - BELIEF>50 (信念大于50)
/// - GRAIN<20 (粮食小于20)
/// - ARMOR==10 (护甲等于10)
/// - BELIEF>30&GRAIN<50 (且条件)
/// - BELIEF>30|GRAIN<50 (或条件)
/// </summary>
public class ConditionEvaluator
{
    public static bool Evaluate(string condition, ResourceManager resourceMgr)
    {
        if (string.IsNullOrEmpty(condition))
            return true;

        if (resourceMgr == null)
        {
            Debug.LogWarning("⚠️ ResourceManager 为空，无法评估条件");
            return false;
        }

        condition = condition.Trim();

        // 处理 OR 条件（|）- 如果任一满足则通过
        if (condition.Contains("|"))
        {
            string[] orParts = condition.Split('|');
            foreach (string part in orParts)
            {
                if (Evaluate(part.Trim(), resourceMgr))
                    return true;
            }
            return false;
        }

        // 处理 AND 条件（&）- 全部满足才通过
        if (condition.Contains("&"))
        {
            string[] andParts = condition.Split('&');
            foreach (string part in andParts)
            {
                if (!Evaluate(part.Trim(), resourceMgr))
                    return false;
            }
            return true;
        }

        // 单一条件解析
        return EvaluateSingleCondition(condition, resourceMgr);
    }

    private static bool EvaluateSingleCondition(string condition, ResourceManager resourceMgr)
    {
        // 支持的操作符：>, <, ==, >=, <=, !=
        
        if (condition.Contains(">="))
        {
            var parts = condition.Split(new string[] { ">=" }, StringSplitOptions.None);
            return CompareResource(parts[0].Trim(), resourceMgr) >= int.Parse(parts[1].Trim());
        }
        if (condition.Contains("<="))
        {
            var parts = condition.Split(new string[] { "<=" }, StringSplitOptions.None);
            return CompareResource(parts[0].Trim(), resourceMgr) <= int.Parse(parts[1].Trim());
        }
        if (condition.Contains("!="))
        {
            var parts = condition.Split(new string[] { "!=" }, StringSplitOptions.None);
            return CompareResource(parts[0].Trim(), resourceMgr) != int.Parse(parts[1].Trim());
        }
        if (condition.Contains(">"))
        {
            var parts = condition.Split('>');
            return CompareResource(parts[0].Trim(), resourceMgr) > int.Parse(parts[1].Trim());
        }
        if (condition.Contains("<"))
        {
            var parts = condition.Split('<');
            return CompareResource(parts[0].Trim(), resourceMgr) < int.Parse(parts[1].Trim());
        }
        if (condition.Contains("=="))
        {
            var parts = condition.Split(new string[] { "==" }, StringSplitOptions.None);
            return CompareResource(parts[0].Trim(), resourceMgr) == int.Parse(parts[1].Trim());
        }

        Debug.LogWarning($"⚠️ 无法解析条件: {condition}");
        return false;
    }

    private static int CompareResource(string resourceName, ResourceManager resourceMgr)
    {
        resourceName = resourceName.ToUpper().Trim();

        switch (resourceName)
        {
            case "BELIEF":
                return resourceMgr.Belief;
            case "GRAIN":
                return resourceMgr.Grain;
            case "ARMOR":
                return resourceMgr.Armor;
            default:
                Debug.LogWarning($"⚠️ 未知的资源类型: {resourceName}");
                return 0;
        }
    }

    /// <summary>
    /// 调试用：打印条件评估过程
    /// </summary>
    public static void DebugEvaluate(string condition, ResourceManager resourceMgr)
    {
        bool result = Evaluate(condition, resourceMgr);
        Debug.Log($"🔍 条件评估: [{condition}] => {(result ? "✅ 通过" : "❌ 失败")}");
    }
}
