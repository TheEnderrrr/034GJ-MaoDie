//scene: start
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBalls : MonoBehaviour
{
    [Header("生成区域设置")]
    public float widthOffset = 200f;  // 宽度方向额外增加的尺寸
    public float heightOffset = 50f;  // 高度方向额外增加的尺寸
    
    [Header("小球设置")]
    public GameObject ballPrefab;  // 小球预制体
    public int maxBallCount = 10;  // 最大小球数量
    public float minBallSize = 0.5f;  // 最小小球大小
    public float maxBallSize = 2f;    // 最大小球大小
    public float moveSpeed = 50f;     // 小球移动速度
    
    private List<GameObject> activeBalls = new List<GameObject>();
    private Camera mainCamera;
    private Vector3 screenCenter;
    private float spawnAreaWidth;
    private float spawnAreaHeight;
    private float worldLeft, worldRight, worldBottom, worldTop;

    void Start()
    {
        mainCamera = Camera.main;
        screenCenter = mainCamera.transform.position;
        
        // 根据屏幕大小计算生成区域（屏幕宽高 + 偏移量）
        spawnAreaWidth = Screen.width + widthOffset;
        spawnAreaHeight = Screen.height + heightOffset;
        
        // 计算世界坐标边界
        CalculateWorldBounds();
        
        Debug.Log($"[SpawnBalls] 屏幕大小：{Screen.width}x{Screen.height}, 生成区域：{spawnAreaWidth}x{spawnAreaHeight}");
        Debug.Log($"[SpawnBalls] 世界坐标范围 - 左:{worldLeft}, 右:{worldRight}, 下:{worldBottom}, 上:{worldTop}");
        
        // 初始生成一批小球
        for (int i = 0; i < maxBallCount; i++)
        {
            CreateRandomBall();
        }
    }

    /// <summary>
    /// 计算摄像机可见范围的世界坐标边界
    /// </summary>
    void CalculateWorldBounds()
    {
        // 获取屏幕四个角的世界坐标
        Vector3 topLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height, mainCamera.nearClipPlane));
        Vector3 bottomRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0, mainCamera.nearClipPlane));
        
        // 计算扩展后的边界（在屏幕基础上向外扩展）
        float halfWidthExtra = widthOffset / 2f;
        float halfHeightExtra = heightOffset / 2f;
        
        worldLeft = topLeft.x - halfWidthExtra;
        worldRight = bottomRight.x + halfWidthExtra;
        worldBottom = bottomRight.y - halfHeightExtra;
        worldTop = topLeft.y + halfHeightExtra;
        
        // 更新屏幕中心
        screenCenter = new Vector3(
            (worldLeft + worldRight) / 2f,
            (worldBottom + worldTop) / 2f,
            screenCenter.z
        );
    }

    void Update()
    {
        // 更新所有小球的位置
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            if (activeBalls[i] == null)
            {
                activeBalls.RemoveAt(i);
                continue;
            }
            
            MoveBall(activeBalls[i]);
            
            // 检查是否离开屏幕范围
            if (IsOutOfBounds(activeBalls[i]))
            {
                Destroy(activeBalls[i]);
                activeBalls.RemoveAt(i);
                // 重新生成一个新的小球
                CreateRandomBall();
            }
        }
    }

    /// <summary>
    /// 在指定区域内随机生成一个小球
    /// </summary>
    void CreateRandomBall()
    {
        // 使用世界坐标边界来生成位置
        Vector3 spawnPos = new Vector3(
            Random.Range(worldLeft, worldRight),
            Random.Range(worldBottom, worldTop),
            0f
        );
        
        // 随机生成大小
        float randomSize = Random.Range(minBallSize, maxBallSize);
        
        // 随机生成颜色
        Color randomColor = new Color(
            Random.value,
            Random.value,
            Random.value
        );
        
        // 创建小球
        GameObject ball;
        if (ballPrefab != null)
        {
            ball = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // 如果没有预制体，创建一个临时的球体
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = spawnPos;
        }
        
        ball.transform.localScale = Vector3.one * randomSize;
        
        // 设置颜色
        Renderer renderer = ball.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(renderer.material);
            mat.color = randomColor;
            renderer.material = mat;
        }
        
        // 添加刚体组件让小球自由运动
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = ball.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;  // 不受重力影响
        rb.freezeRotation = true;  // 冻结旋转
        
        // 给一个随机的初始速度
        rb.velocity = new Vector2(
            Random.Range(-1f, 1f) * moveSpeed,
            Random.Range(-1f, 1f) * moveSpeed
        ).normalized * moveSpeed;
        
        activeBalls.Add(ball);
    }

    /// <summary>
    /// 移动小球（如果有刚体则通过物理引擎移动）
    /// </summary>
    void MoveBall(GameObject ball)
    {
        // 小球通过 Rigidbody2D 自动移动
    }

    /// <summary>
    /// 检查小球是否离开屏幕范围
    /// </summary>
    bool IsOutOfBounds(GameObject ball)
    {
        Vector3 pos = ball.transform.position;
        
        // 如果超出世界坐标范围就删除
        return pos.x < worldLeft || pos.x > worldRight ||
               pos.y < worldBottom || pos.y > worldTop;
    }
}
