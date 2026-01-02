using UnityEngine;
using TMPro;

/// <summary>
/// 🔧 飘字诊断工具
/// 快速排查飘字不显示的原因
/// </summary>
public class PopupDiagnostic : MonoBehaviour
{
    [Header("诊断工具")]
    [SerializeField] private bool runDiagnosticOnStart = false;

    private void Start()
    {
        if (runDiagnosticOnStart)
        {
            RunFullDiagnostic();
        }
    }

    private void Update()
    {
        // 🔥 快速测试快捷键 - 使用 Alt+数字 避免与 Unity 冲突
        if (Input.GetKeyDown(KeyCode.Alpha1) && Input.GetKey(KeyCode.LeftAlt))
        {
            RunFullDiagnostic();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && Input.GetKey(KeyCode.LeftAlt))
        {
            TestDamagePopup();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftAlt))
        {
            TestForcePopup();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) && Input.GetKey(KeyCode.LeftAlt))
        {
            PrintSceneInfo();
        }

        if (Input.GetKeyDown(KeyCode.Alpha7) && Input.GetKey(KeyCode.LeftAlt))
        {
            ComparisonTest();
        }

        if (Input.GetKeyDown(KeyCode.Alpha6) && Input.GetKey(KeyCode.LeftAlt))
        {
            SimpleTest();
        }
    }

    /// <summary>
    /// 完整诊断 - Alt+1 触发
    /// </summary>
    public void RunFullDiagnostic()
    {
        Debug.Log("========== 🔧 飘字系统完整诊断 ==========");

        // 1️⃣ Canvas 检查
        Canvas canvas = FindObjectOfType<Canvas>();
        Debug.Log($"1️⃣ Canvas 检查: {(canvas != null ? "✅ 找到" : "❌ 未找到")}");
        if (canvas != null)
        {
            Debug.Log($"   - Canvas 激活: {(canvas.gameObject.activeInHierarchy ? "✅ 是" : "❌ 否")}");
            Debug.Log($"   - Canvas Render Mode: {canvas.renderMode}");
            Debug.Log($"   - Canvas Sorting Order: {canvas.sortingOrder}");
        }

        // 2️⃣ Camera 检查
        Camera mainCam = Camera.main;
        Debug.Log($"2️⃣ Camera 检查: {(mainCam != null ? "✅ 找到" : "❌ 未找到")}");
        if (mainCam != null)
        {
            Debug.Log($"   - Camera 激活: {(mainCam.gameObject.activeInHierarchy ? "✅ 是" : "❌ 否")}");
            Debug.Log($"   - Camera 位置: {mainCam.transform.position}");
        }

        // 3️⃣ Prefab 检查
        GameObject prefab = Resources.Load<GameObject>("UI/DamagePopup");
        Debug.Log($"3️⃣ DamagePopup Prefab 检查:");
        Debug.Log($"   - Resources.Load 结果: {(prefab != null ? "✅ 找到" : "❌ 未找到")}");

        if (prefab != null)
        {
            DamagePopup popupScript = prefab.GetComponent<DamagePopup>();
            Debug.Log($"   - Prefab 包含 DamagePopup 脚本: {(popupScript != null ? "✅ 是" : "❌ 否")}");

            TextMeshProUGUI tmpText = prefab.GetComponent<TextMeshProUGUI>();
            Debug.Log($"   - Prefab 包含 TextMeshProUGUI: {(tmpText != null ? "✅ 是" : "❌ 否")}");
        }

        // 4️⃣ 静态引用检查
        if (DamagePopup.prefabReference != null)
        {
            Debug.Log($"4️⃣ DamagePopup 静态引用: ✅ 已设置");
        }
        else
        {
            Debug.Log($"4️⃣ DamagePopup 静态引用: ⚠️  未设置 (但 Resources.Load 可以代替)");
        }

        // 5️⃣ 坐标系检查
        if (mainCam != null && canvas != null)
        {
            Vector3 testWorldPos = mainCam.transform.position + mainCam.transform.forward * 10f;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(mainCam, testWorldPos);
            Debug.Log($"5️⃣ 坐标转换测试:");
            Debug.Log($"   - 测试世界坐标: {testWorldPos}");
            Debug.Log($"   - 转换后屏幕坐标: {screenPos}");
            Debug.Log($"   - 屏幕范围: (0-{Screen.width}, 0-{Screen.height})");
            if (screenPos.x >= 0 && screenPos.x <= Screen.width && screenPos.y >= 0 && screenPos.y <= Screen.height)
            {
                Debug.Log($"   - 坐标有效: ✅ 在屏幕范围内");
            }
            else
            {
                Debug.Log($"   - 坐标有效: ⚠️  超出屏幕范围");
            }
        }

        Debug.Log("========== 诊断结束 ==========\n");
    }

    /// <summary>
    /// 快速测试飘字 - Alt+2 触发
    /// </summary>
    public void TestDamagePopup()
    {
        Debug.Log("🔥 开始飘字测试...");

        Canvas canvas = FindObjectOfType<Canvas>();
        Camera mainCam = Camera.main;

        if (canvas == null)
        {
            Debug.LogError("❌ Canvas 不存在！");
            return;
        }

        if (mainCam == null)
        {
            Debug.LogError("❌ Camera 不存在！");
            return;
        }

        // 在屏幕中央生成飘字
        Vector3 testPos = mainCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        
        Debug.Log($"📍 生成位置: {testPos}");
        Debug.Log($"🎨 颜色: 红色");

        DamagePopup.SpawnPopup("-9999", testPos, Color.red);
        Debug.Log("✅ 飘字已触发，检查屏幕中央是否有红色 '-9999' 文字向上浮动");
    }

    /// <summary>
    /// 测试所有 4 种颜色
    /// </summary>
    public void TestAllPopupColors()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        float xOffset = 0;

        // 红色 - 伤害输出
        Vector3 pos1 = mainCam.ViewportToWorldPoint(new Vector3(0.2f + xOffset, 0.5f, 10f));
        DamagePopup.SpawnPopup("-100", pos1, Color.red);
        Debug.Log("✅ 红色飘字 (伤害输出)");

        // 橙色 - 伤害接收
        Vector3 pos2 = mainCam.ViewportToWorldPoint(new Vector3(0.35f + xOffset, 0.5f, 10f));
        DamagePopup.SpawnPopup("-50", pos2, new Color(1, 0.5f, 0));
        Debug.Log("✅ 橙色飘字 (伤害接收)");

        // 绿色 - 治疗
        Vector3 pos3 = mainCam.ViewportToWorldPoint(new Vector3(0.5f + xOffset, 0.5f, 10f));
        DamagePopup.SpawnPopup("+75", pos3, Color.green);
        Debug.Log("✅ 绿色飘字 (治疗)");

        // 青色 - 防御成功
        Vector3 pos4 = mainCam.ViewportToWorldPoint(new Vector3(0.65f + xOffset, 0.5f, 10f));
        DamagePopup.SpawnPopup("BLOCK", pos4, Color.cyan);
        Debug.Log("✅ 青色飘字 (防御成功)");

        Debug.Log("🎨 四种颜色飘字已生成，应该在屏幕中央看到 4 条不同颜色的文字");
    }

    /// <summary>
    /// 打印场景信息 - Alt+4 触发
    /// </summary>
    public void PrintSceneInfo()
    {
        Debug.Log("========== 🎬 场景信息 ==========");
        Debug.Log($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"屏幕分辨率: {Screen.width}x{Screen.height}");
        Debug.Log($"时间缩放: {Time.timeScale}");
        Debug.Log($"FPS: {1f / Time.deltaTime:F0}");

        var allCanvas = FindObjectsOfType<Canvas>();
        Debug.Log($"场景中 Canvas 数量: {allCanvas.Length}");
        foreach (var c in allCanvas)
        {
            Debug.Log($"  - {c.gameObject.name} (Active: {c.gameObject.activeInHierarchy})");
            Debug.Log($"    Render Mode: {c.renderMode}, Sorting Order: {c.sortingOrder}");
        }

        Debug.Log("========== 完成 ==========\n");
    }

    /// <summary>
    /// 强制测试 - 在 Canvas 直接生成飘字（不用坐标转换）
    /// Alt+3 触发
    /// </summary>
    public void TestForcePopup()
    {
        Debug.Log("🔥 强制测试飘字（跳过坐标转换）...");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas 不存在");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("UI/DamagePopup");
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("DamagePopup");
        }

        if (prefab == null)
        {
            Debug.LogError("❌ Prefab 不存在");
            return;
        }

        // 直接在 Canvas 中央生成
        GameObject popupObj = Instantiate(prefab, canvas.transform);
        popupObj.name = "TestPopup";

        RectTransform rt = popupObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            // 设置到屏幕中央
            rt.anchoredPosition = Vector2.zero;
            Debug.Log($"✅ 飘字已在 Canvas 中央生成！应该能看到 'TEST' 文字");
        }

        TextMeshProUGUI tmp = popupObj.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = "TEST";
            tmp.color = Color.red;
            tmp.fontSize = 36;
        }

        // 3 秒后删除
        Destroy(popupObj, 3f);
    }

    /// <summary>
    /// 对比测试：直接操作 vs SpawnPopup()
    /// 按 Alt+7 触发
    /// </summary>
    public void ComparisonTest()
    {
        Debug.Log("🔍 对比测试：直接操作 vs SpawnPopup()");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas 不存在");
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("❌ Camera 不存在");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("UI/DamagePopup");
        if (prefab == null)
            prefab = Resources.Load<GameObject>("DamagePopup");

        if (prefab == null)
        {
            Debug.LogError("❌ Prefab 不存在");
            return;
        }

        Debug.Log($"🎨 Prefab 找到: {prefab.name}");

        // ========== 左侧：直接操作 (完全绕过 Show 方法) ==========
        GameObject directObj = Instantiate(prefab, canvas.transform);
        directObj.name = "Direct_Left";
        
        RectTransform directRt = directObj.GetComponent<RectTransform>();
        if (directRt != null)
        {
            directRt.anchoredPosition = new Vector2(-300, 0);
            Debug.Log($"✅ 左侧直接操作: 位置设置完毕 {directRt.anchoredPosition}");
        }

        TextMeshProUGUI directTmp = directObj.GetComponent<TextMeshProUGUI>();
        if (directTmp != null)
        {
            directTmp.text = "LEFT";
            directTmp.color = Color.red;
            directTmp.fontSize = 50;
            Debug.Log($"✅ 左侧文字: 'LEFT', 颜色: {directTmp.color}, FontSize: {directTmp.fontSize}");
        }
        else
        {
            Debug.LogError("❌ 左侧没有 TextMeshProUGUI 组件!");
        }

        Destroy(directObj, 3f);

        // ========== 中央：SpawnPopup 方式 ==========
        Vector3 centerWorldPos = mainCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        Debug.Log($"🎯 中央世界坐标: {centerWorldPos}");
        DamagePopup.SpawnPopup("CENTER", centerWorldPos, Color.green);

        // ========== 右侧：SpawnPopup 方式 ==========
        Vector3 rightWorldPos = mainCam.ViewportToWorldPoint(new Vector3(0.7f, 0.5f, 10f));
        Debug.Log($"🎯 右侧世界坐标: {rightWorldPos}");
        DamagePopup.SpawnPopup("RIGHT", rightWorldPos, Color.blue);

        Debug.Log("📊 应该看到 3 个飘字: LEFT(左红-静止) CENTER(中绿-动画) RIGHT(右蓝-动画)");
    }

    /// <summary>
    /// 超级简单测试 - 只在屏幕中央生成，不做任何转换
    /// 按 Alt+6 触发
    /// </summary>
    public void SimpleTest()
    {
        Debug.Log("🔧 超级简单测试...");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas 不存在");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("UI/DamagePopup");
        if (prefab == null)
            prefab = Resources.Load<GameObject>("DamagePopup");

        if (prefab == null)
        {
            Debug.LogError("❌ Prefab 不存在");
            return;
        }

        // 创建 3 个副本，排成一行
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = Instantiate(prefab, canvas.transform);
            obj.name = $"SimpleTest_{i}";

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                float xPos = -200 + i * 200; // -200, 0, 200
                rt.anchoredPosition = new Vector2(xPos, 100);
                Debug.Log($"✅ 第 {i} 个: 位置 {rt.anchoredPosition}");
            }

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = $"[{i}]";
                tmp.color = i == 0 ? Color.red : (i == 1 ? Color.green : Color.blue);
                tmp.fontSize = 60;
                Debug.Log($"✅ 第 {i} 个: 文字 '[{i}]', 颜色 {tmp.color}");
            }

            Destroy(obj, 3f);
        }

        Debug.Log("📊 应该在屏幕上看到 3 个数字: [0](红) [1](绿) [2](蓝)");
    }
}

