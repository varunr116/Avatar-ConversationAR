using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class MixamoBasketballController : MonoBehaviour
{
    [Header("Mixamo Avatar")]
    public Transform mixamoCharacter;
    public Animation mixamoAnimation; // Changed from Animator to Animation
    public Transform rightHandBone;

    [Header("Animation Clips")]
    public AnimationClip idleClip;
    public AnimationClip walkingClip;
    public AnimationClip pickupClip;
    public AnimationClip throwingClip;
    public AnimationClip celebrateClip;
    public AnimationClip disappointedClip;

    [Header("Basketball Components")]
    public GameObject basketball;
    public Rigidbody ballRigidbody;

    [Header("Positions")]
    public Transform ballStartPosition;
    public Transform basketTarget;
    public Transform ballWalkTarget;

    [Header("Settings")]
    public bool autoShootEnabled = true;
    public bool physicsEnabled = true;
    public float shootInterval = 10f;
    public float moveSpeed = 1.5f;

    [Header("Debug")]
    public bool enableKeyboardTesting = true;

    private bool isPerformingSequence = false;
    private Vector3 originalCharacterPosition;
    private bool isWalking = false;

    void Start()
    {
        // Auto-assign components
        if (mixamoCharacter == null)
            mixamoCharacter = FindMixamoCharacter();

        if (mixamoAnimation == null)
            mixamoAnimation = mixamoCharacter.GetComponent<Animation>();

        if (mixamoAnimation == null)
        {
            Debug.LogError("Animation component not found! Please add Animation component to Mixamo character.");
            return;
        }

        if (ballRigidbody == null)
            ballRigidbody = basketball.GetComponent<Rigidbody>();

        if (rightHandBone == null)
            rightHandBone = FindBoneByName(mixamoCharacter, "RightHand");

        originalCharacterPosition = mixamoCharacter.position;

        // Set up animation clips
        SetupAnimationClips();

        ResetBallPosition();
        PlayAnimation("Idle");

        Debug.Log("MixamoBasketballController initialized with Animation component");

        if (autoShootEnabled)
        {
            StartCoroutine(MixamoSequenceLoop());
        }
    }

    void SetupAnimationClips()
    {
        // Add all clips to the Animation component
        if (idleClip != null)
        {
            mixamoAnimation.AddClip(idleClip, "Idle");
            mixamoAnimation.clip = idleClip; // Set default
        }

        if (walkingClip != null)
            mixamoAnimation.AddClip(walkingClip, "walking");

        if (pickupClip != null)
            mixamoAnimation.AddClip(pickupClip, "pickup");

        if (throwingClip != null)
            mixamoAnimation.AddClip(throwingClip, "throwing");

        if (celebrateClip != null)
            mixamoAnimation.AddClip(celebrateClip, "celebrate");

        if (disappointedClip != null)
            mixamoAnimation.AddClip(disappointedClip, "disappointed");

        Debug.Log("Animation clips set up successfully");
    }

    void Update()
    {
        // Debug keyboard controls
        if (enableKeyboardTesting)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                Debug.Log("Testing walking animation");
                if (isWalking)
                {
                    PlayAnimation("Idle");
                    isWalking = false;
                }
                else
                {
                    PlayAnimation("walking");
                    isWalking = true;
                }
            }

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                Debug.Log("Testing pickup animation");
                PlayAnimation("pickup");
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                Debug.Log("Testing throw animation");
                PlayAnimation("throwing");
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                Debug.Log("Testing celebrate animation");
                PlayAnimation("celebrate");
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                Debug.Log("Testing disappointed animation");
                PlayAnimation("disappointed");
            }

            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                Debug.Log("Back to Idle");
                PlayAnimation("Idle");
                isWalking = false;
            }
        }
    }

    void PlayAnimation(string animationName)
    {
        if (mixamoAnimation == null) return;

        try
        {
            if (mixamoAnimation[animationName] != null)
            {
                mixamoAnimation.CrossFade(animationName, 0.2f);
                Debug.Log("Playing animation: " + animationName);
            }
            else
            {
                Debug.LogWarning("Animation not found: " + animationName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error playing animation " + animationName + ": " + e.Message);
        }
    }

    bool IsAnimationPlaying(string animationName)
    {
        return mixamoAnimation.IsPlaying(animationName);
    }

    public void ToggleAnimation()
    {
        autoShootEnabled = !autoShootEnabled;
        Debug.Log("Mixamo Animation: " + (autoShootEnabled ? "ON" : "OFF"));

        if (autoShootEnabled && !isPerformingSequence)
        {
            StartCoroutine(MixamoSequenceLoop());
        }
        else
        {
            PlayAnimation("Idle");
            isWalking = false;
        }
    }

    public void TogglePhysics()
    {
        physicsEnabled = !physicsEnabled;
        ballRigidbody.useGravity = physicsEnabled;
        ballRigidbody.isKinematic = !physicsEnabled;
        Debug.Log("Physics: " + (physicsEnabled ? "ON" : "OFF"));
    }

    public void ManualShoot()
    {
       if (!isPerformingSequence)
    {
        StartCoroutine(MixamoShootSequence());
    }
    else
    {
        Debug.Log("Sequence already in progress, cannot manual shoot");
    }
    }

    IEnumerator MixamoSequenceLoop()
    {
        while (autoShootEnabled)
        {
            if (!isPerformingSequence)
            {
                yield return StartCoroutine(MixamoShootSequence());
            }
            yield return new WaitForSeconds(shootInterval);
        }
    }

    IEnumerator MixamoShootSequence()
    {
        isPerformingSequence = true;
        Debug.Log("=== Starting basketball sequence with animation clips ===");

        yield return StartCoroutine(WalkToBallWithAnimation());
        yield return StartCoroutine(PickUpBallWithAnimation());
        yield return StartCoroutine(AimAtBasket());

        bool scored = false;
        yield return StartCoroutine(ThrowBallWithAnimation((success) => scored = success));
        yield return StartCoroutine(CelebrateWithAnimation(scored));
        yield return StartCoroutine(ReturnToStartWithAnimation());

        PlayAnimation("Idle");
        Debug.Log("=== Basketball sequence complete ===");
        isPerformingSequence = false;
    }

    IEnumerator WalkToBallWithAnimation()
    {
        Debug.Log("1. Walking to ball with animation");

        Vector3 startPos = mixamoCharacter.position;
        Vector3 targetPos = ballWalkTarget.position;

        // Face direction
        Vector3 direction = (targetPos - startPos).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            mixamoCharacter.rotation = Quaternion.LookRotation(direction);
        }

        // Start walking animation
        PlayAnimation("walking");

        float duration = Vector3.Distance(startPos, targetPos) / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            mixamoCharacter.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        mixamoCharacter.position = targetPos;
        PlayAnimation("Idle");

        Debug.Log("Walk complete");
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator PickUpBallWithAnimation()
    {
        Debug.Log("2. Picking up ball with animation");

        // Play pickup animation
        PlayAnimation("pickup");

        // Wait for animation to reach pickup moment
        float pickupTime = pickupClip != null ? pickupClip.length * 0.6f : 1.2f;
        yield return new WaitForSeconds(pickupTime);

        // Attach ball to hand
        if (rightHandBone != null)
        {
            basketball.transform.SetParent(rightHandBone);
            basketball.transform.localPosition = Vector3.zero;
            basketball.transform.localRotation = Quaternion.identity;
            Debug.Log("Ball attached to hand");
        }
        else
        {
            Debug.LogWarning("Right hand bone not found");
        }

        ballRigidbody.isKinematic = true;

        // Wait for animation to complete
        float remainingTime = pickupClip != null ? pickupClip.length * 0.4f : 1.0f;
        yield return new WaitForSeconds(remainingTime);

        Debug.Log("Pickup complete");
    }

    IEnumerator AimAtBasket()
    {
        Debug.Log("3. Aiming at basket");

        // Face basket
        Vector3 direction = (basketTarget.position - mixamoCharacter.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            float aimDuration = 1.5f;
            float elapsed = 0f;
            Quaternion startRotation = mixamoCharacter.rotation;

            while (elapsed < aimDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / aimDuration;

                mixamoCharacter.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                yield return null;
            }
        }

        Debug.Log("Aim complete");
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator ThrowBallWithAnimation(System.Action<bool> onComplete)
    {
        Debug.Log("4. Throwing ball with animation");

        // Play throw animation
        PlayAnimation("throwing");

        // Wait for throw moment (usually around 40% of throw animation)
        float throwTime = throwingClip != null ? throwingClip.length * 0.4f : 0.8f;
        yield return new WaitForSeconds(throwTime);

        // Release ball
        basketball.transform.SetParent(null);
        ballRigidbody.isKinematic = !physicsEnabled;

        // Apply force
        Vector3 direction = (basketTarget.position - basketball.transform.position).normalized;
        Vector3 force = direction * 1.2f + Vector3.up * 3.8f;

        if (physicsEnabled)
        {
            ballRigidbody.AddForce(force, ForceMode.Impulse);
            Debug.Log("Ball thrown with physics");
        }
        else
        {
            StartCoroutine(MoveBallToBasket());
            Debug.Log("Ball moving kinematically");
        }

        // Wait for animation to complete
        float remainingTime = throwingClip != null ? throwingClip.length * 0.6f : 1.2f;
        yield return new WaitForSeconds(remainingTime);

        // Check score
        yield return new WaitForSeconds(2f);
        float distance = Vector3.Distance(basketball.transform.position, basketTarget.position);
        bool scored = distance < 1.5f;

        Debug.Log($"Throw complete. Distance to basket: {distance}, Scored: {scored}");
        onComplete?.Invoke(scored);
    }

    IEnumerator CelebrateWithAnimation(bool scored)
    {
        Debug.Log("5. Celebration");

        if (scored)
        {
            PlayAnimation("celebrate");
            float celebrateTime = celebrateClip != null ? celebrateClip.length : 3f;
            yield return new WaitForSeconds(celebrateTime);
        }
        else
        {
            PlayAnimation("disappointed");
            float disappointedTime = disappointedClip != null ? disappointedClip.length : 3f;
            yield return new WaitForSeconds(disappointedTime);
        }

        Debug.Log("Celebration complete");
    }

    IEnumerator ReturnToStartWithAnimation()
    {
        Debug.Log("6. Returning to start");

        Vector3 startPos = mixamoCharacter.position;
        Vector3 targetPos = originalCharacterPosition;

        // Face direction
        Vector3 direction = (targetPos - startPos).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            mixamoCharacter.rotation = Quaternion.LookRotation(direction);
        }

        // Walk back
        PlayAnimation("walking");

        float duration = Vector3.Distance(startPos, targetPos) / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            mixamoCharacter.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        mixamoCharacter.position = targetPos;
        ResetBallPosition();

        Debug.Log("Return complete");
    }

    Transform FindMixamoCharacter()
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            if (child != transform && (child.GetComponent<Animation>() != null || child.GetComponent<Animator>() != null))
            {
                return child;
            }
        }
        return null;
    }

    Transform FindBoneByName(Transform parent, string boneName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains(boneName) || child.name.ToLower().Contains("righthand"))
            {
                return child;
            }
        }
        return null;
    }

    void ResetBallPosition()
    {
        basketball.transform.SetParent(null);
        basketball.transform.position = ballStartPosition.position;
        basketball.transform.rotation = ballStartPosition.rotation;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.isKinematic = true;
    }

    IEnumerator MoveBallToBasket()
    {
        Vector3 startPos = basketball.transform.position;
        Vector3 endPos = basketTarget.position;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 4f;

            basketball.transform.position = currentPos;
            yield return null;
        }
    }
    
    
}