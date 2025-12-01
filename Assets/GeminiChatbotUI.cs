using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// State-of-the-art UI for Gemini chatbot with streaming text animation
    /// </summary>
    public class GeminiChatbotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text chatDisplay;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button clearHistoryButton;
    [SerializeField] private ScrollRect scrollRect; // For scrolling the chat display
    
    [Header("Chatbot Reference")]
    [SerializeField] private GeminiChat geminiChat;
    
    [Header("UI Settings")]
    [SerializeField] private float typingSpeed = 0.02f;
    [SerializeField] private string userPrefix = "You: ";
    [SerializeField] private string aiPrefix = "AI: ";
    [SerializeField] private Color userColor = new Color(0.4f, 0.8f, 1f); // Light blue
    [SerializeField] private Color aiColor = new Color(0.5f, 1f, 0.5f);   // Light green
    [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.4f); // Light red
    
    private StringBuilder chatHistory = new StringBuilder();
    private Coroutine typingCoroutine;
    private string currentUserMessage;
    private bool isTyping = false;
    
    void Start()
    {
        // Auto-discover references if missing
        if (chatDisplay == null)
        {
            chatDisplay = GetComponentInChildren<TMP_Text>();
            if (chatDisplay == null)
                Debug.LogError("[GeminiChatbotUI] Chat display text not assigned and could not be found in children!");
        }
        
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>();
            if (inputField == null)
                Debug.LogError("[GeminiChatbotUI] Input field not assigned and could not be found in children!");
        }
        
        if (geminiChat == null)
        {
            geminiChat = GetComponent<GeminiChat>();
            if (geminiChat == null)
                Debug.LogError("[GeminiChatbotUI] GeminiChat component not assigned and could not be found on this GameObject!");
        }

        // Auto-discover ScrollRect if not assigned
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect == null)
                Debug.LogWarning("[GeminiChatbotUI] ScrollRect not found. Scrolling may not work properly.");
        }

        // Validate references
        if (chatDisplay == null || inputField == null || geminiChat == null)
        {
            return;
        }
        
        // Configure chat display for scrolling
        ConfigureChatDisplayForScrolling();
        
        // Setup button listeners
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }
        
        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.AddListener(OnClearHistoryClicked);
        }
        
        // Subscribe to GeminiChat events
        geminiChat.OnStreamingChunk += OnStreamingChunk;
        geminiChat.OnResponseComplete += OnResponseComplete;
        geminiChat.OnError += OnError;
        
        // Initial welcome message
        ShowWelcomeMessage();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (geminiChat != null)
        {
            geminiChat.OnStreamingChunk -= OnStreamingChunk;
            geminiChat.OnResponseComplete -= OnResponseComplete;
            geminiChat.OnError -= OnError;
        }
    }
    
    void Update()
    {
        // Send message on Enter key (but not Shift+Enter for multi-line)
        if (inputField != null && 
            inputField.isFocused && 
            Assets.Scripts.GlobalInputManager.GetKeyDown(KeyCode.Return) && 
            !Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftShift) && 
            !Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightShift))
        {
            OnSendClicked();
        }
    }
    
    private void ShowWelcomeMessage()
    {
        chatHistory.Clear();
        chatHistory.AppendLine(GetColoredText("Welcome to Unity AI Assistant!", aiColor));
        chatHistory.AppendLine(GetColoredText("Powered by Gemini 2.0 Flash", aiColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
    }
    
    public void OnSendClicked()
    {
        if (geminiChat == null)
        {
            Debug.LogError("[GeminiChatbotUI] GeminiChat is not assigned!");
            return;
        }
        
        if (chatHistory == null)
        {
            Debug.LogError("[GeminiChatbotUI] chatHistory is null! Reinitializing...");
            chatHistory = new StringBuilder();
        }
        
        if (geminiChat.IsProcessing() || isTyping)
        {
            Debug.Log("[GeminiChatbotUI] Already processing a request");
            return;
        }
        
        string userMessage = inputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage))
        {
            return;
        }
        
        // Store current user message
        currentUserMessage = userMessage;
        
        // Clear input field
        inputField.text = "";
        
        // Add user message to chat display
        chatHistory.AppendLine(GetColoredText($"{userPrefix}{userMessage}", userColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
        
        // Disable send button while processing
        if (sendButton != null)
        {
            sendButton.interactable = false;
        }
        
        // Show "thinking" indicator
        chatHistory.Append(GetColoredText($"{aiPrefix}???", aiColor));
        UpdateChatDisplay(true);
        
        // Send to Gemini
        geminiChat.SendMessage(userMessage);
    }
    
    public void OnClearHistoryClicked()
    {
        if (geminiChat.IsProcessing() || isTyping)
        {
            Debug.Log("[GeminiChatbotUI] Cannot clear history while processing");
            return;
        }
        
        geminiChat.ClearHistory();
        ShowWelcomeMessage();
        Debug.Log("[GeminiChatbotUI] Chat history cleared");
    }
    
    private void OnStreamingChunk(string chunk)
    {
        // This is called for each chunk received from the API
        // We'll collect all chunks and display them when complete
        Debug.Log($"[GeminiChatbotUI] Received chunk: {chunk.Substring(0, Mathf.Min(50, chunk.Length))}...");
    }
    
    private void OnResponseComplete(string fullResponse)
    {
        Debug.Log($"[GeminiChatbotUI] Response complete: {fullResponse.Length} characters");
        
        // Remove "thinking" indicator
        RemoveLastLine();
        
        // Start typing animation for AI response
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(fullResponse));
        
        // Re-enable send button
        if (sendButton != null)
        {
            sendButton.interactable = true;
        }
        
        // Focus input field
        FocusInput();
    }
    
    private void OnError(string errorMessage)
    {
        Debug.LogError($"[GeminiChatbotUI] Error: {errorMessage}");
        
        // Remove "thinking" indicator
        RemoveLastLine();
        
        // Show error message
        chatHistory.AppendLine(GetColoredText($"{aiPrefix}Error: {errorMessage}", errorColor));
        chatHistory.AppendLine();
        UpdateChatDisplay(true);
        
        // Re-enable send button
        if (sendButton != null)
        {
            sendButton.interactable = true;
        }
        
        // Focus input field
        FocusInput();
    }
    
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        
        // Add AI prefix
        chatHistory.Append(GetColoredText(aiPrefix, aiColor));
        
        // Type out each character with delay
        foreach (char c in text)
        {
            chatHistory.Append(GetColoredText(c.ToString(), aiColor));
            UpdateChatDisplay(false); // Don't force scroll while typing, only if at bottom
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Add spacing
        chatHistory.AppendLine();
        chatHistory.AppendLine();
        UpdateChatDisplay(false);
        
        isTyping = false;
    }
    
    private void UpdateChatDisplay(bool forceScrollToBottom = false)
    {
        if (chatDisplay != null)
        {
            chatDisplay.text = chatHistory.ToString();
            
            // Force scroll to bottom if using ScrollRect
            Canvas.ForceUpdateCanvases();
            
            // Scroll to bottom - run this on next frame to ensure layout is updated
            if (scrollRect != null)
            {
                StartCoroutine(ScrollToBottomNextFrame(forceScrollToBottom));
            }
        }
    }
    
    private void RemoveLastLine()
    {
        if (chatHistory == null)
        {
            Debug.LogError("[GeminiChatbotUI] chatHistory is null!");
            return;
        }
        
        string currentText = chatHistory.ToString();
        if (string.IsNullOrEmpty(currentText))
        {
            return;
        }
        
        int lastNewline = currentText.LastIndexOf('\n');
        if (lastNewline > 0)
        {
            chatHistory.Clear();
            chatHistory.Append(currentText.Substring(0, lastNewline + 1));
            UpdateChatDisplay();
        }
    }
    
    private string GetColoredText(string text, Color color)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{hexColor}>{text}</color>";
    }
    
    
    /// <summary>
    /// Scroll to bottom on the next frame (after layout update)
    /// </summary>
    private IEnumerator ScrollToBottomNextFrame(bool force)
    {
        // Check if we are already near the bottom (within 5%)
        bool isNearBottom = false;
        if (scrollRect != null)
        {
            // 0 is bottom, 1 is top
            isNearBottom = scrollRect.verticalNormalizedPosition <= 0.05f;
        }

        yield return null; // Wait one frame for layout to update
        
        if (scrollRect != null)
        {
            // Scroll if forced OR if we were already at the bottom
            if (force || isNearBottom)
            {
                scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom, 1 = top
            }
        }
    }
    
    /// <summary>
    /// Focus the input field (can be called from other scripts)
    /// </summary>
    public void FocusInput()
    {
        if (inputField != null && gameObject.activeInHierarchy)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
    }
    
    /// <summary>
    /// Set typing speed for animation
    /// </summary>
    public void SetTypingSpeed(float speed)
    {
        typingSpeed = Mathf.Max(0.001f, speed);
    }
    
    private void ConfigureChatDisplayForScrolling()
    {
        if (chatDisplay != null)
        {
            // Ensure text wraps
            chatDisplay.enableWordWrapping = true;
            chatDisplay.overflowMode = TextOverflowModes.Overflow;
            
            if (scrollRect != null && scrollRect.content != null)
            {
                RectTransform contentRect = scrollRect.content;
                
                // 1. Ensure Content pivots from the top so it grows downwards
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                
                // 2. Ensure Content has a ContentSizeFitter
                ContentSizeFitter contentFitter = contentRect.GetComponent<ContentSizeFitter>();
                if (contentFitter == null)
                {
                    contentFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
                }
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // 3. If the text is a child of Content (not the content itself), 
                // we need a VerticalLayoutGroup on Content to drive the child text
                if (chatDisplay.transform != contentRect && chatDisplay.transform.IsChildOf(contentRect))
                {
                    VerticalLayoutGroup layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    
                    layoutGroup.childControlHeight = true;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childForceExpandWidth = true;
                    
                    // Add some padding if needed
                    layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                }
            }
        }
    }

    /// <summary>
    /// Check if the input field is currently focused (blocks other input)
    /// Other scripts should call this to check if they should ignore keyboard input
    /// </summary>
    public bool IsInputFieldFocused()
    {
        return inputField != null && inputField.isFocused;
    }
    
    /// <summary>
    /// Check if this chatbot UI is active and blocking input
    /// </summary>
    public bool IsBlockingInput()
    {
        return gameObject.activeInHierarchy && IsInputFieldFocused();
    }
    }
}
