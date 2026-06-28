using System.Globalization;
using CacheSimulator.Core;

namespace CacheSimulator.IO;

/// <summary>
/// Exporta resultados para CSV, um arquivo por análise.
/// Usa InvariantCulture em todos os números para garantir '.' como separador decimal,
/// independentemente do locale do sistema (evita corrupção de CSV no pt-BR).
/// </summary>
public static class ResultExporter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    // ── Modo Manual: resultado de uma única simulação ─────────────────────────

    public static void ExportManual(string path, CacheConfig cfg, SimulationStats stats)
    {
        EnsureDir(path);
        double amat = stats.ComputeAMAT(cfg.HitTimeNs, cfg.MainMemoryTimeNs);

        using var w = Writer(path);
        w.WriteLine("# Parâmetros");
        w.WriteLine($"Política de escrita,{cfg.WritePolicy}");
        w.WriteLine($"Tamanho do bloco (bytes),{cfg.BlockSize}");
        w.WriteLine($"Número de linhas,{cfg.NumLines}");
        w.WriteLine($"Associatividade,{cfg.Associativity}");
        w.WriteLine($"Número de conjuntos,{cfg.NumSets}");
        w.WriteLine($"Tamanho da cache (bytes),{cfg.CacheSizeBytes}");
        w.WriteLine($"Hit time (ns),{cfg.HitTimeNs}");
        w.WriteLine($"Tempo MP (ns),{cfg.MainMemoryTimeNs}");
        w.WriteLine($"Política de substituição,{cfg.ReplacementPolicy}");
        w.WriteLine();
        w.WriteLine("# Resultados");
        w.WriteLine($"Total de acessos,{stats.TotalAccesses}");
        w.WriteLine($"Total de leituras,{stats.TotalReads}");
        w.WriteLine($"Total de escritas,{stats.TotalWrites}");
        w.WriteLine($"Leituras na MP,{stats.MainMemoryReads}");
        w.WriteLine($"Escritas na MP,{stats.MainMemoryWrites}");
        w.WriteLine(string.Create(Inv, $"Hit rate leitura,{stats.ReadHitRate:F6}"));
        w.WriteLine(string.Create(Inv, $"Hit rate escrita,{stats.WriteHitRate:F6}"));
        w.WriteLine(string.Create(Inv, $"Hit rate global,{stats.GlobalHitRate:F6}"));
        w.WriteLine(string.Create(Inv, $"AMAT (ns),{amat:F4}"));
    }

    // ── Análise 1: Impacto do Tamanho da Cache ────────────────────────────────

    public static void ExportAnalysis1(string path, List<AnalysisRow> rows)
    {
        EnsureDir(path);
        using var w = Writer(path);

        w.WriteLine("# Análise 1 — Impacto do Tamanho da Cache");
        w.WriteLine("# Fixos: BlockSize=128B | WriteThrough | LRU | Assoc=4 | HitTime=4ns | MP=60ns");
        w.WriteLine();
        w.WriteLine("Nº Blocos,Tam. Cache (bytes),Tam. Cache (KB),Hit Rate Global,AMAT (ns),Leituras MP,Escritas MP");

        foreach (var r in rows)
        {
            double amat = r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs);
            w.WriteLine(string.Create(Inv,
                $"{r.Config.NumLines},{r.Config.CacheSizeBytes},{r.Config.CacheSizeBytes / 1024.0:F2}," +
                $"{r.Stats.GlobalHitRate:F6},{amat:F4}," +
                $"{r.Stats.MainMemoryReads},{r.Stats.MainMemoryWrites}"));
        }
    }

    // ── Análise 2: Impacto do Tamanho do Bloco ────────────────────────────────

    public static void ExportAnalysis2(string path, List<AnalysisRow> rows)
    {
        EnsureDir(path);
        using var w = Writer(path);

        w.WriteLine("# Análise 2 — Impacto do Tamanho do Bloco");
        w.WriteLine("# Fixos: Cache=8KB | WriteThrough | LRU | Assoc=2 | HitTime=4ns | MP=60ns");
        w.WriteLine();
        w.WriteLine("Tam. Bloco (B),Nº Linhas,Hit Rate Global,AMAT (ns),Leituras MP,Escritas MP");

        foreach (var r in rows)
        {
            double amat = r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs);
            w.WriteLine(string.Create(Inv,
                $"{r.Config.BlockSize},{r.Config.NumLines}," +
                $"{r.Stats.GlobalHitRate:F6},{amat:F4}," +
                $"{r.Stats.MainMemoryReads},{r.Stats.MainMemoryWrites}"));
        }
    }

    // ── Análise 3: Impacto da Associatividade ─────────────────────────────────

    public static void ExportAnalysis3(string path, List<AnalysisRow> rows)
    {
        EnsureDir(path);
        using var w = Writer(path);

        w.WriteLine("# Análise 3 — Impacto da Associatividade");
        w.WriteLine("# Fixos: BlockSize=128B | WriteBack | LRU | Cache=8KB (64 linhas) | HitTime=4ns | MP=60ns");
        w.WriteLine();
        w.WriteLine("Associatividade,Nº Conjuntos,Hit Rate Global,AMAT (ns),Leituras MP,Escritas MP");

        foreach (var r in rows)
        {
            double amat = r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs);
            w.WriteLine(string.Create(Inv,
                $"{r.Config.Associativity},{r.Config.NumSets}," +
                $"{r.Stats.GlobalHitRate:F6},{amat:F4}," +
                $"{r.Stats.MainMemoryReads},{r.Stats.MainMemoryWrites}"));
        }
    }

    // ── Análise 4: Impacto da Política de Substituição ────────────────────────

    public static void ExportAnalysis4(string path, List<AnalysisRow> lruRows, List<AnalysisRow> rndRows)
    {
        EnsureDir(path);
        using var w = Writer(path);

        w.WriteLine("# Análise 4 — Impacto da Política de Substituição");
        w.WriteLine("# Fixos: BlockSize=128B | WriteThrough | Assoc=4 | HitTime=4ns | MP=60ns");
        w.WriteLine();
        w.WriteLine("Nº Blocos,Tam. Cache (bytes),Tam. Cache (KB)," +
                    "Hit Rate LRU,AMAT LRU (ns)," +
                    "Hit Rate Aleat.,AMAT Aleat. (ns)");

        for (int i = 0; i < lruRows.Count; i++)
        {
            var lru     = lruRows[i];
            var rnd     = rndRows[i];
            double aLru = lru.Stats.ComputeAMAT(lru.Config.HitTimeNs, lru.Config.MainMemoryTimeNs);
            double aRnd = rnd.Stats.ComputeAMAT(rnd.Config.HitTimeNs, rnd.Config.MainMemoryTimeNs);

            w.WriteLine(string.Create(Inv,
                $"{lru.Config.NumLines},{lru.Config.CacheSizeBytes},{lru.Config.CacheSizeBytes / 1024.0:F2}," +
                $"{lru.Stats.GlobalHitRate:F6},{aLru:F4}," +
                $"{rnd.Stats.GlobalHitRate:F6},{aRnd:F4}"));
        }
    }

    // ── Análise 5: Largura de Banda da Memória ────────────────────────────────
    // Inclui linha de média ao final, conforme solicitado pelo enunciado.

    public static void ExportAnalysis5(string path, List<AnalysisRow> rows)
    {
        EnsureDir(path);
        using var w = Writer(path);

        w.WriteLine("# Análise 5 — Largura de Banda da Memória");
        w.WriteLine("# Combinações: cache 8/16KB × blocos 64/128B × assoc 2/4 × write-through/write-back | LRU | HitTime=4ns | MP=60ns");
        w.WriteLine();
        w.WriteLine("Política,Tam. Cache (KB),Tam. Bloco (B),Assoc.,Leituras MP,Escritas MP,Total MP");

        long sumR = 0, sumW = 0;
        foreach (var r in rows)
        {
            long total = r.Stats.MainMemoryReads + r.Stats.MainMemoryWrites;
            w.WriteLine($"{r.Config.WritePolicy},{r.Config.CacheSizeBytes / 1024}," +
                        $"{r.Config.BlockSize},{r.Config.Associativity}," +
                        $"{r.Stats.MainMemoryReads},{r.Stats.MainMemoryWrites},{total}");
            sumR += r.Stats.MainMemoryReads;
            sumW += r.Stats.MainMemoryWrites;
        }

        if (rows.Count > 0)
        {
            double avgR = (double)sumR / rows.Count;
            double avgW = (double)sumW / rows.Count;
            w.WriteLine(string.Create(Inv, $"MÉDIA,,,, {avgR:F1},{avgW:F1},{avgR + avgW:F1}"));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnsureDir(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    private static StreamWriter Writer(string path) =>
        new(path, append: false, System.Text.Encoding.UTF8);
}
