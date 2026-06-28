namespace CacheSimulator.Core;

/// <summary>
/// Representa um conjunto (set) da cache associativa por conjunto.
/// Contém "Associativity" linhas.
/// </summary>
public class CacheSet
{
    private readonly CacheLine[] _lines;

    public CacheSet(int associativity)
    {
        _lines = new CacheLine[associativity];

        for (int i = 0; i < associativity; i++)
            _lines[i] = new CacheLine();
    }

    /// <summary>Busca uma linha válida com a tag especificada. Retorna null se não encontrar (miss).</summary>
    public CacheLine? FindLine(uint tag) =>
        Array.Find(_lines, l => l.Valid && l.Tag == tag);

    /// <summary>Retorna a primeira linha inválida disponível, ou null se todas estiverem ocupadas.</summary>
    public CacheLine? FindInvalidLine() =>
        Array.Find(_lines, l => !l.Valid);

    /// <summary>Retorna a linha com menor LruCounter — a candidata à substituição por LRU.</summary>
    public CacheLine GetLruVictim() =>
        _lines.MinBy(l => l.LruCounter)!;

    /// <summary>Retorna uma linha aleatória para substituição.</summary>
    public CacheLine GetRandomVictim() =>
        _lines[Random.Shared.Next(_lines.Length)];

    /// <summary>
    /// Atualiza o contador LRU da linha acessada.
    /// Incrementa em relação ao maior contador atual do conjunto.
    /// </summary>
    public void UpdateLru(CacheLine accessed)
    {
        int max = _lines.Max(l => l.LruCounter);
        accessed.LruCounter = max + 1;
    }

    public IReadOnlyList<CacheLine> Lines => _lines;
}
