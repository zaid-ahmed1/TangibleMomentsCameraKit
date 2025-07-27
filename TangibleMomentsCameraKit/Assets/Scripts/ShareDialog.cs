using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShareDialog : MonoBehaviour
{
    [Header("UI References")]
    public Toggle shareToggle;
    public Toggle cancelToggle;
    public GameObject dialogPanel; // The main dialog container
    public TextMeshProUGUI DebugText; // Text for debug output
    [Header("Dependencies")]
    public Postgres postgres; // Reference to your postgres manager
    private Action<string> onSuccessfulShareCallback;

    // Private fields to store the passed arguments
    private Memory currentMemory;
    private Toggle originalToggle;
    private string currentButtonId;
    private HashSet<string> processingButtons = new HashSet<string>();
    private string targetQrCode;
    
    private void Start()
    {
        // Set up toggle listeners
        if (shareToggle != null)
        {
            shareToggle.onValueChanged.AddListener(OnShareToggleChanged);
        }
        
        if (cancelToggle != null)
        {
            cancelToggle.onValueChanged.AddListener(OnCancelToggleChanged);
        }
        
    }
    
    // This method is called from GalleryButtonSpawner.ShowShareDialog()

    public void ShowDialog(Memory capturedMemory, Toggle toggle, string buttonId, string qrCodeTarget, Action<string> onSuccess = null)
    {
        currentMemory = capturedMemory;
        originalToggle = toggle;
        currentButtonId = buttonId;
        targetQrCode = qrCodeTarget;
        onSuccessfulShareCallback = onSuccess;

        dialogPanel.SetActive(true);
        if (shareToggle != null) shareToggle.isOn = false;
        if (cancelToggle != null) cancelToggle.isOn = false;

        DebugText.text = $"📱 Showing share dialog for memory: {capturedMemory?.title}, to QR: {targetQrCode}";
    }


    
    public void HideDialog()
    {
        dialogPanel.SetActive(false);
        
        // Clear stored arguments
        currentMemory = null;
        originalToggle = null;
        currentButtonId = null;
        
        DebugText.text=("📱 Share dialog hidden");
    }
    
    private void OnShareToggleChanged(bool isOn)
    {
        if (isOn && currentMemory != null)
        {
            // Prevent multiple clicks using the stored buttonId
            if (processingButtons.Contains(currentButtonId))
            {
                shareToggle.isOn = false;
                return;
            }
            
            processingButtons.Add(currentButtonId);
            
            // Execute your share logic using the stored arguments
            StartCoroutine(HandleShareAction(currentMemory, originalToggle, currentButtonId));
        }
    }
    
    private void OnCancelToggleChanged(bool isOn)
    {
        if (isOn)
        {
            Debug.Log("❌ Cancel selected");
            
            // Reset the dialog cancel toggle
            cancelToggle.isOn = false;
            
            // Reset the original toggle if it exists
            if (originalToggle != null)
            {
                originalToggle.isOn = false;
            }
            
            // Remove from processing if it was being processed
            if (!string.IsNullOrEmpty(currentButtonId))
            {
                processingButtons.Remove(currentButtonId);
            }
            
            // Hide the dialog
            HideDialog();
        }
    }
        private IEnumerator HandleShareAction(Memory capturedMemory, Toggle toggle, string buttonId)
        {
            Debug.Log("📤 Starting share coroutine...");
            yield return null;
    
            bool success = false;
    
            if (postgres == null)
            {
                Debug.Log("❌ Postgres is NULL!");
                if (toggle != null) toggle.isOn = false;
                processingButtons.Remove(buttonId);
                yield break;
            }
    
            // Perform coroutine separately from try block
            IEnumerator copyCoroutine = postgres.CopyMemoryToQrCodeCoroutine(capturedMemory, targetQrCode);
            yield return StartCoroutine(copyCoroutine);
    
            try
            {
                // No actual `yield` here — just post-success logic
                Debug.Log("📤 Memory copy completed successfully");
                success = true;
            }
            catch (System.Exception e)
            {
                Debug.Log($"❌ SHARE ERROR: {e.Message}");
                Debug.Log($"❌ Stack trace: {e.StackTrace}");
            }
    
            if (success)
            {
                Debug.Log($"✅ Successfully shared {capturedMemory.title} → {targetQrCode}");

                onSuccessfulShareCallback?.Invoke($"{capturedMemory.title}->{targetQrCode}");

                yield return new WaitForSeconds(0.2f);
                HideDialog();
            }

    
            if (toggle != null) toggle.isOn = false;
            processingButtons.Remove(buttonId);
            Debug.Log("📤 Share action finished");
        }




    
    private void OnDestroy()
    {
        // Clean up listeners
        if (shareToggle != null)
        {
            shareToggle.onValueChanged.RemoveListener(OnShareToggleChanged);
        }
        
        if (cancelToggle != null)
        {
            cancelToggle.onValueChanged.RemoveListener(OnCancelToggleChanged);
        }
    }
}