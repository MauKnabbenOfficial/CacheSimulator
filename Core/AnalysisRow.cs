namespace CacheSimulator.Core;

/// <summary>
/// Resultado de uma única execução dentro de uma análise batch.
/// Agrupa o parâmetro que variou, a configuração usada e as estatísticas geradas.
/// </summary>
public record AnalysisRow(
    string ParamName,    // Ex: "Nº Blocos", "Tam. Bloco"
    string ParamValue,   // Ex: "64", "128 B"
    CacheConfig Config,
    SimulationStats Stats
);
