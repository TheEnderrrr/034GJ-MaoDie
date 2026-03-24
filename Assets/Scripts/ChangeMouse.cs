//scene: aobi
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class ChangeMouse : MonoBehaviour
{
    // Start is called before the first frame update
    public Texture2D icon;
    void Start()
    {
        Cursor.SetCursor(icon, Vector2.zero, CursorMode.Auto);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
