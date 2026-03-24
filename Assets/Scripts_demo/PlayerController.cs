using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    [Header("移动参数")]
    public float maxSpeed = 5f;              // 最大移动速度
    public float acceleration = 15f;         // 正向加速度
    public float deceleration = 25f;         // 减速度（通常比加速度大）

    [Header("转向控制")]
    [Range(0f, 1f)] public float turnResponsiveness = 0.8f; // 转向响应度
    public bool instantDirection = false;    // 建议设为false以实现真实物理

    [Header("手感调整")]
    public float minSpeedForEffects = 0.2f;  // 应用转向效果的最小速度
    public float angleSensitivity = 1.5f;    // 角度敏感度（越高减速越明显）

    [Header("玩家特有引力设置")]
    public bool dynamicGravityRadius = true;  // 是否动态调整引力半径
    public float minGravityRadius = 1f;
    public float maxGravityRadius = 5f;

    [Header("控制配置")]
    public bool useMouseControl = true;      // 使用鼠标控制
    public KeyCode controlKey = KeyCode.Mouse1; // 控制键（默认鼠标右键）

    // 引用
    private FreeBall freeBall;               // 球体的FreeBall组件
    public Rigidbody2D rb;                  // 球体的刚体
    private CircleCollider2D gravityCollider; // 引力碰撞器
    private Vector2 lastMouseDir = Vector2.right;
    private bool isDragging = false;

    // 缓存的原FreeBall设置
    private float originalMinSpeed;
    private float originalMaxSpeed;
    private float originalMinMass;
    private float originalMaxMass;
    private bool originalShowGravityLines;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }
    void Start()
    {
        QualitySettings.vSyncCount = 0; // 0表示关闭垂直同步
        // 获取或添加FreeBall组件
        freeBall = GetComponent<FreeBall>();
        if (freeBall == null)
        {
            Debug.LogError("PlayerController需要FreeBall组件！");
            enabled = false;
            return;
        }

        // 保存原始设置
        CacheOriginalSettings();

        // 获取刚体
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("球体没有Rigidbody2D组件！");
            enabled = false;
            return;
        }

        // 获取引力碰撞器
        GravityTrigger gravityTrigger = GetComponentInChildren<GravityTrigger>();
        if (gravityTrigger != null)
        {
            gravityCollider = gravityTrigger.GetComponent<CircleCollider2D>();
        }

        // 设置玩家特有的引力线显示（默认开启）
        freeBall.showGravityLines = true;
        if (freeBall.showGravityLines && freeBall.gravityLinePrefab == null)
        {
            freeBall.gravityLinePrefab = CreatePlayerGravityLinePrefab();
        }

        // 初始化玩家引力线显示
        freeBall.ToggleGravityLines();

        Debug.Log($"玩家控制器已附加到: {gameObject.name}");
    }

    void CacheOriginalSettings()
    {
        // 保存FreeBall的原始设置
        originalMinSpeed = freeBall.minSpeed;
        originalMaxSpeed = freeBall.maxSpeed;
        originalMinMass = freeBall.minMass;
        originalMaxMass = freeBall.maxMass;
        originalShowGravityLines = freeBall.showGravityLines;

        // 设置玩家特有的值（取消随机范围）
        freeBall.minSpeed = 0f;
        freeBall.maxSpeed = 0f;
        freeBall.minMass = rb.mass;
        freeBall.maxMass = rb.mass;
    }

    void RestoreOriginalSettings()
    {
        // 恢复原始设置
        if (freeBall != null)
        {
            freeBall.minSpeed = originalMinSpeed;
            freeBall.maxSpeed = originalMaxSpeed;
            freeBall.minMass = originalMinMass;
            freeBall.maxMass = originalMaxMass;
            freeBall.showGravityLines = originalShowGravityLines;
        }
    }

    GameObject CreatePlayerGravityLinePrefab()
    {
        // 创建玩家专用的引力线预制体（颜色不同）
        GameObject prefab = new GameObject("Player_GravityLine");
        prefab.SetActive(true);

        LineRenderer lr = prefab.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.01f;
        lr.useWorldSpace = true;

        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        lr.material = lineMaterial;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.cyan, 0.0f),
                new GradientColorKey(Color.magenta, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.3f, 1.0f)
            }
        );
        lr.colorGradient = gradient;

        return prefab;
    }

    void FixedUpdate()
    {
        // 更新玩家引力半径（动态调整）
        if (dynamicGravityRadius && gravityCollider != null)
        {
            UpdatePlayerGravityRadius();
        }

        // 鼠标控制
        if (useMouseControl && Input.GetKey(controlKey))
        {
            isDragging = true;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            Vector2 targetDir = (mouseWorldPos - transform.position).normalized;
            lastMouseDir = targetDir;

            Vector2 currentVelocity = rb.velocity;
            float currentSpeed = currentVelocity.magnitude;

            // 物理转向
            HandlePhysicalTurning(targetDir, currentVelocity, currentSpeed);
        }
        else
        {
            isDragging = false;
            // 松开控制键：自然减速
            ApplyNaturalDeceleration();
        }
    }

    void UpdatePlayerGravityRadius()
    {
        if (gravityCollider != null && freeBall != null)
        {
            float currentSpeed = rb.velocity.magnitude;
            float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);

            // 计算物体基本半径
            float objectBaseRadius = freeBall.GetObjectRadius();

            // 动态调整：根据速度和质量
            float radius = freeBall.baseGravityRadius +
                          (rb.mass * freeBall.massRadiusMultiplier) +
                          (speedFactor * 0.5f);

            // 限制范围
            radius = Mathf.Clamp(radius, minGravityRadius, maxGravityRadius);

            // 确保最小半径不小于物体基本半径的2倍
            float minRadius = objectBaseRadius * 2f;
            radius = Mathf.Max(radius, minRadius);

            gravityCollider.radius = radius;
        }
    }

    void HandlePhysicalTurning(Vector2 targetDir, Vector2 currentVelocity, float currentSpeed)
    {
        if (currentSpeed < minSpeedForEffects)
        {
            // 速度很小：直接加速
            rb.velocity = targetDir * Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
            return;
        }

        // 计算当前方向
        Vector2 currentDir = currentVelocity / currentSpeed;

        // 计算转向角度（0-180度）
        float turnAngle = Vector2.Angle(currentDir, targetDir);

        // 角度越大，减速效果越强，加速效果越弱
        float angleFactor = Mathf.Clamp01(turnAngle / 180f * angleSensitivity);

        // 减速度：与角度成正比
        float currentDeceleration = deceleration * angleFactor;

        // 加速度：与角度成反比（但总是至少有最小加速度）
        float effectiveAcceleration = acceleration * (1f - angleFactor * 0.7f);
        effectiveAcceleration = Mathf.Max(effectiveAcceleration, acceleration * 0.3f);

        if (instantDirection)
        {
            // 即使使用"即时方向"，也考虑减速效果
            if (angleFactor > 0.3f) // 有明显转向时才减速
            {
                // 应用减速度
                float decelAmount = currentDeceleration * Time.fixedDeltaTime;
                float newSpeed = Mathf.Max(currentSpeed - decelAmount, 0f);

                if (newSpeed > 0.1f)
                {
                    // 保持当前方向减速
                    rb.velocity = currentDir * newSpeed;
                }
                else
                {
                    // 减速到接近零，准备重新加速
                    rb.velocity = targetDir * 0.1f;
                }
            }

            // 同时应用加速度
            rb.velocity += targetDir * effectiveAcceleration * Time.fixedDeltaTime;
        }
        else
        {
            // 物理方式：同时应用减速和转向加速

            // 1. 应用减速度（方向与当前速度相反）
            Vector2 decelVector = -currentDir * currentDeceleration * Time.fixedDeltaTime;
            rb.velocity += decelVector;

            // 2. 应用转向力（方向朝向目标）
            // 计算转向力：目标速度减去当前速度
            Vector2 desiredVelocity = targetDir * maxSpeed;
            Vector2 steeringForce = desiredVelocity - rb.velocity;

            // 限制转向力大小
            float steeringMagnitude = steeringForce.magnitude;
            float maxSteeringForce = effectiveAcceleration * 2f;
            if (steeringMagnitude > maxSteeringForce)
            {
                steeringForce = steeringForce.normalized * maxSteeringForce;
            }

            // 应用转向力
            rb.velocity += steeringForce * turnResponsiveness * Time.fixedDeltaTime;
        }

        // 确保不超过最大速度
        float finalSpeed = rb.velocity.magnitude;
        if (finalSpeed > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        // 防止速度过小导致抖动
        if (finalSpeed < 0.1f && currentSpeed > 1f)
        {
            rb.velocity = targetDir * 0.1f;
        }
    }

    void ApplyNaturalDeceleration()
    {
        float currentSpeed = rb.velocity.magnitude;
        if (currentSpeed > 0.1f)
        {
            // 使用线性减速，不是指数衰减
            float decelAmount = Mathf.Min(currentSpeed, deceleration * 0.5f * Time.fixedDeltaTime);
            rb.velocity -= rb.velocity.normalized * decelAmount;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    void Update()
    {
        // 调整角度敏感度
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
        {
            angleSensitivity = Mathf.Clamp(angleSensitivity + 0.1f, 0.5f, 3f);
            Debug.Log($"角度敏感度: {angleSensitivity:F2}");
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            angleSensitivity = Mathf.Clamp(angleSensitivity - 0.1f, 0.5f, 3f);
            Debug.Log($"角度敏感度: {angleSensitivity:F2}");
        }

        // 切换引力开关
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleGravity();
        }

        // 调整引力常数
        GravityTrigger gravityTrigger = GetComponentInChildren<GravityTrigger>();
        if (gravityTrigger != null)
        {
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                gravityTrigger.gravityConstant = Mathf.Max(gravityTrigger.gravityConstant - 0.5f, 0.1f);
                Debug.Log($"引力常数: {gravityTrigger.gravityConstant:F2}");
            }

            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                gravityTrigger.gravityConstant = Mathf.Min(gravityTrigger.gravityConstant + 0.5f, 20f);
                Debug.Log($"引力常数: {gravityTrigger.gravityConstant:F2}");
            }
        }
    }

    void ToggleGravity()
    {
        GravityTrigger gravityTrigger = GetComponentInChildren<GravityTrigger>();
        if (gravityTrigger != null)
        {
            bool currentState = gravityTrigger.enableAttraction;
            gravityTrigger.enableAttraction = !currentState;
            Debug.Log($"玩家引力: {(!currentState ? "开启" : "关闭")}");
        }
    }

    //void OnGUI()
    //{
    //    // 显示调试信息
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 16;
    //    style.normal.textColor = Color.white;

    //    Vector2 currentDir = rb.velocity.magnitude > 0.1f ? rb.velocity.normalized : Vector2.zero;
    //    float currentSpeed = rb.velocity.magnitude;

    //    // 计算转向角度
    //    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    mouseWorldPos.z = 0f;
    //    Vector2 targetDir = (mouseWorldPos - transform.position).normalized;

    //    float turnAngle = 0f;
    //    if (currentSpeed > 0.1f)
    //    {
    //        turnAngle = Vector2.Angle(currentDir, targetDir);
    //    }

    //    // 引力信息
    //    string gravityInfo = "";
    //    GravityTrigger gravityTrigger = GetComponentInChildren<GravityTrigger>();
    //    if (gravityTrigger != null)
    //    {
    //        gravityInfo = $"\n引力: {(gravityTrigger.enableAttraction ? "开启" : "关闭")}" +
    //                     $"\n引力常数: {gravityTrigger.gravityConstant:F2}" +
    //                     $"\n引力半径: {gravityCollider?.radius ?? 0f:F2}" +
    //                     $"\n吸引物体数: {gravityTrigger.GetAttractedObjectCount()}";
    //    }

    //    // 显示信息
    //    string info = $"速度: {currentSpeed:F2}" +
    //                 $"\n目标角度: {turnAngle:F0}°" +
    //                 $"\n敏感度: {angleSensitivity:F2} (+/-调整)" +
    //                 $"\n模式: {(instantDirection ? "即时" : "物理")}" +
    //                 $"\n质量: {rb.mass:F2}" +
    //                 gravityInfo +
    //                 $"\n\nG: 切换引力" +
    //                 $"\n[/]: 调整引力常数";

    //    GUI.Label(new Rect(10, 10, 300, 250), info, style);
    //}

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null) return;

        // 绘制当前速度
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, rb.velocity);

        // 绘制目标方向
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 targetDir = (mouseWorldPos - transform.position).normalized;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, targetDir * 2f);

        // 绘制转向角度指示
        if (rb.velocity.magnitude > 0.1f)
        {
            Vector2 currentDir = rb.velocity.normalized;
            float angle = Vector2.Angle(currentDir, targetDir);

            // 根据角度大小显示不同颜色
            if (angle > 120f) Gizmos.color = Color.red;
            else if (angle > 60f) Gizmos.color = new Color(1f, 0.5f, 0f); // 橙色
            else if (angle > 30f) Gizmos.color = Color.yellow;
            else Gizmos.color = Color.green;

            // 角度越大，圆圈越大
            float circleSize = 0.8f + angle / 90f;
            Gizmos.DrawWireSphere(transform.position, circleSize);
        }

        // 绘制引力范围
        if (gravityCollider != null)
        {
            GravityTrigger gravityTrigger = GetComponentInChildren<GravityTrigger>();
            if (gravityTrigger != null && gravityTrigger.enableAttraction)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.1f);
                Gizmos.DrawWireSphere(transform.position, gravityCollider.radius);

                // 绘制引力强度指示（根据质量）
                float massIndicator = rb.mass * 0.2f;
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, gravityCollider.radius * 0.8f);
            }
        }
    }

    void OnDestroy()
    {
        // 恢复原始设置
        RestoreOriginalSettings();

        // 如果有引力线，清除它们
        GravityTrigger gravityTrigger = GetComponentInChildren<GravityTrigger>();
        if (gravityTrigger != null)
        {
            gravityTrigger.ClearAllGravityLines();
        }
    }
}
