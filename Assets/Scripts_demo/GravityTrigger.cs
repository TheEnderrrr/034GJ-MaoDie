using UnityEngine;
using System.Collections.Generic;

public class GravityTrigger : MonoBehaviour
{
    [Header("引力参数")]
    public float gravityConstant = 50f;    // 引力常数
    public float minDistance = 0.5f;         // 最小距离
    public bool enableAttraction = true;     // 是否启用引力

    [Header("质量相关引力")]
    public bool massAffectsGravity = true;   // 质量是否影响引力强度
    public float massPower = 1f;             // 质量对引力的影响指数（1为线性）

    [Header("引力视觉反馈")]
    public LineRenderer lineRendererPrefab;  // 引力线预制体
    public float lineWidthMultiplier = 0.05f; // 线宽乘数
    public bool autoCreateLinePrefab = true; // 是否自动创建LineRenderer预制体

    private Rigidbody2D parentRb;            // 父物体的刚体
    private FreeBall parentFreeBall;         // 父物体的FreeBall脚本
    private float parentMass;                // 父物体质量
    [HideInInspector] public List<Rigidbody2D> attractedObjects = new List<Rigidbody2D>();
    private Dictionary<Rigidbody2D, LineRenderer> gravityLines = new Dictionary<Rigidbody2D, LineRenderer>();
    private GameObject gravityLinesContainer; // 引力线容器

    void Start()
    {
        // 如果父物体有FreeBall脚本，从中获取
        parentFreeBall = GetComponentInParent<FreeBall>();
        if (parentFreeBall != null)
        {
            parentRb = parentFreeBall.GetComponent<Rigidbody2D>();
        }
        else
        {
            // 否则直接获取父物体的Rigidbody2D
            parentRb = GetComponentInParent<Rigidbody2D>();
        }

        if (parentRb != null)
        {
            parentMass = parentRb.mass;
        }
        else
        {
            Debug.LogWarning("GravityTrigger: 父物体没有Rigidbody2D组件！");
            enabled = false;
        }

        // 如果lineRendererPrefab为空且需要自动创建，则创建一个默认的
        if (lineRendererPrefab == null && autoCreateLinePrefab)
        {
            CreateDefaultLineRendererPrefab();
        }

        // 创建引力线容器
        CreateGravityLinesContainer();
    }

    // 创建默认的LineRenderer预制体
    void CreateDefaultLineRendererPrefab()
    {
        // 创建新物体作为模板
        GameObject defaultPrefab = new GameObject("Default_GravityLine");
        defaultPrefab.SetActive(false);

        // 添加LineRenderer组件
        LineRenderer lr = defaultPrefab.AddComponent<LineRenderer>();

        // 配置默认设置
        lr.positionCount = 2;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.01f;
        lr.useWorldSpace = true;

        // 设置默认材质和颜色
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.3f, 0.7f, 1f, 0.5f);
        lr.material = mat;

