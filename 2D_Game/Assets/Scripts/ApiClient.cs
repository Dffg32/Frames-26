using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Talks to the Juegardo server (the Servidor folder). Every call is a coroutine that finishes by
// calling a callback, so the game never freezes while it waits. Callbacks get an error message,
// or null when everything went well.
public static class ApiClient
{
    public const string BaseUrl = "http://localhost:5080/api";

    // The JWT is only kept in memory: closing the game logs you out.
    public static string Token { get; private set; }
    public static string UserName { get; private set; }
    public static bool LoggedIn => !string.IsNullOrEmpty(Token);

    public static void Logout()
    {
        Token = null;
        UserName = null;
    }

    public static IEnumerator Register(string user, string password, Action<string> done) =>
        Authenticate("auth/registro", user, password, done);

    public static IEnumerator Login(string user, string password, Action<string> done) =>
        Authenticate("auth/login", user, password, done);

    static IEnumerator Authenticate(string path, string user, string password, Action<string> done)
    {
        string body = JsonUtility.ToJson(new Credentials { nombreUsuario = user.Trim(), contrasena = password });
        yield return Send("POST", path, body, (error, text) =>
        {
            if (error == null)
            {
                var session = JsonUtility.FromJson<Session>(text);
                Token = session.token;
                UserName = session.nombreUsuario;
            }
            done(error);
        });
    }

    // The player's most recent saved game, or null if they have none.
    public static IEnumerator GetLatestSave(Action<SavedGame, string> done)
    {
        yield return Send("GET", "partidas", null, (error, text) =>
        {
            if (error != null) done(null, error);
            else
            {
                var list = JsonUtility.FromJson<SavedGameList>(WrapArray(text)).items;
                done(list.Length > 0 ? list[0] : null, null);
            }
        });
    }

    // Overwrites the saved game with this id, or creates a new one if id is 0 or it no longer exists.
    // Calls back with the id of the saved game.
    public static IEnumerator SaveGame(SaveData data, int id, Action<int, string> done)
    {
        string body = JsonUtility.ToJson(data);
        if (id > 0)
        {
            string updateError = null;
            long status = 0;
            yield return Send("PUT", $"partidas/{id}", body, (error, _) => updateError = error, code => status = code);
            if (updateError == null)
            {
                done(id, null);
                yield break;
            }
            if (status != 404)
            {
                done(0, updateError);
                yield break;
            }
        }
        yield return Send("POST", "partidas", body, (error, text) =>
            done(error == null ? JsonUtility.FromJson<SavedGame>(text).id : 0, error));
    }

    // Adds a score to the ranking and calls back with its position.
    public static IEnumerator SubmitScore(int points, int level, Action<int, string> done)
    {
        string body = JsonUtility.ToJson(new ScoreInput { puntos = points, nivel = level });
        yield return Send("POST", "puntuaciones", body, (error, text) =>
            done(error == null ? JsonUtility.FromJson<ScoreEntry>(text).posicion : 0, error));
    }

    public static IEnumerator GetRanking(int top, Action<ScoreEntry[], string> done)
    {
        yield return Send("GET", $"puntuaciones?top={top}", null, (error, text) =>
            done(error == null ? JsonUtility.FromJson<ScoreList>(WrapArray(text)).items : null, error));
    }

    static IEnumerator Send(string method, string path, string body, Action<string, string> done, Action<long> status = null)
    {
        using var request = new UnityWebRequest($"{BaseUrl}/{path}", method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 5 };
        if (body != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.SetRequestHeader("Content-Type", "application/json");
        }
        if (LoggedIn) request.SetRequestHeader("Authorization", "Bearer " + Token);

        yield return request.SendWebRequest();

        status?.Invoke(request.responseCode);
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            done("No se puede conectar con el servidor. Arráncalo con Servidor/iniciar-servidor.bat", null);
            yield break;
        }
        string text = request.downloadHandler.text;
        if (request.result == UnityWebRequest.Result.Success)
        {
            done(null, text);
            yield break;
        }
        // A rejected token means the session expired: forget it so the player logs in again.
        if (request.responseCode == 401 && LoggedIn) Logout();
        done(ErrorMessage(text, request.responseCode), text);
    }

    // The server always answers errors with { "mensaje": "..." }.
    static string ErrorMessage(string text, long status)
    {
        try
        {
            string message = JsonUtility.FromJson<ApiError>(text)?.mensaje;
            if (!string.IsNullOrEmpty(message)) return message;
        }
        catch (ArgumentException) { }
        return $"Error del servidor ({status})";
    }

    // JsonUtility can't read a JSON array on its own, so wrap it in an object.
    static string WrapArray(string json) => "{\"items\":" + json + "}";

    // Field names must match the JSON the server sends and expects.
    // (JsonUtility fills these fields, so the compiler's "never assigned" warning doesn't apply.)
#pragma warning disable 0649
    [Serializable] class Credentials { public string nombreUsuario, contrasena; }
    [Serializable] class Session { public string token, nombreUsuario; }
    [Serializable] class ApiError { public string mensaje; }
    [Serializable] class SavedGameList { public SavedGame[] items; }
    [Serializable] class ScoreInput { public int puntos, nivel; }
    [Serializable] class ScoreList { public ScoreEntry[] items; }
#pragma warning restore 0649
}

[Serializable]
public class SavedGame
{
    public int id, nivel, puntos, monedas, vidas;
    public string fechaGuardado;

    public SaveData ToSaveData() => new SaveData { nivel = nivel, puntos = puntos, monedas = monedas, vidas = vidas };
}

[Serializable]
public class ScoreEntry
{
    public int posicion, puntos, nivel;
    public string nombreUsuario;
}
