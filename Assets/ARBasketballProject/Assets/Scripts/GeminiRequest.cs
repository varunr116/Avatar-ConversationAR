using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class GeminiRequest
{
    public GeminiContent[] contents;
    
    public GeminiRequest(string message)
    {
        contents = new GeminiContent[]
        {
            new GeminiContent
            {
                parts = new GeminiPart[]
                {
                    new GeminiPart { text = message }
                }
            }
        };
    }
}

[System.Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
}

[System.Serializable]
public class GeminiPart
{
    public string text;
}

[System.Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
}

[System.Serializable]
public class GeminiCandidate
{
    public GeminiContent content;
    public string finishReason;
}

[System.Serializable]
public class GeminiError
{
    public GeminiErrorDetail error;
}

[System.Serializable]
public class GeminiErrorDetail
{
    public string message;
    public int code;
}

public class GeminiClient
{
    private string apiKey;
    // Updated API URL with correct model name
    private string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
    
    public GeminiClient(string key)
    {
        apiKey = key;
    }
    
    public async Task<string> GetAnswerAsync(string question)
    {
        try
        {
            // Simple conversational prompt - don't hardcode user name
            string conversationalPrompt = $"{question}";
            
            var request = new GeminiRequest(conversationalPrompt);
            string jsonData = JsonUtility.ToJson(request);
            
            string fullUrl = $"{baseUrl}?key={apiKey}";
            
            Debug.Log($"Sending Gemini request: {jsonData}");
            Debug.Log($"Using URL: {fullUrl.Replace(apiKey, "***")}"); // Hide API key in logs
            
            using (UnityWebRequest webRequest = new UnityWebRequest(fullUrl, "POST"))
            {
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                
                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                Debug.Log($"Gemini Response Code: {webRequest.responseCode}");
                Debug.Log($"Gemini Response: {webRequest.downloadHandler.text}");
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<GeminiResponse>(webRequest.downloadHandler.text);
                    
                    if (response.candidates != null && response.candidates.Length > 0)
                    {
                        var candidate = response.candidates[0];
                        if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                        {
                            return CleanResponse(candidate.content.parts[0].text);
                        }
                    }
                }
                else
                {
                    // Try to parse error
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<GeminiError>(webRequest.downloadHandler.text);
                        Debug.LogError($"Gemini API Error: {errorResponse.error.message}");
                        
                        // If it's a model not found error, suggest alternative
                        if (errorResponse.error.code == 404)
                        {
                            Debug.LogError("Try using 'gemini-1.5-flash' or 'gemini-1.5-pro' instead");
                        }
                    }
                    catch
                    {
                        Debug.LogError($"Gemini Error: {webRequest.error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Gemini Exception: {ex.Message}");
        }
        
        return GetFallbackResponse(question);
    }
    
    private string CleanResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "I'm having trouble thinking right now.";
        
        // Remove excessive whitespace
        response = response.Trim();
        
        // Limit length for speech
        if (response.Length > 200)
        {
            response = response.Substring(0, 200) + "...";
        }
        
        return response;
    }
    
    private string GetFallbackResponse(string question)
    {
        string lowerQuestion = question.ToLower();
        
        if (lowerQuestion.Contains("name"))
        {
            return "Hi there! I'm your friendly AR assistant. Nice to meet you!";
        }
        else if (lowerQuestion.Contains("hello") || lowerQuestion.Contains("hi"))
        {
            return "Hello! Great to see you in the AR world!";
        }
        else if (lowerQuestion.Contains("how are you"))
        {
            return "I'm doing fantastic! How are you doing today?";
        }
        else
        {
            return "That's an interesting question! I'm still learning about that topic.";
        }
    }
}