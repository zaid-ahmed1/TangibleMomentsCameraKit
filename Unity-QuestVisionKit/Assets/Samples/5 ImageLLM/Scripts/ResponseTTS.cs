using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;

public enum CharacterEffect
{
    None,
    Chipmunk,
    Monster,
    Daemon,
    Robot,
    Alien
}

public enum EnvironmentEffect
{
    None,
    Reverb,
    Room,
    Cathedral,
    Radio,
    Phone,
    PA,
    Helmet
}

public enum VoiceSettings
{
    BritishButler,
    Cael,
    Cam,
    Carl,
    CartoonBaby,
    CartoonKid,
    CartoonVillain,
    Charlie,
    CockneyAccent,
    Cody,
    Colin,
    Connor,
    Cooper,
    Disaffected,
    Hollywood,
    KenyanAccent,
    Overconfident,
    Pirate,
    Prospector,
    Railey,
    Rebecca,
    Remi,
    Rosie,
    Rubie,
    SouthernAccent,
    Surfer,
    Trendy,
    Vampire,
    Whimsical,
    Wizard
}

public static class VoiceSettingsExtensions
{
    public static string ToVoiceId(this VoiceSettings voiceSettings)
    {
        return voiceSettings switch
        {
            VoiceSettings.BritishButler   => "WIT$BRITISH BUTLER",
            VoiceSettings.Cael            => "WIT$CAEL",
            VoiceSettings.Cam             => "WIT$CAM",
            VoiceSettings.Carl            => "WIT$CARL",
            VoiceSettings.CartoonBaby     => "WIT$CARTOON BABY",
            VoiceSettings.CartoonKid      => "WIT$CARTOON KID",
            VoiceSettings.CartoonVillain  => "WIT$CARTOON VILLAIN",
            VoiceSettings.Charlie         => "WIT$CHARLIE",
            VoiceSettings.CockneyAccent   => "WIT$COCKNEY ACCENT",
            VoiceSettings.Cody            => "WIT$CODY",
            VoiceSettings.Colin           => "WIT$COLIN",
            VoiceSettings.Connor          => "WIT$CONNOR",
            VoiceSettings.Cooper          => "WIT$COOPER",
            VoiceSettings.Disaffected     => "WIT$DISAFFECTED",
            VoiceSettings.Hollywood       => "WIT$HOLLYWOOD",
            VoiceSettings.KenyanAccent    => "WIT$KENYAN ACCENT",
            VoiceSettings.Overconfident   => "WIT$OVERCONFIDENT",
            VoiceSettings.Pirate          => "WIT$PIRATE",
            VoiceSettings.Prospector      => "WIT$PROSPECTOR",
            VoiceSettings.Railey          => "WIT$RAILEY",
            VoiceSettings.Rebecca         => "WIT$REBECCA",
            VoiceSettings.Remi            => "WIT$REMI",
            VoiceSettings.Rosie           => "WIT$ROSIE",
            VoiceSettings.Rubie           => "WIT$RUBIE",
            VoiceSettings.SouthernAccent  => "WIT$SOUTHERN ACCENT",
            VoiceSettings.Surfer          => "WIT$SURFER",
            VoiceSettings.Trendy          => "WIT$TRENDY",
            VoiceSettings.Vampire         => "WIT$VAMPIRE",
            VoiceSettings.Whimsical       => "WIT$WHIMSICAL",
            VoiceSettings.Wizard          => "WIT$WIZARD",
            _ => string.Empty
        };
    }
}

public class ResponseTTS : MonoBehaviour
{
    [Header("TTS Components")]
    [SerializeField] private TTSSpeaker ttsSpeaker;

    [Header("TTS Voice Settings")]
    [SerializeField] private CharacterEffect selectedCharacter = CharacterEffect.Robot;
    [SerializeField] private EnvironmentEffect selectedEnvironment = EnvironmentEffect.Phone;
    [SerializeField] private VoiceSettings selectedVoice = VoiceSettings.Charlie;

    private void OnValidate() => ApplySelectedVoice();

    private void ApplySelectedVoice()
    {
        if (ttsSpeaker && ttsSpeaker.TTSService != null)
        {
            var selectedVoiceId = selectedVoice.ToVoiceId();
            var voiceSettings = ttsSpeaker.TTSService.GetAllPresetVoiceSettings();
            var selectedVoiceSetting = voiceSettings.FirstOrDefault(vs => vs.SettingsId == selectedVoiceId);
            if (selectedVoiceSetting != null)
            {
                ttsSpeaker.VoiceID = selectedVoiceSetting.SettingsId;
            }
            else
                Debug.LogWarning($"Selected voice {selectedVoice} not found in TTS Service.");
        }
        else
        {
            Debug.LogWarning("TTS Service not found or TTSSpeaker not assigned.");
        }
    }

    /// <summary>
    /// Displays the response text and initiates speaking.
    /// </summary>
    public void Speak(string textToSpeak)
    {
        StartCoroutine(SpeakAsync(textToSpeak));
    }

    private IEnumerator SpeakAsync(string text)
    {
        var ssmlText = ApplyEffects(text, selectedCharacter, selectedEnvironment);
        yield return ttsSpeaker.SpeakAsync(ssmlText);
    }

    private static string ApplyEffects(string text, CharacterEffect character, EnvironmentEffect environment)
    {
        if (character == CharacterEffect.None && environment == EnvironmentEffect.None)
            return text;

        var sb = new StringBuilder();
        sb.Append("<speak><sfx");
        if (character != CharacterEffect.None)
        {
            sb.AppendFormat(" character=\"{0}\"", character.ToString().ToLower());
        }

        if (environment != EnvironmentEffect.None)
        {
            sb.AppendFormat(" environment=\"{0}\"", environment.ToString().ToLower());
        }
        
        sb.Append(">");
        sb.Append(text);
        sb.Append("</sfx></speak>");
        return sb.ToString();
    }
}
