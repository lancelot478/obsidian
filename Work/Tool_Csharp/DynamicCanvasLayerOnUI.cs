
using SLua;
using UnityEngine;
[CustomLuaClass]
public class DynamicCanvasLayerOnUI : MonoBehaviour {
    
    public Canvas sourceCanvas;
    [Range(-10, 10)] public int layerOffset = 1;

    private Canvas r;

    private void updateLayer() {
        if (r != null && sourceCanvas != null) {
            r.sortingOrder = sourceCanvas.sortingOrder + layerOffset;
        }
    }
    
    private void Start() {
        r = GetComponent<Canvas>();
        updateLayer();
    }

#if UNITY_EDITOR
    private void Update() {
        updateLayer();
    }
#endif
}