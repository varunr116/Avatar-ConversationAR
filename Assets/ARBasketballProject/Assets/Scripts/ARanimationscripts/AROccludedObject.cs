using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AROccludedObject : MonoBehaviour
{
    [Header("Occlusion Settings")]
    public bool enableOcclusion = true;
    public float occlusionOffsetMeters = 0.08f; // Distance behind real objects to start hiding
    
    private Renderer[] renderers;
    private AROcclusionManager occlusionManager;
    private Camera arCamera;
    
    void Start()
    {
        // Get all renderers on this object and children
        renderers = GetComponentsInChildren<Renderer>();
        
        // Find AR components
        occlusionManager = FindObjectOfType<AROcclusionManager>();
        arCamera = Camera.main;
        
        if (occlusionManager == null)
        {
            Debug.LogWarning("AR Occlusion Manager not found! Occlusion will not work.");
            return;
        }
        
        // Set up materials for occlusion
        SetupOcclusionMaterials();
    }
    
    void SetupOcclusionMaterials()
    {
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                // Enable depth testing for occlusion
                material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                material.SetFloat("_ZWrite", 1.0f);
            }
        }
    }
    
    void Update()
    {
        if (!enableOcclusion || occlusionManager == null) return;
        
        // Check if object should be occluded
        UpdateOcclusion();
    }
    
    void UpdateOcclusion()
    {
        
        
        // Example: Check distance to camera for additional culling
        float distanceToCamera = Vector3.Distance(transform.position, arCamera.transform.position);
        
        // Hide objects that are too far away
        bool shouldBeVisible = distanceToCamera < 10f; // 10 meter max distance
        
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = shouldBeVisible;
        }
    }
    
    public void ToggleOcclusion()
    {
        enableOcclusion = !enableOcclusion;
        Debug.Log("Occlusion: " + (enableOcclusion ? "ON" : "OFF"));
    }
}