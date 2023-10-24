using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.Windows.Speech;



public class SpeechTranslationApp : MonoBehaviour
{
    public Text originalTextDisplay = null;
    public Text translatedTextDisplay = null;
    private const string apiKey = "REPLACE WITH YOUR OWN DEEPL API KEY";  //removed my API key from public view

    [Obsolete]
    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonPress);
    }

    [Obsolete]
    void OnButtonPress()
    {

        string spokenText = AndroidSpeechRecognition();
        string language = LanguageUtils.GetDefaultLanguage(); 
        //string language = "EN-US"; //originally for testing
        TranslateText(spokenText, language);
    }

    private string AndroidSpeechRecognition()
    {
        string spokenText = null;

        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject speechRecognizer = new AndroidJavaObject("android.speech.SpeechRecognizer");

            if (speechRecognizer.Call<bool>("isRecognitionAvailable", currentActivity))
            {
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", "android.speech.action.RECOGNIZE_SPEECH");
                intent.Call<AndroidJavaObject>("putExtra", "android.speech.extra.LANGUAGE_MODEL", "free_form");

                // Start the speech recognition activity
                currentActivity.Call("startActivityForResult", intent, 0);

                // You should handle the result in Unity's OnActivityResult method
                // This is an example of how you can receive the result in Unity
                // Make sure to add your own logic to handle the result
                AndroidJavaObject speechResults = speechRecognizer.Call<AndroidJavaObject>("getResults");
                AndroidJavaObject matches = speechResults.Call<AndroidJavaObject>("getStringArrayList", new AndroidJavaClass("android.speech.SpeechRecognizer").GetStatic<string>("RESULTS_RECOGNITION"));

                if (matches != null && matches.Call<int>("size") > 0)
                {
                    spokenText = matches.Call<string>("get", 0);
                }
            }
            else
            {
                Debug.LogError("Speech recognition is not available on this device.");
            }
        }

        return spokenText;
    }


    [Obsolete]
    private void TranslateText(string textToTranslate, string targetLanguage)
    {

        string deepLApiUrl = "https://api-free.deepl.com/v2/translate";
        string jsonData = $"{{\"text\":[\"{Uri.EscapeDataString(textToTranslate)}\"],\"target_lang\":\"{targetLanguage}\"}}";

        UnityWebRequest webRequest = new UnityWebRequest(deepLApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        webRequest.SetRequestHeader("Authorization", "DeepL-Auth-Key " + apiKey);
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("User-Agent", "YourApp/1.2.3");
        webRequest.SetRequestHeader("Content-Length", bodyRaw.Length.ToString());

        StartCoroutine(SendTranslationRequest(webRequest));
    }

    private IEnumerator SendTranslationRequest(UnityWebRequest webRequest)
    {
        yield return webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
            webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Translation request failed with error: " + webRequest.error + " and ");
            Debug.Log("Response: " + webRequest.downloadHandler.text);
        }
        else
        {
            DeepLTranslationResponse translationResponse =
                JsonUtility.FromJson<DeepLTranslationResponse>(webRequest.downloadHandler.text);

            if (translationResponse != null &&
                translationResponse.translations != null &&
                translationResponse.translations.Length > 0 &&
                translationResponse.translations[0] != null)
            {
                string translatedText = translationResponse.translations[0].text;
                string detectedLanguage = translationResponse.translations[0].detected_source_language;

                if (!string.IsNullOrEmpty(detectedLanguage))
                {
                    originalTextDisplay.text = detectedLanguage;
                }
                else
                {
                    // Handle the case where detected_source_language is null or empty
                    originalTextDisplay.text = "Language detection failed";
                }

                translatedTextDisplay.text = translatedText;
            }
            else
            {
                // Handle the case where translationResponse or translations are null or empty
                originalTextDisplay.text = "Translation not available";
                translatedTextDisplay.text = "Translation not available";
            }
        }
    }
}

[Serializable]
public class DeepLTranslationRequest
{
    public string source_lang;
    public string target_lang;
    public string text;
}

[Serializable]
public class DeepLTranslationResponse
{
    public DeepLTranslation[] translations;
}

[Serializable]
public class DeepLTranslation
{
    public string detected_source_language;
    public string text;
}

public static class LanguageUtils
{
    public static string GetDefaultLanguage()
    {
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        return currentCulture.TwoLetterISOLanguageName;
    }
}