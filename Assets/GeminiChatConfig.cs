using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// ScriptableObject to store Gemini API configuration securely
    /// Create via: Assets > Create > Gemini > Chat Config
    /// </summary>
    [CreateAssetMenu(fileName = "GeminiChatConfig", menuName = "Gemini/Chat Config", order = 1)]
    public class GeminiChatConfig : ScriptableObject
{
    [Header("API Configuration")]
    [Tooltip("Your Gemini API key from https://aistudio.google.com/app/apikey")]
    [SerializeField] private string apiKey = "AIzaSyCGJfoVCxT9PTHtsmW7E42ag6pxxBze-20";
    
    [Tooltip("Gemini model to use - gemini-2.5-flash is recommended for best performance and stability")]
    [SerializeField] private string model = "gemini-2.5-flash";
    
    [Header("Request Settings")]
    [Tooltip("Maximum tokens in the response")]
    [Range(100, 8192)]
    [SerializeField] private int maxTokens = 2048;
    
    [Tooltip("Temperature controls randomness (0.0 = deterministic, 2.0 = very creative)")]
    [Range(0f, 2f)]
    [SerializeField] private float temperature = 0.9f;
    
    [Tooltip("Top-p sampling threshold")]
    [Range(0f, 1f)]
    [SerializeField] private float topP = 0.95f;
    
    [Tooltip("Top-k sampling - number of highest probability tokens to consider")]
    [Range(1, 100)]
    [SerializeField] private int topK = 40;
    
    [Header("System Behavior")]
    [Tooltip("System instruction to guide the AI's behavior")]
    [TextArea(3, 10)]
    [SerializeField] private string systemInstruction = "You are a helpful AI assistant in a Unity VR environment. Be concise, friendly, and helpful.";
    
    [Tooltip("Maximum number of messages to keep in conversation history")]
    [Range(5, 50)]
    [SerializeField] private int maxHistoryMessages = 20;
    
    [Header("Network Settings")]
    [Tooltip("Request timeout in seconds")]
    [Range(10, 120)]
    [SerializeField] private int timeoutSeconds = 30;
    
    [Tooltip("Number of retry attempts on failure")]
    [Range(0, 5)]
    [SerializeField] private int maxRetries = 3;

    // Public getters
    public string ApiKey => apiKey?.Trim();
    public string Model => model?.Trim();
    public int MaxTokens => maxTokens;
    public float Temperature => temperature;
    public float TopP => topP;
    public int TopK => topK;
    public string SystemInstruction => systemInstruction;
    public int MaxHistoryMessages => maxHistoryMessages;
    public int TimeoutSeconds => timeoutSeconds;
    public int MaxRetries => maxRetries;
    
    public string GetApiUrl()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            Debug.LogError("[GeminiChatConfig] API Key is not set!");
            return null;
        }
        
        if (string.IsNullOrWhiteSpace(Model))
        {
            Debug.LogError("[GeminiChatConfig] Model is not set!");
            return null;
        }
        
        // Use regular generateContent instead of streaming for better Unity compatibility
        return $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}";
    }
    
    private void OnValidate()
    {
        // Ensure model name is valid
        if (!string.IsNullOrWhiteSpace(model))
        {
            model = model.Trim();
        }
        
        // Ensure API key doesn't have whitespace
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = apiKey.Trim();
        }
    }
}
}
