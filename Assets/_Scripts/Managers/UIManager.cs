using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 必须引用
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum UIState { MainMenu, Gameplay, Result, Achievement, Battle, NodeSummary, Ending }
    
    // ... (保留你之前所有的 Header 和 变量) ...
    [Header("--- 层级 1 ---")]
    public GameObject MainMenuPanel;
    public GameObject GameplayPanel;
    public GameObject ResultPanel;
    public GameObject AchievementPanel;
    public GameObject NodeSummaryPanel;
    public GameObject BattlePanel;

    [Header("--- 层级 2 & 3 ---")]
    public GameObject HUDLayer;         
    public GameObject EndingLayer;      

    [Header("--- 组件 ---")]
    public TMP_Text TitleText;          
    public Button StartBtn;
    public Button ContinueBtn; // NEW! 新增“继续游戏”按钮 (记得去UI里加一个)
    public Button QuitBtn;              
    public Button AchievementBtn;       

    public TMP_Text EventTitleText;     
    public TMP_Text PlaceText;          
    public TMP_Text ContextText;        
    public Button ButtonA;              
    public Button ButtonB;              

    public TMP_Text ResultText;         
    public Button ConfirmResultBtn;     

    public TMP_Text SummaryTitleText;   
    public TMP_Text SummaryContentText; 
    public Button ToBeContinueBtn;      

    public Transform CardContainer;     
    public Button ConfirmBattleBtn;     

    public TMP_Text ScrollingText;      

    public Button GlobalQuitToTitleBtn; 
    public TMP_Text ResourceInfoText;
    
    public GameObject EventWindow; // 记得这里

    private DataManager.EventData currentEvent;
    private UIState currentState;
    public BattleManager SceneBattleManager;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; } 
    }
    void Start()
    {
        // ==========================================
        // 1. 通用按钮绑定 (所有场景都需要防空判断)
        // ==========================================
        
        // 退出游戏 (主菜单的退出)
        if (QuitBtn != null) 
            QuitBtn.onClick.AddListener(OnClickQuitGame);

        // 返回标题 (游戏内的退出)
        if (GlobalQuitToTitleBtn != null) 
            GlobalQuitToTitleBtn.onClick.AddListener(OnClickReturnToTitle);

        // 成就按钮 (如果主菜单有的话)
        if (AchievementBtn != null) 
            AchievementBtn.onClick.AddListener(() => SwitchState(UIState.Achievement));

        // ==========================================
        // 2. 场景逻辑分流 (核心修改)
        // ==========================================
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 🟢 情况 A：当前在【主菜单场景】 (MainMenu)
        if (currentSceneName == "MainMenu") 
        {
            // --- 绑定开始按钮 ---
            if (StartBtn != null)
            {
                StartBtn.onClick.RemoveAllListeners();
                StartBtn.onClick.AddListener(() => 
                {
                    // 告诉 GM 重置数据，然后加载场景
                    GameManager.Instance.ResetDataOnly(); 
                    SceneManager.LoadScene("SampleScene"); // ⚠️ 确保你的场景名叫 GameScene
                });
            }

            // --- 绑定继续按钮 (带存档检查) ---
            if (ContinueBtn != null)
            {
                ContinueBtn.onClick.RemoveAllListeners();
                ContinueBtn.onClick.AddListener(() => 
                {
                    // 加载存档逻辑：先切场景，再读档
                    // 这里由于要切场景，通常建议用 PlayerPrefs 记一个标记，或者让 GM 知道是 LoadGame
                    // 简化版：先加载场景，GameManager 在 Start 里会判断是否有存档（需额外实现）
                    // 暂时保持原逻辑：
                    GameManager.Instance.LoadGame(); 
                    SceneManager.LoadScene("SampleScene");
                });

                // 如果没存档，隐藏继续按钮 (这是你原来的逻辑)
                if (!PlayerPrefs.HasKey("HasSave")) 
                {
                    ContinueBtn.gameObject.SetActive(false);
                }
            }

            // 初始化状态为主菜单
            SwitchState(UIState.MainMenu);
        }
        // 🔵 情况 B：当前在【游戏场景】 (GameScene)
        else 
        {
            // --- 绑定游戏内交互按钮 (原来的逻辑) ---
            if (ButtonA != null) ButtonA.onClick.AddListener(() => OnSelectOption(true));
            if (ButtonB != null) ButtonB.onClick.AddListener(() => OnSelectOption(false));
            
            if (ConfirmResultBtn != null) ConfirmResultBtn.onClick.AddListener(ReturnToGameplay);
            if (ToBeContinueBtn != null) ToBeContinueBtn.onClick.AddListener(OnClickNextNode);

            // 强制切换到游戏状态
            //SwitchState(UIState.Gameplay);
            if (HUDLayer != null) HUDLayer.SetActive(true);
            // --- 🔥 核心修复：主动请求开局 ---
            if (GameManager.Instance != null)
            {
                // 如果是从主菜单点"继续游戏"进来的，这里可能需要区分是 Load 还是 New
                // 但为了简化，我们先假设 GM 数据已经就绪
                
                UpdatePlaceName(GameManager.Instance.GetCurrentNodeName());
                UpdateResourceDisplay();

                // 只有当当前没有事件显示时，才请求下一个 (防止重复)
                if (currentEvent == null)
                {
                    ShowNextEvent();
                }
            }
            else
            {
                Debug.LogError("⚠️ 没找到 GameManager！请从 MainMenu 开始运行，或者把 _System 预制体拖入场景测试。");
            }
        }
    }
    public void SwitchState(UIState newState)
    {
        currentState = newState;

        // --- 🛡️ 防弹衣修改：先判空，再隐藏 ---
        // 这样即使在主菜单场景里 GameplayPanel 是 None，也不会报错
        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
        if (GameplayPanel != null) GameplayPanel.SetActive(false);
        if (ResultPanel != null) ResultPanel.SetActive(false);
        if (AchievementPanel != null) AchievementPanel.SetActive(false);
        if (NodeSummaryPanel != null) NodeSummaryPanel.SetActive(false);
        if (BattlePanel != null) BattlePanel.SetActive(false);
        // ------------------------------------

        // 处理 HUD 和 Ending 层 (同样判空)
        bool showHUD = (newState != UIState.MainMenu && newState != UIState.Ending);
        if (HUDLayer != null) HUDLayer.SetActive(showHUD);
        if (EndingLayer != null) EndingLayer.SetActive(newState == UIState.Ending);

        // --- 根据状态显示对应的面板 ---
        switch (newState)
        {
            case UIState.MainMenu: 
                if (MainMenuPanel != null) MainMenuPanel.SetActive(true); 
                break;

            case UIState.Gameplay: 
                if (GameplayPanel != null) 
                {
                    GameplayPanel.SetActive(true); 
                    if(EventWindow != null) EventWindow.SetActive(true); 
                }
                break;

            case UIState.Result: 
                if (ResultPanel != null) ResultPanel.SetActive(true); 
                break;

            case UIState.Achievement: 
                if (AchievementPanel != null) AchievementPanel.SetActive(true); 
                break;

            case UIState.NodeSummary: 
                if (NodeSummaryPanel != null) NodeSummaryPanel.SetActive(true); 
                break;

            case UIState.Battle: 
                if (BattlePanel != null) BattlePanel.SetActive(true); 
                break;

            case UIState.Ending: 
                /* 结局逻辑，如果有独立面板也记得判空 */ 
                break;
        }
    }
    public void ShowNextEvent()
    {
        Debug.Log("🕵️‍♂️ [1] ShowNextEvent 开始运行...");

        // --- 1. 检查数据源 ---
        if (DataManager.Instance == null)
        {
            Debug.LogError("❌ [中断] DataManager 是 null！");
            return;
        }

        currentEvent = DataManager.Instance.GetRandomEvent();

        if (currentEvent == null) 
        {
            Debug.LogError("❌ [中断] 获取到的事件是 null！CSV 可能没加载。");
            return;
        }

        Debug.Log($"🕵️‍♂️ [2] 获取事件成功 | ID: {currentEvent.ID} | 标题: {currentEvent.Title} | 和平状态(IsPeaceful): {currentEvent.IsPeaceful}");

        // --- 2. 逻辑分流 ---
        if (currentEvent.IsPeaceful == false)
        {
            Debug.Log("⚔️ [3] 进入【战斗】分支");
            
            // 检查战斗管理器
            if (SceneBattleManager == null)
            {
                Debug.LogError("❌ [中断] 试图进入战斗，但 SceneBattleManager 没拖进 UIManager！");
                return;
            }

            SwitchState(UIState.Battle);
            Debug.Log("⚔️ [4] 呼叫 BattleManager.StartBattle...");
            SceneBattleManager.StartBattle(currentEvent);
        }
        else
        {
            Debug.Log("🕊️ [3] 进入【剧情】分支");

            SwitchState(UIState.Gameplay);
            Debug.Log("🕊️ [4] 面板已打开 (SwitchState 完成)");

            // --- 3. 赋值检查 (这里最容易报错中断) ---
            
            // 检查标题组件
            if (EventTitleText == null) Debug.LogError("❌ [UI丢失] EventTitleText 没拖！标题无法显示！");
            else EventTitleText.text = currentEvent.Title;

            // 检查内容组件
            if (ContextText == null) Debug.LogError("❌ [UI丢失] ContextText 没拖！内容无法显示！");
            else ContextText.text = currentEvent.Context;

            Debug.Log("🕊️ [5] 文字赋值完成");
            
            // 设置按钮 A
            if (ButtonA != null)
            {
                var txtA = ButtonA.GetComponentInChildren<TMP_Text>();
                if (txtA != null) txtA.text = currentEvent.OptA_Text;
                ButtonA.interactable = true;
            }
            else Debug.LogError("❌ [UI丢失] ButtonA 没拖！");

            // 设置按钮 B
            if (ButtonB != null)
            {
                var txtB = ButtonB.GetComponentInChildren<TMP_Text>();
                if (txtB != null) txtB.text = currentEvent.OptB_Text;
                CheckOptionCondition(ButtonB, currentEvent.OptB_Condition);
            }
            else Debug.LogError("❌ [UI丢失] ButtonB 没拖！");

            Debug.Log("✅ [6] ShowNextEvent 全部执行完毕，界面应该显示了！");
        }
    }
    // --- 核心逻辑：解析 "102:500" (ID:阈值) ---
    void CheckOptionCondition(Button btn, string conditionStr)
    {
        // 先重置为可用
        btn.interactable = true;
        
        // 如果没有条件，直接返回
        if (string.IsNullOrEmpty(conditionStr) || conditionStr == "0:0") return;

        try 
        {
            string[] parts = conditionStr.Split(':');
            int resID = int.Parse(parts[0]);
            int threshold = int.Parse(parts[1]);
            
            int currentVal = ResourceManager.Instance.GetResourceValue(resID);

            // 如果资源不足
            if (currentVal < threshold)
            {
                btn.interactable = false; // 变灰禁用
                
                // 在按钮文字上加红色提示
                var txt = btn.GetComponentInChildren<TMP_Text>();
                string resName = ResourceManager.Instance.GetResName(resID);
                txt.text += $"\n<color=red><size=80%>(需 {resName} {threshold})</size></color>";
            }
        }
        catch 
        { 
            Debug.LogWarning($"条件解析失败: {conditionStr}"); 
        }
    }
    // --- 核心：条件解析逻辑 ---

    // 替换掉原来的 void OnSelectOption(bool isA)
    void OnSelectOption(bool isA)
    {
        Debug.Log($"🖱️ [点击测试] 选择了: {(isA ? "A" : "B")}");

        if (currentEvent == null)
        {
            Debug.LogError("❌ 操作无效：currentEvent 是空的！");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ 操作无效：GameManager 是空的！");
            return;
        }

        // 调用逻辑
        string resultStr = GameManager.Instance.ResolveEventOption(currentEvent, isA);
        Debug.Log($"✅ 结算结果: {resultStr}");
        
        ShowResult(resultStr);
    }
    public void ShowResult(string resultStr)
    {
        if (currentState == UIState.Ending)
        {
            Debug.Log("[UI]Have been check the Panel Entered Ending, Stop the Result");
            return;
        }
        SwitchState(UIState.Result);
        ResultText.text = resultStr;
        UpdateResourceDisplay();
    }


    // --- 修改：点击结果界面的确认按钮后 ---
    void ReturnToGameplay()
    {
        SwitchState(UIState.Gameplay);
        
        // 关键改动：不再直接 ShowNextEvent，而是问 GameManager 下一步干嘛
        // (是继续下一个随机事件？还是跳指定事件？还是进结算？)
        GameManager.Instance.CheckGameStateAfterResult(); 
    }

    // --- 新增：显示指定 ID 的事件 ---
    public void ShowSpecificEvent(int eventID)
    {
        // 从所有事件中查找
        currentEvent = DataManager.Instance.AllEvents.Find(e => e.ID == eventID);
        
        if (currentEvent == null) 
        {
            Debug.LogError($"找不到 ID 为 {eventID} 的事件！");
            // 保底：显示个随机的
            ShowNextEvent(); 
            return;
        }

        // 显示逻辑 (复用之前的)
        if (currentEvent.IsPeaceful == false)
        {
            SwitchState(UIState.Battle);
            if(BattleManager.Instance != null) BattleManager.Instance.StartBattle(currentEvent);
        }
        else
        {
            SwitchState(UIState.Gameplay);
            EventTitleText.text = currentEvent.Title;
            ContextText.text = currentEvent.Context;
            
            var txtA = ButtonA.GetComponentInChildren<TMP_Text>();
            if(txtA) txtA.text = currentEvent.OptA_Text;
            
            var txtB = ButtonB.GetComponentInChildren<TMP_Text>();
            if(txtB) txtB.text = currentEvent.OptB_Text;

            CheckOptionCondition(ButtonB, currentEvent.OptB_Condition);
        }
    }
    public void ShowNodeSummary(string title, string content)
    {
        SwitchState(UIState.NodeSummary);
        SummaryTitleText.text = title;
        SummaryContentText.text = content;
    }

    // --- 💀 周日思考：结局显示 ---
    public void ShowEnding(string content)
    {
        SwitchState(UIState.Ending);
        ScrollingText.text = content;
        // 这里可以加一个协程让它滚动
    }

    void OnClickNextNode()
    {
        GameManager.Instance.GoToNextNode();
    }

    public void OnClickReturnToTitle()
    {
        SwitchState(UIState.MainMenu);
    }

    void OnClickQuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void UpdateResourceDisplay()
    {
        // ... (ResourceItem 已经处理了显示，这里可以留空或做其他刷新)
    }
    
    public void UpdatePlaceName(string name)
    {
        if(PlaceText != null) PlaceText.text = name;
    }
}