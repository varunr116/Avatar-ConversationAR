using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Whisper.Samples;
using UnityEngine.SceneManagement;

public class ARConversationPlacer : MonoBehaviour
{
    [Header("Prefab to Place")]
    public GameObject conversationAvatarsPrefab;

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Placement Settings")]
    public PlacementMode placementMode = PlacementMode.FaceCamera;
    public float avatarScale = 1f;

    [Header("Preview")]
    public GameObject placementPreview;
    public bool showPlacementPreview = true;

    public enum PlacementMode
    {
        FaceCamera,
        ParallelCamera,
        SideView
    }

    private GameObject placedConversation;
    private GameObject previewObject;
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
        HandleTouchInput();
        UpdatePlacementPreview();
    }

    void HandleTouchInput()
    {
        Vector2 inputPosition = Vector2.zero;
        bool inputDetected = false;

        // Check for touch input
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                inputDetected = true;
            }
        }

        // Check for mouse input (editor)
#if UNITY_EDITOR
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputPosition = Mouse.current.position.ReadValue();
            inputDetected = true;
        }
#endif

        if (inputDetected && !IsPointerOverUIElement())
        {
            TryPlaceConversation(inputPosition);
        }
    }

    void UpdatePlacementPreview()
    {
        if (!showPlacementPreview) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = raycastHits[0].pose;

            // Create or update preview object
            if (previewObject == null && placementPreview != null)
            {
                previewObject = Instantiate(placementPreview);
            }

            if (previewObject != null)
            {
                previewObject.transform.position = hitPose.position;
                previewObject.transform.rotation = GetPlacementRotation(hitPose.position);
                previewObject.transform.localScale = Vector3.one * avatarScale;
                previewObject.SetActive(true);
            }
        }
        else
        {
            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }
        }
    }

    void TryPlaceConversation(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = raycastHits[0].pose;

            // Remove existing conversation
            if (placedConversation != null)
            {
                DestroyImmediate(placedConversation);
            }

            // Calculate optimal rotation
            Quaternion finalRotation = GetPlacementRotation(hitPose.position);

            // Instantiate conversation prefab
            placedConversation = Instantiate(conversationAvatarsPrefab, hitPose.position, finalRotation);
            placedConversation.transform.localScale = Vector3.one * avatarScale;

            // Initialize the conversation system
            InitializeConversationSystem();

            Debug.Log($"Conversation avatars placed at: {hitPose.position}");
        }
    }

    void InitializeConversationSystem()
    {
        if (placedConversation == null) return;

        // Get the conversation manager from the prefab
        AvatarConversationManager conversationManager = placedConversation.GetComponentInChildren<AvatarConversationManager>();

        if (conversationManager != null)
        {
            // Initialize the conversation system
            Debug.Log("Conversation system initialized successfully");
        }
        else
        {
            Debug.LogError("AvatarConversationManager not found in prefab!");
        }
    }

    Quaternion GetPlacementRotation(Vector3 placementPosition)
    {
        Vector3 cameraPosition = arCamera.transform.position;
        Vector3 cameraForward = arCamera.transform.forward;

        switch (placementMode)
        {
            case PlacementMode.FaceCamera:
                Vector3 directionToCamera = (cameraPosition - placementPosition).normalized;
                directionToCamera.y = 0;
                return Quaternion.LookRotation(directionToCamera);

            case PlacementMode.ParallelCamera:
                Vector3 parallelDirection = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                return Quaternion.LookRotation(parallelDirection);

            case PlacementMode.SideView:
                Vector3 sideDirection = Vector3.Cross(Vector3.up, cameraForward).normalized;
                return Quaternion.LookRotation(sideDirection);

            default:
                return Quaternion.identity;
        }
    }

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

    public void ChangePlacementMode(int mode)
    {
        placementMode = (PlacementMode)mode;
        Debug.Log("Placement mode changed to: " + placementMode);
    }

    public void ChangeAvatarScale(float scale)
    {
        avatarScale = Mathf.Clamp(scale, 0.1f, 3f);

        if (placedConversation != null)
        {
            placedConversation.transform.localScale = Vector3.one * avatarScale;
        }

        Debug.Log("Avatar scale changed to: " + avatarScale);
    }
     public void BackButtonPressed()
    {
        SceneManager.LoadScene(0);
    }
}