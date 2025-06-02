using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleHF
{
    const string BASE_URL = "https://api-inference.huggingface.co/models/";
    readonly string apiKey;
    readonly string endpoint;  // full URL to POST to

    [Serializable]
    class Payload { public string inputs; }

    [Serializable]
    class HFResult { public string generated_text; }

    [Serializable]
    class Wrapper { public HFResult[] array; }

    /// <summary>
    /// If modelOrUrl starts with "http", uses it verbatim; otherwise prepends BASE_URL.
    /// </summary>
    public SimpleHF(string hfApiKey, string modelOrUrl)
    {
        apiKey = hfApiKey;
        if (modelOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            endpoint = modelOrUrl;
        else
            endpoint = BASE_URL + modelOrUrl;
    }

    public async Task<string> GetAnswerAsync(string prompt)
    {
        var bodyJson = JsonUtility.ToJson(new Payload { inputs = prompt });
        using var www = new UnityWebRequest(endpoint, "POST")
        {
            uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        www.SetRequestHeader("Content-Type", "application/json");

        var op = www.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (www.result != UnityWebRequest.Result.Success)
            throw new Exception($"HF error ({www.responseCode}): {www.downloadHandler.text}");

        // wrap the JSON array for JsonUtility
        string wrapped = $"{{\"array\":{www.downloadHandler.text}}}";
        var res = JsonUtility.FromJson<Wrapper>(wrapped);
        if (res.array == null || res.array.Length == 0)
            throw new Exception("No generations returned");

        return res.array[0].generated_text.Trim();
    }
}
