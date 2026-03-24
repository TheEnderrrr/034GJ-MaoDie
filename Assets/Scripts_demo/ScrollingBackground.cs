using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    public float scrollSpeedX = 0.1f; // X轴滚动速度
    public float scrollSpeedY = 0f;   // Y轴滚动速度（星空可设为0.05）
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;
        rend.material.mainTextureOffset = new Vector2(offsetX, offsetY);

        // 如果使用多个材质层（如星空+云层）
        // rend.materials[1].mainTextureOffset = new Vector2(offsetX * 0.5f, offsetY);
    }
}
