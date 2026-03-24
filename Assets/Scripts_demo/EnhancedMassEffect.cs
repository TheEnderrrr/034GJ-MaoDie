using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class EnhancedMassEffect : MonoBehaviour
{
    [Header("基础设置")]
    [Range(1f, 3f)]
    public float maxScaleMultiplier = 2f;
    public float scaleSmoothTime = 0.3f;

    [Header("颜色设置")]
    public bool useContinuousColor = true;
    public float colorChangeSpeed = 5f;

    [Header("发光效果")]
    public bool enableGlow = true;
    public float glowIntensity = 1f;
    public AnimationCurve glowCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 2f);

    [Header("粒子效果")]
    public ParticleSystem massParticleSystem;
    public float particleEmissionRate = 10f;
    public float particleSpeedMultiplier = 1f;

    [Header("质量震动效果")]
    public bool enableMassShake = false;
    public float shakeIntensity = 0.1f;
    public float shakeFrequency = 10f;

    // 质量颜色数组（简化版，只包含7种颜色）
    private readonly Color[] enhancedMassColors = new Color[]
    {
        new Color(1f, 1f, 1f, 1f),     // 0: 纯白
        new Color(0f, 1f, 0f, 1f),     // 1: 绿色
        new Color(0f, 0.5f, 1f, 1f),   // 2: 蓝色
        new Color(0.5f, 0f, 1f, 1f),   // 3: 紫色
        new Color(1f, 0f, 0f, 1f),     // 4: 红色
        new Color(1f, 0.5f, 0f, 1f),   // 5: 橙黄色（包含橙和黄）
        new Color(0.5f, 0.3f, 0.1f, 1f)// 6: 棕黑色
    };

    // 引用
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private FreeBall freeBall;
    private Material glowMaterial;

    // 缓存
    private Vector3 originalScale = Vector3.one;
    private Vector3 originalPosition;
    private float currentMassRatio = 0.00001f;
    private float targetScale;
    private float scaleVelocity;
    private Color currentColor;
    private Color targetColor;
    private float shakeTimer = 0f;

    // 质量范围
    private float minMass = 0.5f;
    private float maxMass = 2f;

    // 防止重复初始化的标志
    private bool isInitialized = false;

    void Awake()
    {
        // 在Awake中获取组件，确保在Start之前就准备好
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        freeBall = GetComponent<FreeBall>();

        // 确保Rigidbody2D存在
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            Debug.LogWarning("EnhancedMassEffect: Added missing Rigidbody2D component.");
        }
    }

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;

        try
        {
            if (freeBall != null)
            {
                minMass = Mathf.Max(0.1f, freeBall.minMass); // 确保最小值有效
                maxMass = Mathf.Max(minMass + 0.1f, freeBall.maxMass); // 确保maxMass大于minMass
            }
            else
            {
                // 使用默认值，但确保有效
                minMass = Mathf.Max(0.1f, minMass);
                maxMass = Mathf.Max(minMass + 0.1f, maxMass);
            }

            // 保存原始值
            originalScale = transform.localScale;
            if (originalScale.x <= 0 || originalScale.y <= 0 || originalScale.z <= 0)
            {
                originalScale = Vector3.one;
                Debug.LogWarning("EnhancedMassEffect: Invalid original scale, resetting to Vector3.one");
            }

            originalPosition = transform.position;

            // 初始化颜色
            currentColor = spriteRenderer.color;
            targetColor = currentColor;

            // 创建发光材质（如果启用）
            if (enableGlow && spriteRenderer != null && spriteRenderer.material != null)
            {
                SetupGlowMaterial();
            }

            // 设置粒子系统（如果有）
            if (massParticleSystem != null)
            {
                SetupParticleSystem();
            }

            // 初始更新
            UpdateMassEffect(true);

            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EnhancedMassEffect initialization failed: {e.Message}");
            // 设置默认值以防止进一步错误
            originalScale = Vector3.one;
            currentMassRatio = 0.5f;
            targetScale = 1f;
            isInitialized = true;
        }
    }

    void Update()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        // 确保组件仍然存在
        if (rb == null || spriteRenderer == null)
        {
            Debug.LogError("EnhancedMassEffect: Required components are missing!");
            return;
        }

        // 更新质量效果
        UpdateMassEffect(false);

        // 更新震动效果
        if (enableMassShake && currentMassRatio > 0.3f)
        {
            UpdateShakeEffect();
        }

        // 更新粒子效果
        if (massParticleSystem != null)
        {
            UpdateParticleEffect();
        }
    }

    void SetupGlowMaterial()
    {
        try
        {
            // 复制材质以支持发光
            if (spriteRenderer.material != null)
            {
                glowMaterial = new Material(spriteRenderer.material);
                spriteRenderer.material = glowMaterial;

                // 启用发光（如果着色器支持）
                if (glowMaterial.HasProperty("_EmissionColor"))
                {
                    glowMaterial.EnableKeyword("_EMISSION");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"EnhancedMassEffect: Failed to setup glow material: {e.Message}");
            enableGlow = false;
        }
    }

    void SetupParticleSystem()
    {
        try
        {
            var emission = massParticleSystem.emission;
            emission.rateOverTime = 0f; // 初始关闭

            var main = massParticleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(currentColor);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"EnhancedMassEffect: Failed to setup particle system: {e.Message}");
            massParticleSystem = null;
        }
    }

    void UpdateMassEffect(bool immediate)
    {
        // 确保有有效的质量值
        if (rb == null) return;

        float previousRatio = currentMassRatio;

        // 安全计算质量比例
        currentMassRatio = CalculateSafeMassRatio(rb.mass);

        // 检查质量是否有显著变化
        bool massChangedSignificantly = Mathf.Abs(currentMassRatio - previousRatio) > 0.05f;

        // 更新大小
        UpdateScale(immediate);

        // 更新颜色
        UpdateColor(immediate);

        // 更新发光效果
        if (enableGlow)
        {
            UpdateGlowEffect(immediate);
        }

        // 如果有显著变化，播放效果
        if (massChangedSignificantly && !immediate)
        {
            OnMassChanged();
        }
    }

    float CalculateSafeMassRatio(float currentMass)
    {
        // 防止除零错误和无效计算
        if (Mathf.Approximately(maxMass, minMass) || maxMass <= minMass || float.IsNaN(currentMass))
        {
            return 0.5f; // 返回中间值
        }

        // 确保currentMass在有效范围内
        currentMass = Mathf.Clamp(currentMass, minMass, maxMass);

        // 计算比例
        float ratio = (currentMass - minMass) / (maxMass - minMass);

        // 确保不是NaN
        if (float.IsNaN(ratio) || float.IsInfinity(ratio))
        {
            return 0.5f;
        }

        return Mathf.Clamp01(ratio);
    }

    void UpdateScale(bool immediate)
    {
        // 安全计算目标缩放
        targetScale = 1f + (currentMassRatio * (maxScaleMultiplier - 1f));
        transform.localScale = new Vector3(1,1,1) * targetScale;
        
    }

    void ApplyScale(float scaleFactor)
    {
        // 确保scaleFactor是有效值
        if (float.IsNaN(scaleFactor) || float.IsInfinity(scaleFactor) || scaleFactor <= 0)
        {
            scaleFactor = 1f;
        }

        // 应用缩放
        Vector3 newScale = originalScale * scaleFactor;

        // 最终检查防止NaN
        if (float.IsNaN(newScale.x) || float.IsNaN(newScale.y) || float.IsNaN(newScale.z))
        {
            newScale = originalScale;
        }

        transform.localScale = newScale;
    }

    void UpdateColor(bool immediate)
    {
        if (spriteRenderer == null) return;

        // 计算目标颜色
        if (useContinuousColor)
        {
            // 计算目标颜色
            float colorIndex = currentMassRatio * (enhancedMassColors.Length - 1);
            int lowerIndex = Mathf.FloorToInt(colorIndex);
            int upperIndex = Mathf.CeilToInt(colorIndex);
            float lerpValue = colorIndex - lowerIndex;

            lowerIndex = Mathf.Clamp(lowerIndex, 0, enhancedMassColors.Length - 1);
            upperIndex = Mathf.Clamp(upperIndex, 0, enhancedMassColors.Length - 1);

            if (lowerIndex == upperIndex)
            {
                targetColor = enhancedMassColors[lowerIndex];
            }
            else
            {
                targetColor = Color.Lerp(enhancedMassColors[lowerIndex], enhancedMassColors[upperIndex], lerpValue);
            }
        }
        else
        {
            // 离散颜色
            int colorIndex = Mathf.RoundToInt(currentMassRatio * (enhancedMassColors.Length - 1));
            colorIndex = Mathf.Clamp(colorIndex, 0, enhancedMassColors.Length - 1);
            targetColor = enhancedMassColors[colorIndex];
        }

        // 确保颜色有效
        if (float.IsNaN(targetColor.r) || float.IsNaN(targetColor.g) || float.IsNaN(targetColor.b))
        {
            targetColor = Color.white;
        }

        // 应用颜色
        if (immediate || colorChangeSpeed <= 0)
        {
            currentColor = targetColor;
            spriteRenderer.color = currentColor;
        }
        else
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorChangeSpeed);
            spriteRenderer.color = currentColor;
        }
    }

    void UpdateGlowEffect(bool immediate)
    {
        if (glowMaterial == null || !glowMaterial) return;

        try
        {
            // 计算发光强度
            float glowStrength = glowCurve.Evaluate(currentMassRatio) * glowIntensity;

            if (glowMaterial.HasProperty("_EmissionColor"))
            {
                Color emissionColor = currentColor * glowStrength;
                if (float.IsNaN(emissionColor.r) || float.IsNaN(emissionColor.g) || float.IsNaN(emissionColor.b))
                {
                    emissionColor = Color.white * glowStrength;
                }
                glowMaterial.SetColor("_EmissionColor", emissionColor);
            }

            // 简单实现：通过alpha和color来模拟发光
            if (glowMaterial.HasProperty("_Color"))
            {
                Color finalColor = currentColor * (1f + glowStrength * 0.3f);
                finalColor.a = spriteRenderer.color.a;

                // 确保颜色有效
                if (float.IsNaN(finalColor.r) || float.IsNaN(finalColor.g) || float.IsNaN(finalColor.b))
                {
                    finalColor = Color.white;
                }

                if (immediate || colorChangeSpeed <= 0)
                {
                    glowMaterial.color = finalColor;
                }
                else
                {
                    glowMaterial.color = Color.Lerp(glowMaterial.color, finalColor, Time.deltaTime * colorChangeSpeed);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"EnhancedMassEffect: Glow effect update failed: {e.Message}");
            enableGlow = false;
        }
    }

    void UpdateShakeEffect()
    {
        if (!enableMassShake) return;

        shakeTimer += Time.deltaTime * shakeFrequency;

        // 震动强度基于质量比例
        float currentShakeIntensity = shakeIntensity * currentMassRatio;

        // 使用正弦波创建震动
        float shakeX = Mathf.Sin(shakeTimer * 1.3f) * currentShakeIntensity;
        float shakeY = Mathf.Cos(shakeTimer * 1.7f) * currentShakeIntensity;

        // 确保震动值有效
        if (float.IsNaN(shakeX)) shakeX = 0f;
        if (float.IsNaN(shakeY)) shakeY = 0f;

        Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0);
        transform.position = originalPosition + shakeOffset;
    }

    void UpdateParticleEffect()
    {
        try
        {
            var emission = massParticleSystem.emission;
            var main = massParticleSystem.main;

            // 粒子发射率基于质量比例
            float emissionRate = currentMassRatio * particleEmissionRate;
            emission.rateOverTime = emissionRate;

            // 粒子颜色跟随主颜色
            main.startColor = new ParticleSystem.MinMaxGradient(currentColor);

            // 粒子速度基于质量
            main.startSpeed = currentMassRatio * particleSpeedMultiplier;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"EnhancedMassEffect: Particle effect update failed: {e.Message}");
        }
    }

    void OnMassChanged()
    {
        // 质量变化时播放一次脉冲效果
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PulseEffect());
        }
    }

    IEnumerator PulseEffect()
    {
        float baseScale = targetScale;
        Vector3 pulseScale = originalScale * baseScale * 1.1f;

        // 快速放大
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pulse = Mathf.Lerp(baseScale, baseScale * 1.1f, Mathf.Sin(t * Mathf.PI * 0.5f));

            // 确保pulse值有效
            if (float.IsNaN(pulse) || pulse <= 0) pulse = baseScale;

            ApplyScale(pulse);
            yield return null;
        }

        // 恢复
        ApplyScale(targetScale);
    }

    // 公共方法
    public void SetGlowEnabled(bool enabled)
    {
        enableGlow = enabled;
        if (!enabled && glowMaterial != null)
        {
            // 恢复默认颜色
            glowMaterial.color = currentColor;
        }
    }

    public void SetShakeEnabled(bool enabled)
    {
        enableMassShake = enabled;
        if (!enabled)
        {
            // 恢复原始位置
            transform.position = originalPosition;
        }
    }

    public float GetCurrentMassRatio()
    {
        return currentMassRatio;
    }

    public Color GetCurrentColor()
    {
        return currentColor;
    }

    /// <summary>
    /// 获取当前质量颜色对应的索引（0~6，对应颜色数组中的位置）
    /// </summary>
    public int GetColorIndex()
    {
        if (useContinuousColor)
        {
            // 连续颜色：根据质量比例映射到索引
            // 将0~1均分为Length个区间，每个区间对应一个颜色
            float ratio = currentMassRatio;
            int index = Mathf.FloorToInt(ratio * enhancedMassColors.Length);
            // 当 ratio == 1 时，index 可能等于 Length，需要 clamp
            index = Mathf.Clamp(index, 0, enhancedMassColors.Length - 1);
            return index;
        }
        else
        {
            // 离散颜色：直接使用之前计算索引的方式
            int index = Mathf.RoundToInt(currentMassRatio * (enhancedMassColors.Length - 1));
            index = Mathf.Clamp(index, 0, enhancedMassColors.Length - 1);
            return index;
        }
    }

    void OnDestroy()
    {
        // 清理材质
        if (glowMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(glowMaterial);
            }
            else
            {
                DestroyImmediate(glowMaterial);
            }
        }
    }

    void OnValidate()
    {
        // 在编辑器中进行验证
        if (maxScaleMultiplier < 1f) maxScaleMultiplier = 1f;
        if (minMass < 0.1f) minMass = 0.1f;
        if (maxMass < minMass + 0.1f) maxMass = minMass + 0.1f;
    }
}
