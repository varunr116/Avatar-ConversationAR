using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SimpleARBasketball : MonoBehaviour 
{
    [Header("Game Objects")]
    public GameObject basketball;
    public Transform ballStartPosition;
    public Transform basketTarget;
    
    [Header("Animation Control")]
    public bool autoShootEnabled = true;
    public float shootInterval = 4f;
    
    [Header("Physics Control")]
    public Rigidbody ballRigidbody;
    public bool physicsEnabled = true;
    
    [Header("Shooting Settings")]
    public float shootForce = 5f;
    public float upwardForce = 8f;
    
    private Coroutine autoShootCoroutine;
    private Vector3 originalBallPosition;
    
    void Start() 
    {
        if (ballRigidbody == null)
            ballRigidbody = basketball.GetComponent<Rigidbody>();
        
        originalBallPosition = ballStartPosition.position;
        
        if (autoShootEnabled)
        {
            StartAutoShooting();
        }
    }
    
    // void Update()
    // {
    //     // Fixed keyboard controls for testing
    //     if (Keyboard.current.aKey.wasPressedThisFrame)
    //         ToggleAnimation();
        
    //     if (Keyboard.current.pKey.wasPressedThisFrame)
    //         TogglePhysics();
        
    //     if (Keyboard.current.spaceKey.wasPressedThisFrame)
    //         ShootBall();
    // }
    
    public void ToggleAnimation() 
    {
        autoShootEnabled = !autoShootEnabled;
        
        if (autoShootEnabled) 
        {
            StartAutoShooting();
        } 
        else 
        {
            StopAutoShooting();
            ResetBallPosition();
        }
        
        Debug.Log("Animation: " + (autoShootEnabled ? "ON" : "OFF"));
    }
    
    public void TogglePhysics() 
    {
        physicsEnabled = !physicsEnabled;
        ballRigidbody.useGravity = physicsEnabled;
        ballRigidbody.isKinematic = !physicsEnabled;
        
        Debug.Log("Physics: " + (physicsEnabled ? "ON" : "OFF"));
    }
    
    void StartAutoShooting() 
    {
        if (autoShootCoroutine != null) 
            StopCoroutine(autoShootCoroutine);
        autoShootCoroutine = StartCoroutine(AutoShootLoop());
    }
    
    void StopAutoShooting() 
    {
        if (autoShootCoroutine != null) 
            StopCoroutine(autoShootCoroutine);
    }
    
    IEnumerator AutoShootLoop() 
    {
        while (autoShootEnabled) 
        {
            yield return new WaitForSeconds(shootInterval);
            ShootBall();
        }
    }
    
   public void ShootBall() 
    {
        ResetBallPosition();
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        
        Vector3 direction = (basketTarget.position - ballStartPosition.position).normalized;
        Vector3 shootDirection = direction * shootForce + Vector3.up * upwardForce;
        
        if (physicsEnabled)
        {
            ballRigidbody.AddForce(shootDirection, ForceMode.Impulse);
        }
        else
        {
            StartCoroutine(MoveToTarget());
        }
    }
    
    void ResetBallPosition()
    {
        basketball.transform.position = ballStartPosition.position;
        basketball.transform.rotation = ballStartPosition.rotation;
    }
    
    IEnumerator MoveToTarget()
    {
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startPos = ballStartPosition.position;
        Vector3 endPos = basketTarget.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 2f;
            
            basketball.transform.position = currentPos;
            yield return null;
        }
    }
}