//scene: aobi
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSmooth : MonoBehaviour
{
    [Header("目标设置")]
    public Transform target;                    // 跟随的目标玩家
    
    [Header("平滑度设置")]
    public float smoothSpeed = 5f;              // 摄像机移动平滑速度（越大越跟手）
    public float smoothDampTime = 0.3f;         // 平滑阻尼时间（越小响应越快）
    
    [Header("偏移设置")]
    public Vector3 offset = new Vector3(0, 0, -10f);  // 摄像机相对于目标的偏移量
    
    [Header("高级选项")]
    public bool useSmoothDamp = true;           // 使用 SmoothDamp 更平滑，false 使用 Lerp
    public bool lockX = false;                  // 锁定 X 轴移动
    public bool lockY = false;                  // 锁定 Y 轴移动
    public bool lockZ = false;                  // 锁定 Z 轴移动
    
    [Header("性能优化")]
    public float positionThreshold = 0.001f;    // 位置阈值，小于此值不更新（减少抖动）
    public bool updateInFixedUpdate = false;    // 在 FixedUpdate 中更新（适合物理场景）
    
    // 私有变量
    private Vector3 currentVelocity = Vector3.zero;  // 当前速度（用于 SmoothDamp）
    private Vector3 lastTargetPosition;         // 上一帧目标位置
    
    // Start is called before the first frame update
    void Start()
    {
        // 如果没有指定目标，尝试查找场景中的玩家
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraSmooth: 未找到 Player 标签的目标对象，请手动指定 Target！");
            }
        }
        
        // 初始化摄像机位置
        if (target != null)
        {
            transform.position = target.position + offset;
            lastTargetPosition = target.position;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (updateInFixedUpdate) return;  // 如果在 FixedUpdate 中更新，则跳过 Update
        UpdateCamera();
    }
    
    void FixedUpdate()
    {
        if (updateInFixedUpdate)
        {
            UpdateCamera();
        }
    }
    
    void UpdateCamera()
    {
        if (target == null)
            return;
        
        // 检查目标是否移动（避免不必要的计算）
        if (Vector3.SqrMagnitude(target.position - lastTargetPosition) < positionThreshold * positionThreshold)
        {
            // 目标几乎静止，不需要更新
            return;
        }
        lastTargetPosition = target.position;
        
        // 计算目标位置
        Vector3 desiredPosition = target.position + offset;
        
        // 根据锁定轴调整目标位置
        if (lockX) desiredPosition.x = transform.position.x;
        if (lockY) desiredPosition.y = transform.position.y;
        if (lockZ) desiredPosition.z = transform.position.z;
        
        // 使用不同的平滑算法移动摄像机
        if (useSmoothDamp)
        {
            // 使用 SmoothDamp - 更自然的平滑效果
            // 注意：smoothDampTime 不要太小，否则会产生抖动
            smoothDampTime = Mathf.Max(0.01f, smoothDampTime);
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position, 
                desiredPosition, 
                ref currentVelocity, 
                smoothDampTime,
                Mathf.Infinity,
                Time.deltaTime
            );
            transform.position = newPosition;
        }
        else
        {
            // 使用 Lerp - 简单的线性插值
            // 优化：使用固定插值系数，避免帧率影响
            float interpolationFactor = smoothSpeed * Time.deltaTime;
            Vector3 newPosition = Vector3.Lerp(
                transform.position, 
                desiredPosition, 
                interpolationFactor
            );
            transform.position = newPosition;
        }
    }
}
