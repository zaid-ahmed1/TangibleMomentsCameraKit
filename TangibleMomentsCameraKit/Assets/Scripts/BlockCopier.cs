using TMPro;
using UnityEngine;

public class BlockCopier : MonoBehaviour
{
    private bool hasCopied = false;
    public TextMeshProUGUI debugText;
    void OnTriggerEnter(Collider other)
    {
        if (hasCopied) return;

        var memoryBlock = other.GetComponent<JengaBlockMemory>();
        if (memoryBlock == null) return;

        // Reference to dummy block root
        Transform dummyRoot = transform.parent;

        GameObject clone = Instantiate(other.gameObject, dummyRoot.position, dummyRoot.rotation, dummyRoot.parent);
        clone.name = "Cloned_" + memoryBlock.memoryKey;

        debugText.text = clone.name;

        // Make green
        foreach (var rend in clone.GetComponentsInChildren<Renderer>())
        {
            rend.material.color = Color.green;
        }

        Destroy(dummyRoot.gameObject); // destroy the whole dummy
        hasCopied = true;
    }
}