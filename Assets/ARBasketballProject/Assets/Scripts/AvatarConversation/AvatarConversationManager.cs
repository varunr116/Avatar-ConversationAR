using System;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Whisper;
using System.Collections;
using TMPro;

namespace Whisper.Samples
{
    public class AvatarConversationManager : MonoBehaviour
    {
        [Header("Whisper Setup")]
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;

        [Header("UI - World Space Canvas")]
        public Button button;         
        public TMP_Text buttonText;     
        public Text transcriptText; 
        public Text answerText;     
        public ScrollRect scroll;

        [Header("Gemini Settings")]
       
        public string geminiApiKey = "YOUR_GEMINI_API_KEY";
        
        [Header("Text-to-Speech")]
        public SimpleWebTTS ttsManager;
        public bool enableVoiceResponse = true;

        [Header("Avatar Setup - Animation Components")]
        public Animation maleAnimation;      // Male avatar Animation component
        public Animation femaleAnimation;    // Female avatar Animation component
        
        [Header("Animation Clips")]
        public AnimationClip maleTalkingClip;
        public AnimationClip maleIdleClip;
        public AnimationClip femaleTalkingClip;
        public AnimationClip femaleIdleClip;
        
        [Header("AR Specific")]
        public bool autoFindComponents = true;
        
        // Private variables
        private WhisperStream _stream;
        private GeminiClient _geminiClient;
        private bool isProcessingConversation = false;
        private bool isMaleTalking = false;
        private bool isFemaleTalking = false;
        private bool isInitialized = false;

        void Start()
        {
            StartCoroutine(InitializeWithDelay());
        }
        
        IEnumerator InitializeWithDelay()
        {
            // Wait a frame for prefab to be fully instantiated
            yield return null;
            
            if (autoFindComponents)
            {
                AutoFindComponents();
            }
            
            SetupAnimationClips();
            InitializeSystemAsync();
        }
        
        void AutoFindComponents()
        {
            // Auto-find components if not assigned
            if (whisper == null)
                whisper = FindObjectOfType<WhisperManager>();
            
            if (microphoneRecord == null)
                microphoneRecord = FindObjectOfType<MicrophoneRecord>();
            
            if (ttsManager == null)
                ttsManager = FindObjectOfType<SimpleWebTTS>();
            
            // Find Animation components if not assigned
            if (maleAnimation == null || femaleAnimation == null)
            {
                Animation[] animations = GetComponentsInChildren<Animation>();
                if (animations.Length >= 2)
                {
                    maleAnimation = animations[0];
                    femaleAnimation = animations[1];
                    Debug.Log($"Auto-assigned Male Animation: {maleAnimation.name}, Female Animation: {femaleAnimation.name}");
                }
            }
            
            // Find UI components
            if (button == null)
                button = GetComponentInChildren<Button>();
            
            if (buttonText == null && button != null)
                buttonText = button.GetComponentInChildren<TMP_Text>();
            
            Debug.Log("Auto-component discovery completed");
        }

        void SetupAnimationClips()
        {
            // Setup male animation clips
            if (maleAnimation != null)
            {
                if (maleTalkingClip != null)
                {
                    maleAnimation.AddClip(maleTalkingClip, "MaleTalking");
                    Debug.Log("Added male talking clip");
                }
                
                if (maleIdleClip != null)
                {
                    maleAnimation.AddClip(maleIdleClip, "MaleIdle");
                    maleAnimation.clip = maleIdleClip; // Set default
                    Debug.Log("Added male idle clip");
                }
            }
            
            // Setup female animation clips
            if (femaleAnimation != null)
            {
                if (femaleTalkingClip != null)
                {
                    femaleAnimation.AddClip(femaleTalkingClip, "FemaleTalking");
                    Debug.Log("Added female talking clip");
                }
                
                if (femaleIdleClip != null)
                {
                    femaleAnimation.AddClip(femaleIdleClip, "FemaleIdle");
                    femaleAnimation.clip = femaleIdleClip; // Set default
                    Debug.Log("Added female idle clip");
                }
            }
        }

