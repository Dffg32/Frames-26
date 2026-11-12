using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static TextDrawing;

// The menu shown over the frozen level: play, saved games and the ranking.
// Saved games and scores live on the server, so GUARDADO asks you to log in or register first.
// Arrows / W-S to choose, ENTER to accept, ESC to go back, TAB to switch between text fields.
// Options can also be clicked.
public class MainMenu : MonoBehaviour
{
    enum Page { Main, Account, Save, Ranking }

    static readonly string[] MainOptions = { "JUGAR", "GUARDADO", "RANKING" };
    static readonly string[] AccountOptions = { "INICIAR SESIÓN", "REGISTRARSE", "VOLVER" };
    static readonly string[] SaveOptions = { "GUARDAR PARTIDA", "CARGAR PARTIDA", "CERRAR SESIÓN", "VOLVER" };
    static readonly string[] RankingOptions = { "VOLVER" };

    Page page;
    int index;
    // Server requests still running; the menu ignores input until they finish.
    int pendingRequests;
    string notice, info;
    Color noticeColor;
    float noticeUntil;
    string[] rankingLines = new string[0];
    string userField = "", passwordField = "";
    bool focusUserField;
    // Server id of the player's latest saved game (0 if none), so saving overwrites it.
    int savedGameId;

    bool Busy => pendingRequests > 0;

    string[] Options => page switch
    {
        Page.Main => MainOptions,
        Page.Account => AccountOptions,
        Page.Save => SaveOptions,
        _ => RankingOptions,
    };

    public void Open()
    {
        enabled = true;
        OpenPage(Page.Main, 0);
    }

    public void Close() => enabled = false;

    void OpenPage(Page newPage, int selected)
    {
        page = newPage;
        index = selected;
        notice = null;
        info = null;
        switch (page)
        {
            case Page.Account:
                info = "Inicia sesión o crea una cuenta para guardar tus partidas en el servidor";
                focusUserField = true;
                break;
            case Page.Save:
                info = "Buscando tu partida en el servidor...";
                Run(ApiClient.GetLatestSave(OnLatestSave));
                break;
            case Page.Ranking:
                info = "Cargando el ranking...";
                rankingLines = new string[0];
                Run(ApiClient.GetRanking(10, OnRanking));
                break;
        }
    }

    void Choose(int option)
    {
        if (Busy) return;
        index = option;
        switch (page)
        {
            case Page.Main:
                if (option == 0) Game.Instance.StartNewGame();
                else if (option == 1) OpenPage(ApiClient.LoggedIn ? Page.Save : Page.Account, 0);
                else OpenPage(Page.Ranking, 0);
                break;

            case Page.Account:
                if (option == 0) Run(ApiClient.Login(userField, passwordField, OnLoggedIn));
                else if (option == 1) Run(ApiClient.Register(userField, passwordField, OnLoggedIn));
                else OpenPage(Page.Main, 1);
                break;

            case Page.Save:
                if (option == 0) SaveGame();
                else if (option == 1) Run(ApiClient.GetLatestSave(OnLoadRequested));
                else if (option == 2)
                {
                    ApiClient.Logout();
                    savedGameId = 0;
                    OpenPage(Page.Account, 0);
                    ShowNotice("Sesión cerrada", Good);
                }
                else OpenPage(Page.Main, 1);
                break;

            default:
                OpenPage(Page.Main, 2);
                break;
        }
    }

    void SaveGame()
    {
        var progress = Game.Instance.CurrentProgress;
        if (progress == null)
        {
            ShowNotice("No hay ninguna partida en curso. Pulsa ESC durante la partida para venir aquí.", Bad);
            return;
        }
        Run(ApiClient.SaveGame(progress, savedGameId, (id, error) =>
        {
            if (error != null)
            {
                ShowServerError(error);
                return;
            }
            savedGameId = id;
            // Also keep a local copy of the last save in partida.json.
            SaveSystem.Save(progress);
            info = Describe(progress);
            ShowNotice("Partida guardada en el servidor", Good);
        }));
    }

    void OnLoggedIn(string error)
    {
        if (error != null)
        {
            ShowNotice(error, Bad);
            return;
        }
        passwordField = "";
        OpenPage(Page.Save, 0);
        ShowNotice($"Sesión iniciada como {ApiClient.UserName}", Good);
    }

    void OnLatestSave(SavedGame save, string error)
    {
        if (error != null)
        {
            ShowServerError(error);
            return;
        }
        savedGameId = save?.id ?? 0;
        info = save == null ? "Todavía no tienes ninguna partida guardada" : Describe(save.ToSaveData());
    }

    void OnLoadRequested(SavedGame save, string error)
    {
        if (error != null) ShowServerError(error);
        else if (save == null) ShowNotice("No tienes ninguna partida guardada", Bad);
        else Game.Instance.ContinueFrom(save.ToSaveData());
    }

