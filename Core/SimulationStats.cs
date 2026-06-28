namespace CacheSimulator.Core;

/// <summary>
/// Acumula todas as métricas de desempenho durante a simulação.
/// </summary>
public class SimulationStats
{
    // ── Contadores brutos ────────────────────────────────────────────────────
    public long ReadHits         { get; private set; }
    public long ReadMisses       { get; private set; }
    public long WriteHits        { get; private set; }
    public long WriteMisses      { get; private set; }
    public long MainMemoryReads  { get; private set; }
    public long MainMemoryWrites { get; private set; }

    // ── Totais derivados ─────────────────────────────────────────────────────
    public long TotalReads    => ReadHits + ReadMisses;
    public long TotalWrites   => WriteHits + WriteMisses;
    public long TotalAccesses => TotalReads + TotalWrites;

    // ── Taxas de acerto ──────────────────────────────────────────────────────
    public double ReadHitRate   => TotalReads    == 0 ? 0 : (double)ReadHits  / TotalReads;
    public double WriteHitRate  => TotalWrites   == 0 ? 0 : (double)WriteHits / TotalWrites;
    public double GlobalHitRate => TotalAccesses == 0 ? 0 : (double)(ReadHits + WriteHits) / TotalAccesses;

    // ── AMAT (Average Memory Access Time) ───────────────────────────────────
    // Fórmula: AMAT = T_hit + (1 - hit_rate) × T_miss_penalty
    public double ComputeAMAT(int hitTimeNs, int mainMemoryTimeNs) =>
        hitTimeNs + (1.0 - GlobalHitRate) * mainMemoryTimeNs;

    // ── Métodos de registro ──────────────────────────────────────────────────
    public void RecordReadHit()          => ReadHits++;
    public void RecordReadMiss()         => ReadMisses++;
    public void RecordWriteHit()         => WriteHits++;
    public void RecordWriteMiss()        => WriteMisses++;
    public void RecordMainMemoryRead()   => MainMemoryReads++;
    public void RecordMainMemoryWrite()  => MainMemoryWrites++;

    // ── Formatação ───────────────────────────────────────────────────────────
    public void Print(CacheConfig config)
    {
        double amat = ComputeAMAT(config.HitTimeNs, config.MainMemoryTimeNs);

        Console.WriteLine();
        Console.WriteLine("  ┌─────────────────────────────────────────┐");
        Console.WriteLine("  │           RESULTADOS DA SIMULAÇÃO        │");
        Console.WriteLine("  ├─────────────────────────────────────────┤");
        Console.WriteLine($"  │  Total de acessos     : {TotalAccesses,10}       │");
        Console.WriteLine($"  │    Leituras           : {TotalReads,10}       │");
        Console.WriteLine($"  │    Escritas           : {TotalWrites,10}       │");
        Console.WriteLine("  ├─────────────────────────────────────────┤");
        Console.WriteLine($"  │  Acessos MP (leitura) : {MainMemoryReads,10}       │");
        Console.WriteLine($"  │  Acessos MP (escrita) : {MainMemoryWrites,10}       │");
        Console.WriteLine("  ├─────────────────────────────────────────┤");
        Console.WriteLine($"  │  Hit rate (leitura)   : {ReadHitRate,9:P4}      │");
        Console.WriteLine($"  │  Hit rate (escrita)   : {WriteHitRate,9:P4}      │");
        Console.WriteLine($"  │  Hit rate (global)    : {GlobalHitRate,9:P4}      │");
        Console.WriteLine("  ├─────────────────────────────────────────┤");
        Console.WriteLine($"  │  Tempo médio (AMAT)   : {amat,9:F4} ns      │");
        Console.WriteLine("  └─────────────────────────────────────────┘");
    }
}
