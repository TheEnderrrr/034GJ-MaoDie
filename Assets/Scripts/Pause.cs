//scene: aobi
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    [Header("UI 设置")]
    [Tooltip("暂停 UI 面板对象")]
    public GameObject pauseUIPanel;  // 暂停 UI 面板
    
    [Header("场景设置")]
    [Tooltip("主页面场景的索引或名称")]
    public string mainMenuScene = "0";  // 主页面场景（默认使用索引 0）
    
    [Header("快捷键")]
    [Tooltip("是否启用 ESC 键快速暂停/继续")]
    public bool enableEscKey = true;  // 启用 ESC 键切换暂停状态
    
    private bool isPaused = false;
    private bool uiAssigned = false;

    void Start()
    {
        // 检查是否已经赋值了 UI 面板
        if (pauseUIPanel != null)
        {
            uiAssigned = true;
            pauseUIPanel.SetActive(false);  // 初始隐藏暂停 UI
        }
        else
        {
            Debug.LogWarning("[Pause] pauseUIPanel 未赋值！请确保在 Inspector 中拖拽赋值暂停 UI 面板。");
        }
    }

    void Update()
    {
        // 检测 ESC 键切换暂停状态
        if (enableEscKey && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void ResumeGame()
    {
        if (!uiAssigned)
        {
            Debug.LogError("[Pause] UI 面板未赋值，无法继续游戏！");
            return;
        }
        
        isPaused = false;
        Time.timeScale = 1f;  // 恢复时间流速
        pauseUIPanel.SetActive(false);  // 隐藏暂停 UI
        Debug.Log("[Pause] 游戏继续");
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (!uiAssigned)
        {
            Debug.LogError("[Pause] UI 面板未赋值，无法暂停游戏！");
            return;
        }
        
        isPaused = true;
        Time.timeScale = 0f;  // 停止时间流速
        pauseUIPanel.SetActive(true);  // 显示暂停 UI
        Debug.Log("[Pause] 游戏暂停");
    }

    /// <summary>
    /// 返回主页面
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuScene))
        {
            Debug.LogError("[Pause] 主页面场景未设置！请在 Inspector 中配置 mainMenuScene。");
            return;
        }
        
        // 尝试使用场景索引
        int sceneIndex = -1;
        if (int.TryParse(mainMenuScene, out sceneIndex))
        {
            if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                LoadSceneByIndex(sceneIndex);
                return;
            }
            else
            {
                Debug.LogError($"[Pause] 场景索引 {sceneIndex} 无效！请检查 Build Settings 中的场景列表。");
            }
        }
        
        // 如果索引方式失败，尝试使用场景名称
        LoadSceneByName(mainMenuScene);
    }

    /// <summary>
    /// 通过索引加载场景
    /// </summary>
    void LoadSceneByIndex(int index)
    {
        Debug.Log($"[Pause] 正在返回主页面（场景索引：{index}）...");
        Time.timeScale = 1f;  // 先恢复时间流速
        SceneManager.LoadScene(index);
    }

    /// <summary>
    /// 通过名称加载场景
    /// </summary>
    void LoadSceneByName(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            Debug.Log($"[Pause] 正在返回主页面（场景：{sceneName}）...");
            Time.timeScale = 1f;  // 先恢复时间流速
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[Pause] 场景 '{sceneName}' 不存在！请确保该场景已添加到 Build Settings 中。");
        }
    }

    /// <summary>
    /// 退出游戏（可选功能）
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[Pause] 正在退出游戏...");
        Time.timeScale = 1f;
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 清理：在游戏对象销毁时恢复时间流速
    /// </summary>
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
