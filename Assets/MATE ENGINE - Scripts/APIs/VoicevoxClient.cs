using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class VoicevoxClient
{
    private const string Host = "127.0.0.1";
    private const int Port = 50021;
    private const int QueryTimeoutSeconds = 5;
    private const int SynthesisTimeoutSeconds = 30;
    private const float SpeedScale = 1.15f;

    public static async Task<AudioClip> SynthesizeAsync(string text, int speakerId)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        string queryUrl = $"http://{Host}:{Port}/audio_query?text={UnityWebRequest.EscapeURL(text)}&speaker={speakerId}";
        string audioQueryJson = await PostForStringAsync(queryUrl);
        if (audioQueryJson == null) return null;

        audioQueryJson = ApplySpeedScale(audioQueryJson, SpeedScale);

        string synthesisUrl = $"http://{Host}:{Port}/synthesis?speaker={speakerId}";
        return await PostForAudioClipAsync(synthesisUrl, audioQueryJson);
    }

    private static string ApplySpeedScale(string audioQueryJson, float speedScale)
    {
        // audio_query always returns a top-level "speedScale" field; the query body
        // is otherwise passed through untouched rather than fully modeled/parsed.
        return Regex.Replace(audioQueryJson, "\"speedScale\"\\s*:\\s*[0-9.]+", $"\"speedScale\":{speedScale}");
    }

    private static async Task<string> PostForStringAsync(string url)
    {
        using (var request = new UnityWebRequest(url, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = QueryTimeoutSeconds;

            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[VoicevoxClient] audio_query failed (VOICEVOX not running?): {request.error}");
                return null;
            }
            return request.downloadHandler.text;
        }
    }

    private static async Task<AudioClip> PostForAudioClipAsync(string url, string bodyJson)
    {
        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.SetRequestHeader("Content-Type", "application/json");
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
            request.timeout = SynthesisTimeoutSeconds;

            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[VoicevoxClient] synthesis failed (VOICEVOX not running?): {request.error}");
                return null;
            }
            return DownloadHandlerAudioClip.GetContent(request);
        }
    }
}
