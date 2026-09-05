// "Provided algorithm" for Lab 3.2 -- a standalone, deliberately naive
// in-memory confidential/non-confidential data server, written the way a
// developer unfamiliar with the lessons of Lab 3.1 might reasonably write
// one: no framework-level validation, a hand-rolled reversible cipher with
// a hardcoded key, an always-growing debug log, and no synchronization on
// shared state. It is analyzed (not fixed) by the Lab 3.2 report using the
// methodology built from Lab 3.1's findings.
using System.Net;
using System.Text;
using System.Text.Json;

var confidential = new Dictionary<int, string>();   // id -> base64 "ciphertext"
var confidentialTitles = new Dictionary<int, string>();
var publicData = new Dictionary<int, (string Title, string Content)>();
var activityLog = new List<string>();                // never trimmed, never cleared
var nextConfId = 0;
var nextPubId = 0;

var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:5090/");
listener.Start();
Console.WriteLine("Provided algorithm listening on http://localhost:5090/");

while (true)
{
    var ctx = listener.GetContext();
    _ = Task.Run(() => Handle(ctx));
}

void Handle(HttpListenerContext ctx)
{
    try
    {
        var path = ctx.Request.Url!.AbsolutePath;
        var method = ctx.Request.HttpMethod;
        using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
        var body = reader.ReadToEnd();

        if (path == "/confidential" && method == "POST")
        {
            // No model validation at all: wrong types, missing fields, and
            // oversized payloads all surface only as whatever generic
            // exception System.Text.Json or a null-reference happens to
            // throw, caught below as an undifferentiated 500.
            var doc = JsonSerializer.Deserialize<Dictionary<string, string>>(body)!;
            var title = doc["title"];
            var content = doc["content"];

            // "Encryption": XOR with a key hardcoded in source -- reversible
            // by anyone who reads the code or decompiles the binary, with no
            // nonce (the same plaintext always yields the same ciphertext).
            var cipher = XorObfuscate(content);

            // Debug/audit convenience that leaks the very thing it should
            // protect: the plaintext confidential value is appended here and
            // never removed for the lifetime of the process.
            activityLog.Add($"{DateTime.UtcNow:o} CREATE confidential: title='{title}' content='{content}'");

            // No synchronization: concurrent requests can race on nextConfId++
            // and on the plain Dictionary indexer.
            var id = nextConfId++;
            confidential[id] = cipher;
            confidentialTitles[id] = title;

            Respond(ctx, 201, JsonSerializer.Serialize(new { id, title, content }));
        }
        else if (path.StartsWith("/confidential/") && method == "PUT")
        {
            var id = int.Parse(path["/confidential/".Length..]);
            var doc = JsonSerializer.Deserialize<Dictionary<string, string>>(body)!;
            var title = doc["title"];
            var content = doc["content"];
            activityLog.Add($"{DateTime.UtcNow:o} UPDATE confidential#{id}: title='{title}' content='{content}'");
            confidential[id] = XorObfuscate(content);
            confidentialTitles[id] = title;
            Respond(ctx, 200, JsonSerializer.Serialize(new { id, title, content }));
        }
        else if (path.StartsWith("/confidential/") && method == "DELETE")
        {
            var id = int.Parse(path["/confidential/".Length..]);
            activityLog.Add($"{DateTime.UtcNow:o} DELETE confidential#{id}");
            confidential.Remove(id);
            confidentialTitles.Remove(id);
            Respond(ctx, 204, "");
        }
        else if (path == "/confidential" && method == "GET")
        {
            var items = confidential.Select(kv => new
            {
                id = kv.Key,
                title = confidentialTitles[kv.Key],
                content = XorObfuscate(kv.Value, alreadyBase64: true), // decrypt (XOR is its own inverse)
            });
            Respond(ctx, 200, JsonSerializer.Serialize(items));
        }
        else if (path == "/public" && method == "POST")
        {
            var doc = JsonSerializer.Deserialize<Dictionary<string, string>>(body)!;
            var title = doc["title"];
            var content = doc["content"];
            var id = nextPubId++;
            publicData[id] = (title, content);
            Respond(ctx, 201, JsonSerializer.Serialize(new { id, title, content }));
        }
        else if (path == "/public" && method == "GET")
        {
            var items = publicData.Select(kv => new { id = kv.Key, kv.Value.Title, kv.Value.Content });
            Respond(ctx, 200, JsonSerializer.Serialize(items));
        }
        else
        {
            Respond(ctx, 404, "");
        }
    }
    catch (Exception ex)
    {
        // The one and only error-handling path in the whole program: every
        // validation failure, type mismatch, missing field, or malformed
        // JSON ends up here, indistinguishable from every other one, as a
        // bare 500 with the raw exception message.
        Console.Error.WriteLine($"UNHANDLED: {ex}");
        try { Respond(ctx, 500, ex.Message); } catch { /* connection may already be gone */ }
    }
}

string XorObfuscate(string text, bool alreadyBase64 = false)
{
    var key = "my-static-secret-key-123"u8.ToArray();
    var bytes = alreadyBase64 ? Convert.FromBase64String(text) : Encoding.UTF8.GetBytes(text);
    for (var i = 0; i < bytes.Length; i++) bytes[i] ^= key[i % key.Length];
    return alreadyBase64 ? Encoding.UTF8.GetString(bytes) : Convert.ToBase64String(bytes);
}

void Respond(HttpListenerContext ctx, int status, string body)
{
    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/json";
    var bytes = Encoding.UTF8.GetBytes(body);
    ctx.Response.ContentLength64 = bytes.Length;
    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
    ctx.Response.OutputStream.Close();
}
