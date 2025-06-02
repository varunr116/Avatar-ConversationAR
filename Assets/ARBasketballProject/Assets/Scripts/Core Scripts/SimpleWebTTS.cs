using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleWebTTS : MonoBehaviour
{
    [Header("TTS Settings")]
    public AudioSource audioSource;
    [Range(0.5f, 2.0f)]
    public float speechSpeed = 1.0f;
    
    [Header("Voice Settings")]
    public string language = "en";
    
    // Events
    public System.Action OnSpeechStarted;
    public System.Action OnSpeechFinished;
    
    private bool isSpeaking = false;
    private Coroutine currentSpeechCoroutine;
    
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D audio
    }
    
    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrEmpty(text) || isSpeaking)
        {
            Debug.LogWarning("TTS: Cannot speak - text is empty or already speaking");
            return;
        }
        
        Debug.Log($"TTS: Starting to speak '{text}'");
        
        // Create a task completion source to convert coroutine to async
        var tcs = new TaskCompletionSource<bool>();
        
        // Start the coroutine and wait for it to complete
        currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(text, tcs));
        
        // Wait for the coroutine to complete
        await tcs.Task;
    }
    
    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text) || isSpeaking)
        {
            return;
        }
        
        // Start coroutine directly without async
        currentSpeechCoroutine = StartCoroutine(SpeakCoroutine(text, null));
    }
    
    IEnumerator SpeakCoroutine(string text, TaskCompletionSource<bool> taskSource)
    {
        isSpeaking = true;
        OnSpeechStarted?.Invoke();
        
        // Clean text for TTS
        string cleanText = CleanTextForTTS(text);
        Debug.Log($"TTS: Clean text - '{cleanText}'");
        
        bool success = false;
        
        // Try Google Translate TTS first
        yield return StartCoroutine(TryGoogleTranslateTTS(cleanText, (result) => success = result));
        
        if (!success)
        {
            Debug.LogWarning("Google TTS failed, trying backup method");
            // Try backup method
            yield return StartCoroutine(TryBackupTTS(cleanText, (result) => success = result));
        }
        
        if (!success)
        {
            Debug.LogWarning("All TTS methods failed, using silent fallback");
            // Silent fallback - just wait estimated time
            float duration = EstimateSpeechDuration(cleanText);
            yield return new WaitForSeconds(duration);
        }
        
        // Mark as complete
        isSpeaking = false;
        OnSpeechFinished?.Invoke();
        
        // Complete the task if it was async
        if (taskSource != null)
        {
            taskSource.SetResult(true);
        }
        
        Debug.Log("TTS: Speech completed");
    }
    
    IEnumerator TryGoogleTranslateTTS(string text, System.Action<bool> callback)
    {
        string url = $"https://translate.google.com/translate_tts?ie=UTF-8&tl={language}&client=tw-ob&q={UnityWebRequest.EscapeURL(text)}";
        Debug.Log($"TTS: Trying Google TTS with URL: {url}");
        
        UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        
        // Add headers to avoid blocking
        webRequest.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        webRequest.SetRequestHeader("Referer", "https://translate.google.com/");
        
        yield return webRequest.SendWebRequest();
        
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(webRequest);
            
            if (clip != null && clip.length > 0)
            {
               
                
                audioSource.clip = clip;
                audioSource.pitch = speechSpeed;
                audioSource.Play();
                
                // Wait for audio to finish
                yield return new WaitForSeconds(clip.length / speechSpeed);
                
                callback(true);
            }
            else
            {
                Debug.LogWarning("TTS: Audio clip is null or empty");
                callback(false);
            }
        }
        else
        {
            Debug.LogWarning($"TTS: Google TTS request failed - {webRequest.error}");
            callback(false);
        }
        
        webRequest.Dispose();
    }
    
    IEnumerator TryBackupTTS(string text, System.Action<bool> callback)
    {
        // Alternative TTS service (VoiceRSS demo)
        string url = $"https://api.voicerss.org/?key=demo&hl={language}-us&src={UnityWebRequest.EscapeURL(text)}&f=44khz_16bit_mono";
        Debug.Log($"TTS: Trying backup TTS");
        
        UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        
        yield return webRequest.SendWebRequest();
        
        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(webRequest);
            
            if (clip != null && clip.length > 0)
            {
                Debug.Log($"TTS: Backup TTS successful, length: {clip.length} seconds");
                
                audioSource.clip = clip;
                audioSource.pitch = speechSpeed;
                audioSource.Play();
                
                yield return new WaitForSeconds(clip.length / speechSpeed);
                
                callback(true);
            }
            else
            {
                Debug.LogWarning("TTS: Backup audio clip is null or empty");
                callback(false);
            }
        }
        else
        {
            Debug.LogWarning($"TTS: Backup TTS failed - {webRequest.error}");
            callback(false);
        }
        
        webRequest.Dispose();
    }
    
    string CleanTextForTTS(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        // Remove emojis and special characters but keep basic punctuation
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[^\w\s\.,!?;:\-']", "");
        
        // Replace multiple spaces with single space
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        
        // Limit length for TTS
        if (text.Length > 200)
        {
            text = text.Substring(0, 200).Trim();
            // Try to end at a sentence boundary
            int lastPeriod = text.LastIndexOf('.');
            int lastQuestion = text.LastIndexOf('?');
            int lastExclamation = text.LastIndexOf('!');
            
            int lastSentence = Mathf.Max(lastPeriod, Mathf.Max(lastQuestion, lastExclamation));
            if (lastSentence > text.Length - 50) // If sentence boundary is close to end
            {
                text = text.Substring(0, lastSentence + 1);
            }
        }
        
        return text.Trim();
    }
    
    float EstimateSpeechDuration(string text)
    {
        // Estimate based on average speaking rate (150 words per minute)
        float wordsPerSecond = 2.5f / speechSpeed;
        int wordCount = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return Mathf.Max(1.0f, wordCount / wordsPerSecond);
    }
    
    public void StopSpeaking()
    {
        Debug.Log("TTS: Stopping speech");
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
            currentSpeechCoroutine = null;
        }
        
        if (isSpeaking)
        {
            isSpeaking = false;
            OnSpeechFinished?.Invoke();
        }
    }
    
    public bool IsSpeaking()
    {
        return isSpeaking || (audioSource != null && audioSource.isPlaying);
    }
    
    void OnDestroy()
    {
        StopSpeaking();
    }
}