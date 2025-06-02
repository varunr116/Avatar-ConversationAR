using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
public class MenuController : MonoBehaviour
{

    [Header("UI Elements")]
    public Button playBasketballButton;
    public Button playBasketballAvatarButton;
    public Button talkToAvatarButton;
    //public Button basketballHoopMenu;

    void Start()
    {
playBasketballButton.onClick.AddListener(ChangetoBasketball);
        playBasketballAvatarButton.onClick.AddListener(ChangetoBasketballAvatar);
        talkToAvatarButton.onClick.AddListener(ChangetoTalktoAvatar);
        
    }
    private void ChangeScene(int index)
    {
        SceneManager.LoadScene(index);
    }
    private void ChangetoBasketball()
    {
        ChangeScene(1);
    }
    private void ChangetoBasketballAvatar()
    {
        ChangeScene(2);
    }
    private void ChangetoTalktoAvatar()
    {
        ChangeScene(3);
    }
   
}
