using UnityEditor;

// The game talks to the local server over plain HTTP (http://localhost:5080). Unity blocks HTTP by
// default, so allow it in the editor and development builds only; release builds still need HTTPS.
[InitializeOnLoad]
static class NetworkSettings
{
    static NetworkSettings()
    {
        if (PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
    }
}
