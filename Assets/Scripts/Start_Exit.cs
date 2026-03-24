//scene: start

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Start_Exit : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("点击开始按钮后切换到的目标场景名称或索引")]
    public string targetSceneName = "GameScene";  // 目标场景名称
    
    [Header("按钮模式")]
    [Tooltip("true=开始游戏按钮，false=退出游戏按钮")]
    public bool isStartButton = true;
    
    [Header("编辑器选项")]
    [Tooltip("在编辑器中是否自动绑定按钮事件")]
    public bool autoBindInEditor = true;

    private void Awake()
    {
        // 解除垂直同步（关键！）
        QualitySettings.vSyncCount = 0;
    
        // 设置目标帧率（-1 = 无限制，按硬件能力跑）
        Application.targetFrameRate = -1; 
        // 或指定高帧率（如 144）
        // Application.targetFrameRate = 144;
    }

    void Start()
    {
        // 如果不是自动绑定，需要手动在 Inspector 中配置 OnClick 事件
        if (autoBindInEditor)
        {
            BindButtonEvent();
        }
    }

    /// <summary>
    /// 绑定按钮点击事件
    /// </summary>
    void BindButtonEvent()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning("[Start_Exit] 该对象上没有找到 Button 组件！请确保脚本挂载在带有 Button 组件的对象上。");
            return;
        }
        
        // 移除旧的事件监听（避免重复）
        button.onClick.RemoveAllListeners();
        
        // 添加新的事件监听
        button.onClick.AddListener(OnButtonClick);
    }

    /// <summary>
    /// 按钮点击处理
    /// </summary>
    public void OnButtonClick()
    {
        if (isStartButton)
        {
            StartGame();
        }
        else
        {
            ExitGame();
        }
    }

    /// <summary>
    /// 开始游戏 - 切换到目标场景
    /// </summary>
    public void StartGame()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[Start_Exit] 目标场景名称未设置！请在 Inspector 中配置 targetSceneName。");
            return;
        }
        
        // 尝试使用场景索引（数字优先）
        int sceneIndex = -1;
        if (int.TryParse(targetSceneName, out sceneIndex))
        {
            if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"[Start_Exit] 正在切换到场景索引：{sceneIndex}");
                SceneManager.LoadScene(sceneIndex);
                return;
            }
            else
            {
                Debug.LogError($"[Start_Exit] 场景索引 {sceneIndex} 无效！请检查 Build Settings 中的场景列表。");
                return;
            }
        }
        
        // 如果索引方式失败，尝试使用场景名称
        Scene scene = SceneManager.GetSceneByName(targetSceneName);
        if (scene.IsValid())
        {
            Debug.Log($"[Start_Exit] 正在切换到场景：{targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[Start_Exit] 场景 '{targetSceneName}' 不存在！请确保该场景已添加到 Build Settings 中。");
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("[Start_Exit] 正在退出游戏...");
        
#if UNITY_EDITOR
        // 在编辑器中退出播放模式
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建版本中退出游戏
        Application.Quit();
#endif
    }
}
