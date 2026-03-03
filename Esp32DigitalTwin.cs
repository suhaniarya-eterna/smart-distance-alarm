using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Audio; // For Audio Source control

public class Esp32DigitalTwin : MonoBehaviour
{
    [Header("Connection Settings")]
    public string esp32Ip = "172.23.87.251"; // CHANGE TO YOUR ESP32 IP

    [Header("Target Objects")]
    public GameObject esp32Model;      // Cube representing physical ESP32
    public Light alarmLight;           // Light that changes color on alarm

    [Header("Audio Sources")]
    public AudioSource alarmSoundSource; // Audio Source component attached here

    [Header("Alarm Clips")]
    public AudioClip alarmOnClip;        // Siren sound when alarm active
    public AudioClip alarmOffClip;       // Sound when safe again
    public AudioClip alertBeep;          // Short beep for warnings

    private float lastDistance = 0f;
    private bool wasAlarmActive = false;

    void Start()
    {
        Debug.Log($"Starting Connection to: http://{esp32Ip}/distance");

        // Make sure Audio Source is playing immediately
        if (alarmSoundSource != null && alarmSoundSource.clip == null)
        {
            alarmSoundSource.loop = true;
        }

        StartCoroutine(PollESP32());
    }

    IEnumerator PollESP32()
    {
        while (true)
        {
            string url = $"http://{esp32Ip}/distance";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 3;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Request Failed: {request.error}");
                }
                else
                {
                    ParseJsonData(request.downloadHandler.text);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    void ParseJsonData(string json)
    {
        float distance = 0f;
        bool alarm = false;

        // Extract numeric value for "distance"
        int distStart = json.IndexOf("\"distance\":") + 12;
        int distEnd = json.IndexOf(",", distStart);
        if (distEnd > distStart)
        {
            string distStr = json.Substring(distStart, distEnd - distStart);
            float.TryParse(distStr, out distance);
        }

        // Extract boolean value for "alarm"
        if (json.Contains("\"alarm\":true"))
        {
            alarm = true;
        }

        // Apply to Virtual Model
        UpdateDigitalTwin(distance, alarm);
    }

    void UpdateDigitalTwin(float distance, bool alarm)
    {
        // Update visual representation
        if (esp32Model != null)
        {
            esp32Model.transform.localScale = Vector3.one * (distance / 100f + 1f);
        }

        // Control Alarm Light
        if (alarmLight != null)
        {
            alarmLight.color = alarm ? Color.red : Color.green;
        }

        // 🔊 TRIGGER ALARM AUDIO
        HandleAlarmState(alarm, distance);

        // Log for debugging
        Debug.Log($"Distance: {distance}cm | Alarm: {alarm}");
    }

    void HandleAlarmState(bool alarm, float distance)
    {
        if (alarm != wasAlarmActive && alarmSoundSource != null)
        {
            if (alarm)
            {
                // ⚠️ Just entered danger zone
                if (alarmOnClip != null)
                {
                    alarmSoundSource.PlayOneShot(alarmOnClip);
                }
                Debug.Log("🚨 ALARM ACTIVE!");
            }
            else
            {
                // ✅ Back to safe zone
                if (alarmOffClip != null)
                {
                    alarmSoundSource.PlayOneShot(alarmOffClip);
                }
                else if (alertBeep != null)
                {
                    // Short beep if no off sound
                    alarmSoundSource.PlayOneShot(alertBeep);
                }
                Debug.Log("✅ SAFE ZONE ACHIEVED");
            }
        }
        else if (alarm && !wasAlarmActive)
        {
            // Keep alarm active sound going continuously
            if (alarmOnClip != null && !alarmSoundSource.isPlaying)
            {
                alarmSoundSource.PlayOneShot(alarmOnClip);
            }
        }

        wasAlarmActive = alarm;
    }
}