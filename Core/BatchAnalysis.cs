using CacheSimulator.IO;

namespace CacheSimulator.Core;

/// <summary>
/// Executa as 5 análises exigidas pelo TDE 2 de forma automática.
///
/// Parâmetros globais fixos (conforme enunciado):
///   Hit time       = 4 ns
///   Tempo MP       = 60 ns
///
/// Critério de estabilização (análises 1 e 4):
///   Para quando |hit_rate[i] - hit_rate[i-1]| &lt; 0,5% por 2 passos consecutivos.
/// </summary>
public static class BatchAnalysis
{
    public const int HitTimeNs = 4;
    public const int MpTimeNs  = 60;

    private const double StabilizationThreshold = 0.005; // 0,5%
    private const int    StabilizationWindow    = 2;
    private const int    MaxIterations          = 22;    // 8 × 2^22 = segurança

    // ── Análise 1: Impacto do Tamanho da Cache ───────────────────────────────
    // Fixos : bloco=128B, write-through, LRU, assoc=4
    // Variável: número de blocos (início em 8, potências de 2)

    public static List<AnalysisRow> CacheSizeImpact(string cacheFile)
    {
        var results  = new List<AnalysisRow>();
        var hitRates = new List<double>();
        int numLines = 8;

        for (int iter = 0; iter < MaxIterations; iter++, numLines *= 2)
        {
            var config = new CacheConfig
            {
                WritePolicy       = WritePolicy.WriteThrough,
                BlockSize         = 128,
                NumLines          = numLines,
                Associativity     = 4,
                HitTimeNs         = HitTimeNs,
                ReplacementPolicy = ReplacementPolicy.LRU,
                MainMemoryTimeNs  = MpTimeNs
            };

            var stats = RunSimulation(config, cacheFile);
            results.Add(new AnalysisRow("Nº Blocos", numLines.ToString(), config, stats));
            hitRates.Add(stats.GlobalHitRate);

            Console.Write($"\r  [{numLines,6} blocos | {config.CacheSizeBytes / 1024.0,6:F1} KB]" +
                          $"  Hit: {stats.GlobalHitRate:P2}   ");

            if (IsStabilized(hitRates)) break;
        }
        Console.WriteLine();
        return results;
    }

    // ── Análise 2: Impacto do Tamanho do Bloco ───────────────────────────────
    // Fixos   : cache=8KB, write-through, LRU, assoc=2
    // Variável: tamanho do bloco de 8 a 4096 bytes

    public static List<AnalysisRow> BlockSizeImpact(string cacheFile)
    {
        var results      = new List<AnalysisRow>();
        const int CacheB = 8 * 1024;
        const int Assoc  = 2;

        for (int blockSize = 8; blockSize <= 4096; blockSize *= 2)
        {
            int numLines = CacheB / blockSize;
            if (numLines < Assoc) break; // inválido: assoc > num_linhas

            var config = new CacheConfig
            {
                WritePolicy       = WritePolicy.WriteThrough,
                BlockSize         = blockSize,
                NumLines          = numLines,
                Associativity     = Assoc,
                HitTimeNs         = HitTimeNs,
                ReplacementPolicy = ReplacementPolicy.LRU,
                MainMemoryTimeNs  = MpTimeNs
            };

            var stats = RunSimulation(config, cacheFile);
            results.Add(new AnalysisRow("Tam. Bloco", $"{blockSize} B", config, stats));

            Console.Write($"\r  [Bloco {blockSize,5} B | {numLines,4} linhas]" +
                          $"  Hit: {stats.GlobalHitRate:P2}   ");
        }
        Console.WriteLine();
        return results;
    }

    // ── Análise 3: Impacto da Associatividade ────────────────────────────────
    // Fixos   : bloco=128B, write-back, LRU, cache=8KB (64 linhas)
    // Variável: associatividade de 1-way a 64-way

    public static List<AnalysisRow> AssociativityImpact(string cacheFile)
    {
        var results      = new List<AnalysisRow>();
        const int CacheB = 8 * 1024;
        const int BlockB = 128;
        int numLines     = CacheB / BlockB; // 64

        for (int assoc = 1; assoc <= numLines; assoc *= 2)
        {
            var config = new CacheConfig
            {
                WritePolicy       = WritePolicy.WriteBack,
                BlockSize         = BlockB,
                NumLines          = numLines,
                Associativity     = assoc,
                HitTimeNs         = HitTimeNs,
                ReplacementPolicy = ReplacementPolicy.LRU,
                MainMemoryTimeNs  = MpTimeNs
            };

            var stats = RunSimulation(config, cacheFile);
            results.Add(new AnalysisRow("Associat.", $"{assoc}-way", config, stats));

            Console.Write($"\r  [{assoc,3}-way | {config.NumSets,3} conjuntos]" +
                          $"  Hit: {stats.GlobalHitRate:P2}   ");
        }
        Console.WriteLine();
        return results;
    }

