using System.Text.Json;
using EyeBreak.Models;

namespace EyeBreak.Services;

public sealed class SettingsStore
{
    public string FilePath { get; }

    public SettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), "EyeBreak", "settings.json");
    }

    public AppSettings Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                AppSettings? saved = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (saved != null) return saved.Validated();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Invalid or inaccessible configuration must not prevent the tray from starting.
            }
        }
        AppSettings defaults = new();
        try { Save(defaults); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        return defaults;
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
        string temp = FilePath + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(settings.Validated(),
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, FilePath, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { }
        }
    }
}
