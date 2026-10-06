using System.Text.Json;
using System.Text.Json.Serialization;
using AnalisiImmobiliare.Core.Modelli;

namespace AnalisiImmobiliare.Core.Servizi;

/// <summary>Salva e apre gli scenari come file JSON leggibili (estensione .immo).</summary>
public static class ArchivioScenari
{
    public const string Estensione = ".immo";

    private static readonly JsonSerializerOptions Opzioni = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string InJson(ScenarioInvestimento scenario) => JsonSerializer.Serialize(scenario, Opzioni);

    public static ScenarioInvestimento DaJson(string json) =>
        JsonSerializer.Deserialize<ScenarioInvestimento>(json, Opzioni)
        ?? throw new InvalidDataException("Il file non contiene uno scenario valido.");

    public static void Salva(ScenarioInvestimento scenario, string percorso) =>
        File.WriteAllText(percorso, InJson(scenario));

    public static ScenarioInvestimento Apri(string percorso) => DaJson(File.ReadAllText(percorso));
}