        // 设置渐变
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.3f, 0.7f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.7f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0.2f, 1f)
            }
        );
        lr.colorGradient = gradient;

        lineRendererPrefab = lr;
        Debug.Log("GravityTrigger: 创建了默认的LineRenderer预制体");
    }

    // 由FreeBall脚本调用设置引用
    public void SetParentBall(FreeBall ball)
    {
        parentFreeBall = ball;
        if (parentFreeBall != null)
        {
            parentRb = parentFreeBall.GetComponent<Rigidbody2D>();
            if (parentRb != null)
            {
                parentMass = parentRb.mass;
            }
        }
    }

    // 半径更新时调用
    public void OnRadiusUpdated()
    {
        // 可以在这里添加半径改变时的逻辑
        Debug.Log($"引力半径已更新: {GetGravityRadius():F2}");
    }

    void FixedUpdate()
    {
        if (!enableAttraction || parentRb == null) return;

        // 更新父物体质量（可能在运行时改变）
        parentMass = parentRb.mass;

        // 对每个被吸引物体施加引力
        for (int i = attractedObjects.Count - 1; i >= 0; i--)
        {
            Rigidbody2D targetRb = attractedObjects[i];
            if (targetRb == null || targetRb == parentRb)
            {
                // 移除无效的引用
                RemoveGravityLine(targetRb);
                attractedObjects.RemoveAt(i);
                continue;
            }

            ApplyGravityForce(targetRb);
            UpdateGravityLine(targetRb);
        }
    }

    void ApplyGravityForce(Rigidbody2D targetRb)
    {
        // 计算两物体之间的向量
        Vector2 direction = parentRb.position - targetRb.position;
        float distance = direction.magnitude;

        // 防止距离过小导致力过大
        distance = Mathf.Max(distance, minDistance);

        // 计算引力强度，考虑质量影响
        float massFactor = 1f;
        if (massAffectsGravity)
        {
            // 质量越大，引力越强（线性或指数）
            massFactor = Mathf.Pow(parentMass, massPower);
        }

        // 引力公式：F = G * (m1 * m2) * massFactor / r^2
        float forceMagnitude = gravityConstant * (parentMass * targetRb.mass) * massFactor / (distance * distance);

        // 计算引力向量
        Vector2 force = direction.normalized * forceMagnitude;

        // 对被吸引物体施加引力
        targetRb.AddForce(force, ForceMode2D.Force);

        // 牵引物体也受到反作用力
        parentRb.AddForce(-force * 0.5f, ForceMode2D.Force); // 减少反作用力以避免过度运动
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D otherRb = other.attachedRigidbody;

        if (otherRb != null && otherRb != parentRb && !attractedObjects.Contains(otherRb))
        {
            attractedObjects.Add(otherRb);

            // 创建引力线
            if (lineRendererPrefab != null)
            {
                CreateGravityLine(otherRb);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D otherRb = other.attachedRigidbody;

        if (otherRb != null && attractedObjects.Contains(otherRb))
        {
            attractedObjects.Remove(otherRb);
            RemoveGravityLine(otherRb);
        }
    }

    void CreateGravityLinesContainer()
    {
        if (gravityLinesContainer == null)
        {
            gravityLinesContainer = new GameObject("GravityLines");
            gravityLinesContainer.transform.SetParent(transform);
            gravityLinesContainer.transform.localPosition = Vector3.zero;
        }
    }

    void CreateGravityLine(Rigidbody2D targetRb)
    {
        if (lineRendererPrefab == null) return;

        // 创建引力线
        LineRenderer line = Instantiate(lineRendererPrefab, transform.position, Quaternion.identity);
        line.transform.SetParent(gravityLinesContainer != null ? gravityLinesContainer.transform : transform);

        // 设置线宽基于引力强度
        float lineWidth = parentMass * lineWidthMultiplier;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth * 0.5f;

        // 设置初始位置
        line.SetPosition(0, transform.position);
        line.SetPosition(1, targetRb.position);

        // 添加到字典
        gravityLines[targetRb] = line;
    }

    void UpdateGravityLine(Rigidbody2D targetRb)
    {
        if (gravityLines.TryGetValue(targetRb, out LineRenderer line) && line != null)
        {
            // 更新线的位置
            line.SetPosition(0, transform.position);
            line.SetPosition(1, targetRb.position);

            // 根据距离更新线宽和颜色
            float distance = Vector2.Distance(transform.position, targetRb.position);
            float maxRadius = GetGravityRadius();
            float distanceRatio = 1f - (distance / maxRadius);

            // 距离越近，线越宽
            float baseWidth = parentMass * lineWidthMultiplier;
            line.startWidth = baseWidth * (0.5f + distanceRatio * 0.5f);
            line.endWidth = baseWidth * 0.3f * (0.5f + distanceRatio * 0.5f);

            // 根据引力强度改变颜色
            Color lineColor = Color.Lerp(Color.blue, Color.red, distanceRatio);
            line.startColor = lineColor;
            line.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.3f);
        }
    }

    void RemoveGravityLine(Rigidbody2D targetRb)
    {
        if (gravityLines.TryGetValue(targetRb, out LineRenderer line) && line != null)
        {
            Destroy(line.gameObject);
        }
        gravityLines.Remove(targetRb);
    }

    // 清除所有引力线
    public void ClearAllGravityLines()
    {
        foreach (var line in gravityLines.Values)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        gravityLines.Clear();
    }

    // 获取引力范围
    public float GetGravityRadius()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            return collider.radius;
        }
        return 0f;
    }

    // 获取当前被吸引的物体数量
    public int GetAttractedObjectCount()
    {
        return attractedObjects.Count;
    }

    // 清除所有引力线（在销毁时调用）
    void OnDestroy()
    {
        ClearAllGravityLines();
    }
}
