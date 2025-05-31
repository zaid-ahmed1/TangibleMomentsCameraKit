using UnityEngine;

public class ParticipantButtonManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Method to handle participant button click
    public void SetParticipant(int participantNumber)
    {
        // Store the participant number in PlayerPrefs
        PlayerPrefs.SetInt("ParticipantNumber", participantNumber);
        PlayerPrefs.Save();
        
        // Log the participant number for debugging
        Debug.Log($"Participant number set to: {participantNumber}");
        
        // Optionally, you can add additional logic here, like updating UI or triggering events
        gameObject.SetActive(false);
    }
}