    void OnRanking(ScoreEntry[] entries, string error)
    {
        if (error != null)
        {
            info = null;
            ShowNotice(error, Bad);
            return;
        }
        info = entries.Length == 0 ? "Todavía no hay puntuaciones" : null;
        rankingLines = new string[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            rankingLines[i] = $"{entries[i].posicion}. {entries[i].nombreUsuario}   {entries[i].puntos} PUNTOS   (MUNDO {entries[i].nivel})";
    }

    // If the session expired, the request logged us out: go back to the login page.
    void ShowServerError(string error)
    {
        if (page == Page.Save && !ApiClient.LoggedIn) OpenPage(Page.Account, 0);
        else info = null;
        ShowNotice(error, Bad);
    }

    static string Describe(SaveData data) =>
        $"{ApiClient.UserName}:  MUNDO {data.nivel} · {data.puntos} PUNTOS · {data.monedas} MONEDAS · {data.vidas} VIDAS";

    void ShowNotice(string text, Color color)
    {
        notice = text;
        noticeColor = color;
        noticeUntil = Time.unscaledTime + 3f;
    }

    void Run(IEnumerator request) => StartCoroutine(RunTracked(request));

    IEnumerator RunTracked(IEnumerator request)
    {
        pendingRequests++;
        yield return request;
        pendingRequests--;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || Busy) return;
        // While typing in a text field, W, S and SPACE are letters, not menu keys.
        bool typing = page == Page.Account && GUIUtility.keyboardControl != 0;
        int count = Options.Length;
        if (kb.upArrowKey.wasPressedThisFrame || (!typing && kb.wKey.wasPressedThisFrame)) index = (index + count - 1) % count;
        if (kb.downArrowKey.wasPressedThisFrame || (!typing && kb.sKey.wasPressedThisFrame)) index = (index + 1) % count;

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || (!typing && kb.spaceKey.wasPressedThisFrame))
            Choose(index);
        else if (kb.escapeKey.wasPressedThisFrame && page != Page.Main)
            OpenPage(Page.Main, page == Page.Ranking ? 2 : 1);
    }

    void OnGUI()
    {
        var baseStyle = BaseStyle();
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var title = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 3 };
        string heading = page switch { Page.Main => "JUEGARDO", Page.Account => "CUENTA", Page.Save => "GUARDADO", _ => "RANKING" };
        Shadowed(new Rect(0, Screen.height * 0.08f, Screen.width, Screen.height * 0.18f), heading, title, Gold);

        var small = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 3 / 4 };
        float lineHeight = small.fontSize * 1.6f;
        if (info != null) Shadowed(new Rect(0, Screen.height * 0.27f, Screen.width, lineHeight), info, small);

        float optionsTop = Screen.height * 0.4f;
        if (page == Page.Account)
        {
            DrawLoginFields(small, Screen.height * 0.34f, lineHeight);
            optionsTop = Screen.height * 0.52f;
        }
        else if (page == Page.Ranking)
        {
            for (int i = 0; i < rankingLines.Length; i++)
                Shadowed(new Rect(0, Screen.height * 0.33f + i * lineHeight, Screen.width, lineHeight), rankingLines[i], small, i == 0 ? Gold : Color.white);
            optionsTop = Screen.height * 0.8f;
        }

        var option = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 5 / 4 };
        var options = Options;
        float height = option.fontSize * 1.8f;
        for (int i = 0; i < options.Length; i++)
        {
            var rect = new Rect(0, optionsTop + i * height, Screen.width, height);
            if (rect.Contains(Event.current.mousePosition) && !Busy) index = i;
            bool selected = i == index;
            Shadowed(rect, selected ? $"> {options[i]} <" : options[i], option, selected ? Gold : Color.white);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) Choose(i);
        }

        var status = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter, fontSize = baseStyle.fontSize * 4 / 5 };
        var statusRect = new Rect(0, optionsTop + options.Length * height + 8, Screen.width, height);
        if (Busy) Shadowed(statusRect, "Conectando con el servidor...", status, Gold);
        else if (notice != null && Time.unscaledTime < noticeUntil) Shadowed(statusRect, notice, status, noticeColor);

        var hint = new GUIStyle(baseStyle) { alignment = TextAnchor.LowerCenter, fontSize = baseStyle.fontSize * 2 / 3 };
        string help = page switch
        {
            Page.Main => "Flechas para elegir · ENTER para aceptar",
            Page.Account => "TAB para cambiar de campo · ENTER para aceptar · ESC para volver",
            _ => "Flechas para elegir · ENTER para aceptar · ESC para volver",
        };
        Shadowed(new Rect(0, 0, Screen.width, Screen.height - 20), help, hint);
    }

    void DrawLoginFields(GUIStyle labelStyle, float top, float lineHeight)
    {
        // TAB jumps between the two fields.
        var e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Tab || e.character == '\t'))
        {
            if (e.keyCode == KeyCode.Tab)
                GUI.FocusControl(GUI.GetNameOfFocusedControl() == "usuario" ? "contrasena" : "usuario");
            e.Use();
        }

        var label = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleRight };
        var field = new GUIStyle(GUI.skin.textField) { fontSize = labelStyle.fontSize, alignment = TextAnchor.MiddleLeft };
        float width = Mathf.Min(420f, Screen.width * 0.4f);
        float x = (Screen.width - width) / 2f;
        float fieldHeight = lineHeight * 1.1f;

        Shadowed(new Rect(0, top, x - 12, fieldHeight), "Usuario", label);
        GUI.SetNextControlName("usuario");
        userField = GUI.TextField(new Rect(x, top, width, fieldHeight), userField, 20, field);

        Shadowed(new Rect(0, top + fieldHeight * 1.3f, x - 12, fieldHeight), "Contraseña", label);
        GUI.SetNextControlName("contrasena");
        passwordField = GUI.PasswordField(new Rect(x, top + fieldHeight * 1.3f, width, fieldHeight), passwordField, '*', 64, field);

        if (focusUserField)
        {
            GUI.FocusControl("usuario");
            focusUserField = false;
        }
    }
}