        async void InitializeSystemAsync()
        {
            try
            {
                // Validate components
                if (string.IsNullOrEmpty(geminiApiKey) || geminiApiKey == "YOUR_GEMINI_API_KEY")
                {
                    Debug.LogError("Gemini API key is missing!");
                    if (answerText != null)
                        answerText.text = "Error: Missing Gemini API key";
                    return;
                }

                if (maleAnimation == null || femaleAnimation == null)
                {
                    Debug.LogError("Both male and female Animation components must be assigned!");
                    return;
                }

                if (whisper == null)
                {
                    Debug.LogError("WhisperManager not found!");
                    return;
                }

                // Initialize Whisper
                _stream = await whisper.CreateStream(microphoneRecord);
                _stream.OnResultUpdated += OnResult;
                _stream.OnStreamFinished += OnFinished;
                microphoneRecord.OnRecordStop += _ => OnRecordingStopped();

                // Initialize UI
                if (button != null)
                    button.onClick.AddListener(OnButtonPressed);
                
                if (buttonText != null)
                    buttonText.text = "Record";
                
                if (transcriptText != null)
                    transcriptText.text = "";
                
                if (answerText != null)
                    answerText.text = "Ready to chat!";

                // Initialize Gemini client
                _geminiClient = new GeminiClient(geminiApiKey);
                
                // Setup TTS events
                if (ttsManager != null)
                {
                    ttsManager.OnSpeechStarted += OnSpeechStarted;
                    ttsManager.OnSpeechFinished += OnSpeechFinished;
                }

                // Set avatars to idle
                PlayMaleAnimation("MaleIdle", true);
               // PlayFemaleAnimation("FemaleIdle", true);
                
                isInitialized = true;
                Debug.Log("AR Avatar Conversation System initialized successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing conversation system: {ex.Message}");
                if (answerText != null)
                    answerText.text = "Initialization failed";
            }
        }

