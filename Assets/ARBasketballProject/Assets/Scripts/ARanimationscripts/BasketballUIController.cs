using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class BasketballUIController : MonoBehaviour
{
    [Header("UI References")]
    public Button animationToggleButton;
    public Button physicsToggleButton;
    public Button manualShootButton;
    public TextMeshProUGUI animationButtonText;
    public TextMeshProUGUI physicsButtonText;

    [Header("Basketball Controllers")]
    public SimpleARBasketball simpleBasketballController;
    public MixamoBasketballController basketballController;

    [Header("Settings")]
    public bool isWithoutAvatar = false; // Flag to determine controller type

    [Header("Button Colors")]
    public Color activeColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color shootButtonColor = new Color(1f, 0.5f, 0.2f, 1f);

    void Start()
    {
        // Set up button listeners
        animationToggleButton.onClick.AddListener(OnAnimationToggle);
        physicsToggleButton.onClick.AddListener(OnPhysicsToggle);
        manualShootButton.onClick.AddListener(OnManualShoot);

        // Set shoot button color
        manualShootButton.image.color = shootButtonColor;

        // Initial UI state
        UpdateUI();

        // Initialize DOTween
        DOTween.Init();
    }

    public void OnAnimationToggle()
    {
        AnimateButtonPress(animationToggleButton);

        if (isWithoutAvatar && simpleBasketballController != null)
        {
            simpleBasketballController.ToggleAnimation();
        }
        else if (!isWithoutAvatar && basketballController != null)
        {
            basketballController.ToggleAnimation();
        }

        UpdateUI();
    }

    public void OnPhysicsToggle()
    {
        AnimateButtonPress(physicsToggleButton);

        if (isWithoutAvatar && simpleBasketballController != null)
        {
            simpleBasketballController.TogglePhysics();
        }
        else if (!isWithoutAvatar && basketballController != null)
        {
            basketballController.TogglePhysics();
        }

        UpdateUI();
    }

    public void OnManualShoot()
    {
        AnimateButtonPress(manualShootButton);

        if (isWithoutAvatar && simpleBasketballController != null)
        {
            simpleBasketballController.ShootBall();
        }
        else if (!isWithoutAvatar && basketballController != null)
        {
            basketballController.ManualShoot();
        }
    }

    void AnimateButtonPress(Button button)
    {
        button.transform.DOKill();
        button.transform.DOScale(0.95f, 0.1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                button.transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
            });
    }

    public void UpdateUI()
    {
        bool animationOn = false;
        bool physicsOn = false;

        if (isWithoutAvatar && simpleBasketballController != null)
        {
            animationOn = simpleBasketballController.autoShootEnabled;
            physicsOn = simpleBasketballController.physicsEnabled;
        }
        else if (!isWithoutAvatar && basketballController != null)
        {
            animationOn = basketballController.autoShootEnabled;
            physicsOn = basketballController.physicsEnabled;
        }

        // Update Animation Button
        animationButtonText.text = "Animation: " + (animationOn ? "ON" : "OFF");
        animationToggleButton.image.DOColor(animationOn ? activeColor : inactiveColor, 0.2f);

        // Update Physics Button  
        physicsButtonText.text = "Physics: " + (physicsOn ? "ON" : "OFF");
        physicsToggleButton.image.DOColor(physicsOn ? activeColor : inactiveColor, 0.2f);
    }
    
    public void BackButtonPressed()
    {
        SceneManager.LoadScene(0);
    }
}