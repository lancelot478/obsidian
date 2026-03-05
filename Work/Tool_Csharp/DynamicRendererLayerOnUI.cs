using System;
using UnityEngine;

public class DynamicRendererLayerOnUI : MonoBehaviour {
    public Canvas sourceCanvas;
    [Range(-10, 10)] public int layerOffset = 1;

    private Renderer r;

    private void updateLayer() {
        if (r != null && sourceCanvas != null) {
            r.sortingOrder = sourceCanvas.sortingOrder + layerOffset;
        }
    }
    
    private void Start() {
        r = GetComponent<Renderer>();
        updateLayer();
    }

#if UNITY_EDITOR
    private void Update() {
        updateLayer();
    }
#endif
}
