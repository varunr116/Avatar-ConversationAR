using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Unity.Mathematics;

public class ARTapToPlace : MonoBehaviour
{
    [Header("Prefab to Place")]
    public GameObject basketballHoopPrefab;
    
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    
    [Header("Settings")]
    public bool isWithoutAvatar = false; // Flag to determine controller type
    
    private GameObject placedHoop;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    
    void Start()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();
        
        if (arCamera == null)
            arCamera = Camera.main;
    }
    
    void Update()
    {
        // Check for touch input using new Input System
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                
                // Check if touch is over UI element
                if (!IsPointerOverUIElement())
                {
                    TryPlaceHoop(touchPosition);
                }
            }
        }
        
        // For testing in editor with mouse
        #if UNITY_EDITOR
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            // Check if mouse is over UI element
            if (!IsPointerOverUIElement())
            {
                TryPlaceHoop(mousePosition);
            }
        }
        #endif
    }
    
    // Check if touch/mouse is over a UI element
    private bool IsPointerOverUIElement()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        
        #if UNITY_EDITOR
        eventData.position = Mouse.current.position.ReadValue();
        #else
        if (Touchscreen.current != null)
            eventData.position = Touchscreen.current.primaryTouch.position.ReadValue();
        #endif
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
    
   void TryPlaceHoop(Vector2 screenPosition)
{
    if (raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
    {
        Pose hitPose = raycastHits[0].pose;
        
        if (placedHoop != null)
        {
            DestroyImmediate(placedHoop);
        }
        
        // Calculate rotation to face camera
        Vector3 cameraPosition = arCamera.transform.position;
        Vector3 placementPosition = hitPose.position;
        
        // Create direction vector from placement to camera (on Y plane only)
        Vector3 directionToCamera = (cameraPosition - placementPosition).normalized;
        directionToCamera.y = 0; // Keep hoop upright, only rotate on Y axis
        
        // Create rotation that faces the camera
        Quaternion faceCamera = Quaternion.LookRotation(directionToCamera);
        
        // Instantiate with camera-facing rotation
        placedHoop = Instantiate(basketballHoopPrefab, placementPosition, faceCamera);
        
        // Connect UI to the new basketball controller
        BasketballUIController uiController = FindObjectOfType<BasketballUIController>();
        if (uiController != null)
        {
            // Set the flag in UI controller
            uiController.isWithoutAvatar = isWithoutAvatar;
            
            if (isWithoutAvatar)
            {
                SimpleARBasketball simpleController = placedHoop.GetComponent<SimpleARBasketball>();
                uiController.simpleBasketballController = simpleController;
                Debug.Log("Connected Simple Basketball Controller");
            }
            else
            {
                MixamoBasketballController mixamoController = placedHoop.GetComponent<MixamoBasketballController>();
                uiController.basketballController = mixamoController;
                Debug.Log("Connected Mixamo Basketball Controller");
            }
            
            uiController.UpdateUI();
        }
        
        Debug.Log("Basketball hoop placed facing camera at: " + placementPosition);
    }
}
}