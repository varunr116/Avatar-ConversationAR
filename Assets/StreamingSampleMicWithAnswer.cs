using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Whisper;
using HuggingFace.API;
public class StreamingSampleMicWithAnswer : MonoBehaviour
{
   [Header("Whisper Setup")]
    public WhisperManager whisper;            // assign your WhisperManager asset
    public MicrophoneRecord mic;              // assign your MicrophoneRecord component

    [Header("UI References")]
    public Button    recordButton;            // Record / Stop button
    public Text      recordButtonText;        // Label on the button
    public Text      transcriptText;          // Shows live & final transcript
    public Text      answerText;              // Shows HF’s answer
    public ScrollRect scrollRect;             // For auto-scrolling

    // Internal stream handle
    private WhisperStream _stream;

    async void Start()
    {
        // 1) Create and hook the Whisper stream
        _stream = await whisper.CreateStream(mic);
        _stream.OnResultUpdated  += OnPartialTranscript;
        _stream.OnStreamFinished += OnFinalTranscript;
        
        // 2) Hook record stop to reset button text
        mic.OnRecordStop += OnRecordStop;
        
        // 3) Wire up button
        recordButton.onClick.AddListener(OnRecordButtonClicked);
        recordButtonText.text = "Record";
        answerText.text       = "";
    }

    private void OnRecordButtonClicked()
    {
        if (!mic.IsRecording)
        {
            // Clear previous texts
            transcriptText.text = "";
            answerText.text     = "";

            // Start streaming & recording
            _stream.StartStream();
            mic.StartRecord();
            recordButtonText.text = "Stop";
        }
        else
        {
            // Stop recording (triggers OnFinalTranscript)
            mic.StopRecord();
            recordButtonText.text = "Record";
        }
    }

    private void OnRecordStop(AudioChunk _)
    {
        // Ensure button label is correct if stopped externally
        recordButtonText.text = "Record";
    }

    private void OnPartialTranscript(string partial)
    {
        // Live update
        transcriptText.text = partial;
        ScrollToBottom();
    }

    private void OnFinalTranscript(string finalTranscript)
    {
        // Display the final transcript
        transcriptText.text = finalTranscript;
        ScrollToBottom();

        // Kick off Hugging Face text generation
        answerText.text = "Thinking…";
        HuggingFaceAPI.TextGeneration(
            finalTranscript,
            onSuccess: answer => {
                answerText.text = answer;
                ScrollToBottom();
            },
            onError: error => {
                answerText.text = $"Error: {error}";
                ScrollToBottom();
            }
        );
    }

    private void ScrollToBottom()
    {
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