        void OnButtonPressed()
        {
            if (!isInitialized)
            {
                Debug.Log("System not initialized yet...");
                return;
            }

            Debug.Log("=== AR CONVERSATION BUTTON PRESSED ===");

            if (isProcessingConversation)
            {
                Debug.Log("Still processing previous conversation...");
                return;
            }

            // Stop any ongoing speech
            if (ttsManager != null && ttsManager.IsSpeaking())
            {
                Debug.Log("Stopping current TTS and female avatar");
                ttsManager.StopSpeaking();
                StopFemaleTalking();
            }

            if (!microphoneRecord.IsRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        void StartRecording()
        {
            Debug.Log("Starting AR recording - Male avatar begins talking");
            
            // Clear previous text
            if (transcriptText != null)
                transcriptText.text = "";
            if (answerText != null)
                answerText.text = "";
            
            try
            {
                // Start recording
                _stream.StartStream();
                microphoneRecord.StartRecord();
                
                // Update UI
                if (buttonText != null)
                    buttonText.text = "Stop Recording";
                
                // Start male avatar talking animation
                StartMaleTalking();
                
                Debug.Log("AR Recording started successfully - Male avatar is now talking");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error starting AR recording: {ex.Message}");
                StopMaleTalking();
            }
        }

        void StopRecording()
        {
            Debug.Log("Stopping AR recording - Male avatar stops talking");
            
            try
            {
                microphoneRecord.StopRecord();
                if (buttonText != null)
                    buttonText.text = "Record";
                
                // Stop male avatar talking animation
                StopMaleTalking();
                
                Debug.Log("AR Recording stopped successfully - Male avatar returned to idle");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error stopping AR recording: {ex.Message}");
                StopMaleTalking();
            }
        }

        void OnRecordingStopped()
        {
            StopMaleTalking();
            if (buttonText != null)
                buttonText.text = "Record";
        }

        void OnResult(string partial)
        {
            if (transcriptText != null)
                transcriptText.text = partial;
            ScrollToBottom();
        }

        async void OnFinished(string finalResult)
        {
            isProcessingConversation = true;
            
            // Ensure male avatar has stopped talking
            StopMaleTalking();
            
            if (transcriptText != null)
                transcriptText.text = finalResult;
            
            if (scroll != null)
                UiUtils.ScrollDown(scroll);

            if (string.IsNullOrEmpty(finalResult.Trim()))
            {
                if (answerText != null)
                    answerText.text = "I didn't catch that. Could you try again?";
                isProcessingConversation = false;
                return;
            }

            if (answerText != null)
                answerText.text = "Thinking…";
            
            try
            {
                Debug.Log($"Sending to Gemini: {finalResult}");
                var reply = await _geminiClient.GetAnswerAsync(finalResult);
                
                // Update UI with response
                if (answerText != null)
                    answerText.text = reply;
                Debug.Log($"Gemini Reply: {reply}");
                
                // Start female avatar speaking
                if (enableVoiceResponse && ttsManager != null && !string.IsNullOrEmpty(reply))
                {
                    Debug.Log("Starting AR female avatar speech...");
                    StartFemaleTalking();
                    await ttsManager.SpeakAsync(reply);
                    StopFemaleTalking();
                    Debug.Log("AR Female avatar finished speaking");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"AR Conversation Error: {ex.Message}");
                string errorMsg = "I'm having trouble right now. Could you try again?";
                if (answerText != null)
                    answerText.text = errorMsg;
                
                // Female avatar speaks error message
                if (enableVoiceResponse && ttsManager != null)
                {
                    StartFemaleTalking();
                    await ttsManager.SpeakAsync(errorMsg);
                    StopFemaleTalking();
                }
            }
            finally
            {
                isProcessingConversation = false;
                if (scroll != null)
                    UiUtils.ScrollDown(scroll);
            }
        }

        // Male avatar animation methods
        void StartMaleTalking()
        {
            isMaleTalking = true;
            PlayMaleAnimation("MaleTalking", true);
            Debug.Log("Male avatar started talking");
        }

        void StopMaleTalking()
        {
            isMaleTalking = false;
            PlayMaleAnimation("MaleIdle", true);
            Debug.Log("Male avatar stopped talking");
        }

        void PlayMaleAnimation(string animationName, bool loop)
        {
            if (maleAnimation == null) return;
            
            try
            {
                if (maleAnimation[animationName] != null)
                {
                    if (loop)
                    {
                        maleAnimation[animationName].wrapMode = WrapMode.Loop;
                    }
                    else
                    {
                        maleAnimation[animationName].wrapMode = WrapMode.Once;
                    }
                    
                    maleAnimation.CrossFade(animationName, 0.3f);
                    Debug.Log($"Playing male animation: {animationName}");
                }
                else
                {
                    Debug.LogWarning($"Male animation '{animationName}' not found!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing male animation: {ex.Message}");
            }
        }

        // Female avatar animation methods
        void StartFemaleTalking()
        {
            isFemaleTalking = true;
            PlayFemaleAnimation("FemaleTalking", true);
            Debug.Log("Female avatar started talking");
        }

        void StopFemaleTalking()
        {
            isFemaleTalking = false;
            PlayFemaleAnimation("FemaleIdle", true);
            Debug.Log("Female avatar stopped talking");
        }

        void PlayFemaleAnimation(string animationName, bool loop)
        {
            if (femaleAnimation == null) return;
            
            try
            {
                if (femaleAnimation[animationName] != null)
                {
                    if (loop)
                    {
                        femaleAnimation[animationName].wrapMode = WrapMode.Loop;
                    }
                    else
                    {
                        femaleAnimation[animationName].wrapMode = WrapMode.Once;
                    }
                    
                    femaleAnimation.CrossFade(animationName, 0.3f);
                    Debug.Log($"Playing female animation: {animationName}");
                }
                else
                {
                    Debug.LogWarning($"Female animation '{animationName}' not found!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing female animation: {ex.Message}");
            }
        }

        void OnSpeechStarted()
        {
            Debug.Log("🔊 Female avatar TTS started");
        }

        void OnSpeechFinished()
        {
            Debug.Log("🔇 Female avatar TTS finished");
            StopFemaleTalking();
        }

        void ScrollToBottom()
        {
            if (scroll != null)
                scroll.verticalNormalizedPosition = 0f;
        }

        void OnDestroy()
        {
            // Cleanup events
            if (ttsManager != null)
            {
                ttsManager.OnSpeechStarted -= OnSpeechStarted;
                ttsManager.OnSpeechFinished -= OnSpeechFinished;
            }
        }

        // Public testing methods
        public void TestMaleAvatar()
        {
            if (isInitialized)
                StartCoroutine(TestMaleAnimation());
        }

        public void TestFemaleAvatar()
        {
            if (isInitialized)
                StartCoroutine(TestFemaleAnimation());
        }

        IEnumerator TestMaleAnimation()
        {
            Debug.Log("Testing male avatar talking animation");
            StartMaleTalking();
            yield return new WaitForSeconds(3f);
            StopMaleTalking();
            Debug.Log("Male avatar test complete");
        }

        IEnumerator TestFemaleAnimation()
        {
            Debug.Log("Testing female avatar talking animation");
            StartFemaleTalking();
            yield return new WaitForSeconds(3f);
            StopFemaleTalking();
            Debug.Log("Female avatar test complete");
        }
    }
}