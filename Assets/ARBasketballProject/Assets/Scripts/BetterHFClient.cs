using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class HuggingFaceRequest
{
    public string inputs;
    public HuggingFaceParameters parameters;
    
    public HuggingFaceRequest(string question)
    {
        inputs = question;
        parameters = new HuggingFaceParameters();
    }
}

[System.Serializable]
public class HuggingFaceParameters
{
    public int max_length = 150;
    public float temperature = 0.7f;
    public int max_new_tokens = 50;
    public bool return_full_text = false;
}

[System.Serializable]
public class HuggingFaceResponse
{
    public string generated_text;
}

public class BetterHFClient
{
    private string apiKey;
    private string modelUrl;
    
    public BetterHFClient(string key, string model)
    {
        apiKey = key;
        modelUrl = model;
    }
    
    public async Task<string> GetAnswerAsync(string question)
    {
        try
        {
            // Format question for better conversation
            string formattedQuestion = $"Human: {question}\nAssistant:";
            
            var request = new HuggingFaceRequest(formattedQuestion);
            string jsonData = JsonUtility.ToJson(request);
            
            using (UnityWebRequest webRequest = new UnityWebRequest(modelUrl, "POST"))
            {
                // Set headers
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                // Set body
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                
                // Send request
                var operation = webRequest.SendWebRequest();
                
                // Wait for completion
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseText = webRequest.downloadHandler.text;
                    Debug.Log($"HF Response: {responseText}");
                    
                    // Parse response
                    if (responseText.StartsWith("["))
                    {
                        // Array response format
                        var responses = JsonHelper.FromJson<HuggingFaceResponse>(responseText);
                        if (responses.Length > 0)
                        {
                            return CleanResponse(responses[0].generated_text);
                        }
                    }
                    else
                    {
                        // Single object response
                        var response = JsonUtility.FromJson<HuggingFaceResponse>(responseText);
                        return CleanResponse(response.generated_text);
                    }
                }
                else
                {
                    Debug.LogError($"HF API Error: {webRequest.error}");
                    Debug.LogError($"Response Code: {webRequest.responseCode}");
                    Debug.LogError($"Response: {webRequest.downloadHandler.text}");
                    return GetFallbackResponse(question);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"HF Exception: {ex.Message}");
            return GetFallbackResponse(question);
        }
        
        return "I'm having trouble thinking right now. Could you try asking again?";
    }
    
    private string CleanResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "I'm not sure how to respond to that.";
        
        // Remove the original question from response
        if (response.Contains("Assistant:"))
        {
            var parts = response.Split(new[] { "Assistant:" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                response = parts[1].Trim();
            }
        }
        
        // Clean up common issues
        response = response.Replace("Human:", "").Trim();
        
        // Truncate if too long
        if (response.Length > 200)
        {
            response = response.Substring(0, 200) + "...";
        }
        
        return response;
    }
    
    private string GetFallbackResponse(string question)
    {
        string[] fallbacks = {
            "That's an interesting question! I'm still learning about that topic.",
            "I'd love to help you with that. Can you tell me more?",
            "That's something I'm still thinking about. What do you think?",
            "Great question! I'm processing that information.",
            "I find that topic fascinating. What's your perspective on it?"
        };
        
        return fallbacks[UnityEngine.Random.Range(0, fallbacks.Length)];
    }
}

// Helper class for JSON array parsing
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }
    
    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}