    // ── Análise 4: Impacto da Política de Substituição ───────────────────────
    // Fixos   : bloco=128B, write-through, assoc=4
    // Variável: nº de blocos (a partir de 16) × LRU vs Aleatória
    // Retorna dois lists paralelos (LRU e Random, índices espelhados)

    public static (List<AnalysisRow> Lru, List<AnalysisRow> Random)
        ReplacementPolicyImpact(string cacheFile)
    {
        var lruRows  = new List<AnalysisRow>();
        var rndRows  = new List<AnalysisRow>();
        var lruRates = new List<double>();
        var rndRates = new List<double>();
        int numLines = 16; // começa em 16 conforme enunciado

        for (int iter = 0; iter < MaxIterations; iter++, numLines *= 2)
        {
            var cfgLru = new CacheConfig
            {
                WritePolicy       = WritePolicy.WriteThrough,
                BlockSize         = 128,
                NumLines          = numLines,
                Associativity     = 4,
                HitTimeNs         = HitTimeNs,
                ReplacementPolicy = ReplacementPolicy.LRU,
                MainMemoryTimeNs  = MpTimeNs
            };
            var cfgRnd = new CacheConfig
            {
                WritePolicy       = WritePolicy.WriteThrough,
                BlockSize         = 128,
                NumLines          = numLines,
                Associativity     = 4,
                HitTimeNs         = HitTimeNs,
                ReplacementPolicy = ReplacementPolicy.Random,
                MainMemoryTimeNs  = MpTimeNs
            };

            var statsLru = RunSimulation(cfgLru, cacheFile);
            var statsRnd = RunSimulation(cfgRnd, cacheFile);

            lruRows.Add(new AnalysisRow("Nº Blocos", numLines.ToString(), cfgLru, statsLru));
            rndRows.Add(new AnalysisRow("Nº Blocos", numLines.ToString(), cfgRnd, statsRnd));
            lruRates.Add(statsLru.GlobalHitRate);
            rndRates.Add(statsRnd.GlobalHitRate);

            Console.Write($"\r  [{numLines,6} blocos | {cfgLru.CacheSizeBytes / 1024.0,5:F1} KB]" +
                          $"  LRU: {statsLru.GlobalHitRate:P2}  Aleat.: {statsRnd.GlobalHitRate:P2}   ");

            if (IsStabilized(lruRates) && IsStabilized(rndRates)) break;
        }
        Console.WriteLine();
        return (lruRows, rndRows);
    }

    // ── Análise 5: Largura de Banda da Memória ───────────────────────────────
    // Combinações: cache 8/16KB × blocos 64/128B × assoc 2/4 × write-through/back
    // Total: 2×2×2×2 = 16 simulações

    public static List<AnalysisRow> BandwidthAnalysis(string cacheFile)
    {
        var results = new List<AnalysisRow>();

        int[]         cacheSizes = [8 * 1024, 16 * 1024];
        int[]         blockSizes = [64, 128];
        int[]         assocs     = [2, 4];
        WritePolicy[] policies   = [WritePolicy.WriteThrough, WritePolicy.WriteBack];

        // Ordena por política para facilitar a comparação na tabela
        foreach (var policy    in policies)
        foreach (var cacheSize in cacheSizes)
        foreach (var blockSize in blockSizes)
        foreach (var assoc     in assocs)
        {
            int numLines = cacheSize / blockSize;
            if (numLines < assoc) continue;

            var config = new CacheConfig
            {
                WritePolicy       = policy,
                BlockSize         = blockSize,
                NumLines          = numLines,
                Associativity     = assoc,
                HitTimeNs         = HitTimeNs,
                ReplacementPolicy = ReplacementPolicy.LRU,
                MainMemoryTimeNs  = MpTimeNs
            };

            var stats  = RunSimulation(config, cacheFile);
            string lbl = $"{cacheSize / 1024}KB/{blockSize}B/{assoc}-way/{policy}";
            results.Add(new AnalysisRow("Config.", lbl, config, stats));

            Console.Write($"\r  {lbl,-36}" +
                          $"  Leit: {stats.MainMemoryReads,8}  Escrit: {stats.MainMemoryWrites,8}   ");
        }
        Console.WriteLine();
        return results;
    }

    // ── Helper: executa uma simulação completa ────────────────────────────────

    public static SimulationStats RunSimulation(CacheConfig config, string cacheFile)
    {
        var engine = new CacheEngine(config);
        foreach (var access in AddressFileReader.Read(cacheFile))
            engine.Access(access.Address, access.IsWrite);
        return engine.Stats;
    }

    // ── Helper: verifica estabilização da hit rate ────────────────────────────
    // Estável quando os últimos `StabilizationWindow` deltas consecutivos
    // são todos menores que `StabilizationThreshold`.

    private static bool IsStabilized(List<double> rates)
    {
        if (rates.Count < StabilizationWindow + 1) return false;
        for (int i = rates.Count - StabilizationWindow; i < rates.Count; i++)
            if (Math.Abs(rates[i] - rates[i - 1]) >= StabilizationThreshold) return false;
        return true;
    }
}
