namespace CacheSimulator.Core;

public enum WritePolicy { WriteThrough = 0, WriteBack = 1 }
public enum ReplacementPolicy { LRU, Random }

/// <summary>
/// Todos os parâmetros de configuração da cache + lógica de decomposição de endereço.
/// </summary>
public class CacheConfig
{
    // ── Parâmetros de entrada ────────────────────────────────────────────────
    public WritePolicy WritePolicy { get; init; }
    public int BlockSize { get; init; }          // bytes — potência de 2
    public int NumLines { get; init; }           // total de linhas — potência de 2
    public int Associativity { get; init; }      // linhas por conjunto — potência de 2
    public int HitTimeNs { get; init; }          // tempo de acerto (ns)
    public ReplacementPolicy ReplacementPolicy { get; init; }
    public int MainMemoryTimeNs { get; init; }   // tempo de leitura/escrita na MP (ns)

    // ── Valores derivados ────────────────────────────────────────────────────
    public int NumSets    => NumLines / Associativity;
    public int OffsetBits => Log2(BlockSize);
    public int IndexBits  => NumSets > 1 ? Log2(NumSets) : 0;
    public int TagBits    => 32 - IndexBits - OffsetBits;
    public int CacheSizeBytes => NumLines * BlockSize;

    // ── Decomposição de endereço (32 bits) ───────────────────────────────────
    //  [ TAG (TagBits) | INDEX (IndexBits) | OFFSET (OffsetBits) ]

    public uint GetOffset(uint address) =>
        address & Mask(OffsetBits);

    public uint GetIndex(uint address) =>
        (address >> OffsetBits) & Mask(IndexBits);

    public uint GetTag(uint address) =>
        address >> (OffsetBits + IndexBits);

    // ── Validação ────────────────────────────────────────────────────────────
    public void Validate()
    {
        if (!IsPowerOfTwo(BlockSize) || BlockSize < 1)
            throw new ArgumentException($"BlockSize ({BlockSize}) deve ser potência de 2.");

        if (!IsPowerOfTwo(NumLines) || NumLines < 1)
            throw new ArgumentException($"NumLines ({NumLines}) deve ser potência de 2.");

        if (!IsPowerOfTwo(Associativity) || Associativity < 1 || Associativity > NumLines)
            throw new ArgumentException($"Associativity ({Associativity}) deve ser potência de 2 entre 1 e {NumLines}.");

        if (HitTimeNs <= 0)
            throw new ArgumentException("HitTimeNs deve ser positivo.");

        if (MainMemoryTimeNs <= 0)
            throw new ArgumentException("MainMemoryTimeNs deve ser positivo.");

        if (TagBits <= 0)
            throw new ArgumentException("Configuração inválida: TagBits resultou em zero ou negativo.");
    }

    // ── Exibição ─────────────────────────────────────────────────────────────
    public void PrintDecomposition()
    {
        Console.WriteLine($"  Tamanho do bloco    : {BlockSize} bytes");
        Console.WriteLine($"  Número de linhas    : {NumLines}");
        Console.WriteLine($"  Associatividade     : {Associativity}-way");
        Console.WriteLine($"  Número de conjuntos : {NumSets}");
        Console.WriteLine($"  Tamanho da cache    : {CacheSizeBytes} bytes ({CacheSizeBytes / 1024.0:F1} KB)");
        Console.WriteLine($"  Bits de offset      : {OffsetBits}");
        Console.WriteLine($"  Bits de índice      : {IndexBits}");
        Console.WriteLine($"  Bits de tag         : {TagBits}");
        Console.WriteLine($"  Política de escrita : {WritePolicy}");
        Console.WriteLine($"  Substituição        : {ReplacementPolicy}");
        Console.WriteLine($"  Hit time            : {HitTimeNs} ns");
        Console.WriteLine($"  Tempo MP            : {MainMemoryTimeNs} ns");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static uint Mask(int bits) =>
        bits == 0 ? 0u : (uint)((1 << bits) - 1);

    private static int Log2(int n) =>
        (int)Math.Log2(n);

    private static bool IsPowerOfTwo(int n) =>
        n > 0 && (n & (n - 1)) == 0;
}
