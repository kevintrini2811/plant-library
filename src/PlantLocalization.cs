namespace PlantLibrary;

public static class PlantLocalization
{
    public static bool German => ReplantedModUi.NativeLocalization.German;
    public static string? LanguageLabel() => ReplantedModUi.NativeLocalization.LanguageLabel();
    public static string Localize(string key) => ReplantedModUi.NativeLocalization.Localize(key);
}
