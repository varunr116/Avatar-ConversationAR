using System;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Whisper;

namespace Whisper.Samples
{
    public class StreamingSampleMic : MonoBehaviour
    {
        [Header("Whisper Setup")]
        public WhisperManager   whisper;
        public MicrophoneRecord microphoneRecord;

        [Header("UI")]
        public Button     button;         
        public Text       buttonText;     
        public Text       transcriptText; 
        public Text       answerText;     
        public ScrollRect scroll;

        [Header("Gemini Settings")]
        [Tooltip("Your Google Gemini API key")]
        public string geminiApiKey;
        
        [Header("Text-to-Speech")]
        public SimpleWebTTS ttsManager;
        public bool enableVoiceResponse = true;
        [Tooltip("Show speaking indicator")]
        public bool showSpeakingIndicator = true;

        WhisperStream _stream;
        GeminiClient _geminiClient;
        private bool isProcessingConversation = false;

        async void Start()
        {
            if (string.IsNullOrEmpty(geminiApiKey))
            {
                Debug.LogError("Gemini API key is missing!");
                answerText.text = "Error: Missing Gemini API key";
                return;
            }

            // Validate TTS
            if (ttsManager == null)
            {
                Debug.LogWarning("TTS Manager not assigned - voice responses disabled");
                enableVoiceResponse = false;
            }

            // init Whisper
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated  += OnResult;
            _stream.OnStreamFinished += OnFinished;
            microphoneRecord.OnRecordStop += _ => buttonText.text = "Record";

            // UI wiring
            button.onClick.AddListener(OnButtonPressed);
            buttonText.text  = "Record";
            transcriptText.text = "";
            answerText.text     = "";

            // init Gemini client
            _geminiClient = new GeminiClient(geminiApiKey);
            
            // Setup TTS events
            if (ttsManager != null)
            {
                ttsManager.OnSpeechStarted += OnSpeechStarted;
                ttsManager.OnSpeechFinished += OnSpeechFinished;
            }
            
            Debug.Log("Initialized with Gemini AI + SimpleWebTTS");
        }

        void OnButtonPressed()
{
    Debug.Log("=== BUTTON PRESSED ===");
    Debug.Log($"Is Processing: {isProcessingConversation}");
    Debug.Log($"Is Recording: {(microphoneRecord != null ? microphoneRecord.IsRecording.ToString() : "microphoneRecord is null")}");
    Debug.Log($"TTS Speaking: {(ttsManager != null ? ttsManager.IsSpeaking().ToString() : "ttsManager is null")}");

    // Prevent new recording while processing
    if (isProcessingConversation)
    {
        Debug.Log("Still processing previous conversation...");
        return;
    }

    // Stop any ongoing speech
    if (ttsManager != null && ttsManager.IsSpeaking())
    {
        Debug.Log("Stopping current TTS");
        ttsManager.StopSpeaking();
    }

    if (!microphoneRecord.IsRecording)
    {
        Debug.Log("Starting recording...");
        transcriptText.text = "";
        answerText.text = "";
        
        try
        {
            _stream.StartStream();
            microphoneRecord.StartRecord();
            buttonText.text = "Stop";
            Debug.Log("Recording started successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error starting recording: {ex.Message}");
        }
    }
    else
    {
        Debug.Log("Stopping recording...");
        try
        {
            microphoneRecord.StopRecord();
            buttonText.text = "Record";
            Debug.Log("Recording stopped successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error stopping recording: {ex.Message}");
        }
    }
}

        void OnResult(string partial)
        {
            transcriptText.text = partial;
            ScrollToBottom();
        }

        async void OnFinished(string finalResult)
        {
            isProcessingConversation = true;
            
            transcriptText.text = finalResult;
            UiUtils.ScrollDown(scroll);

            if (string.IsNullOrEmpty(finalResult.Trim()))
            {
                answerText.text = "I didn't catch that. Could you try again?";
                isProcessingConversation = false;
                return;
            }

            answerText.text = "Thinking…";
            
            try
            {
                Debug.Log($"Sending to Gemini: {finalResult}");
                var reply = await _geminiClient.GetAnswerAsync(finalResult);
                
                // Update UI with response
                answerText.text = reply;
                Debug.Log($"Gemini Reply: {reply}");
                
                // Speak the response
                if (enableVoiceResponse && ttsManager != null && !string.IsNullOrEmpty(reply))
                {
                    Debug.Log("Starting TTS...");
                    await ttsManager.SpeakAsync(reply);
                    Debug.Log("TTS completed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Conversation Error: {ex.Message}");
                string errorMsg = "I'm having trouble right now. Could you try again?";
                answerText.text = errorMsg;
                
                // Speak error message too
                if (enableVoiceResponse && ttsManager != null)
                {
                    await ttsManager.SpeakAsync(errorMsg);
                }
            }
            finally
            {
                isProcessingConversation = false;
                UiUtils.ScrollDown(scroll);
            }
        }

        void OnSpeechStarted()
        {
            Debug.Log("🔊 Avatar started speaking");
            
            if (showSpeakingIndicator)
            {
                // Add speaking indicator to UI
                answerText.text += " 🔊";
            }
            
            // TODO: Trigger avatar speaking animation here
            // Example: avatarAnimator.SetBool("isSpeaking", true);
        }

        void OnSpeechFinished()
        {
            Debug.Log("🔇 Avatar finished speaking");
            
            if (showSpeakingIndicator)
            {
                // Remove speaking indicator
                string text = answerText.text;
                if (text.EndsWith(" 🔊"))
                {
                    answerText.text = text.Substring(0, text.Length - 2);
                }
            }
            
            // TODO: Stop avatar speaking animation here
            // Example: avatarAnimator.SetBool("isSpeaking", false);
        }

        void ScrollToBottom()
        {
            if (scroll != null)
                scroll.verticalNormalizedPosition = 0f;
        }

        void OnDestroy()
        {
            // Cleanup TTS events
            if (ttsManager != null)
            {
                ttsManager.OnSpeechStarted -= OnSpeechStarted;
                ttsManager.OnSpeechFinished -= OnSpeechFinished;
            }
        }

        // Public methods for external control
        public void ToggleVoiceResponse()
        {
            enableVoiceResponse = !enableVoiceResponse;
            Debug.Log($"Voice response: {(enableVoiceResponse ? "Enabled" : "Disabled")}");
        }

        public void StopCurrentSpeech()
        {
            if (ttsManager != null)
            {
                ttsManager.StopSpeaking();
            }
        }

        public bool IsAvatarSpeaking()
        {
            return ttsManager != null && ttsManager.IsSpeaking();
        }
    }
}