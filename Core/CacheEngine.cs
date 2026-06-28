namespace CacheSimulator.Core;

/// <summary>
/// Motor principal da simulação. Processa acessos de leitura e escrita
/// aplicando as políticas configuradas (write-through/back, LRU/Random).
/// </summary>
public class CacheEngine
{
    private readonly CacheConfig _config;
    private readonly CacheSet[]  _sets;

    public SimulationStats Stats { get; } = new();

    public CacheEngine(CacheConfig config)
    {
        config.Validate();
        _config = config;
        _sets   = new CacheSet[config.NumSets];
        for (int i = 0; i < config.NumSets; i++)
            _sets[i] = new CacheSet(config.Associativity);
    }

    // ── Ponto de entrada ─────────────────────────────────────────────────────

    public void Access(uint address, bool isWrite)
    {
        if (isWrite) Write(address);
        else         Read(address);
    }

    // ── Leitura ──────────────────────────────────────────────────────────────

    private void Read(uint address)
    {
        var (set, tag) = Locate(address);
        var line       = set.FindLine(tag);

        if (line is not null)
        {
            // HIT: atualiza LRU e retorna
            Stats.RecordReadHit();
            set.UpdateLru(line);
        }
        else
        {
            // MISS: carrega bloco da memória principal
            Stats.RecordReadMiss();
            Stats.RecordMainMemoryRead();
            LoadBlock(set, tag);
        }
    }

    // ── Escrita ──────────────────────────────────────────────────────────────

    private void Write(uint address)
    {
        var (set, tag) = Locate(address);
        var line       = set.FindLine(tag);

        if (line is not null)
        {
            // HIT de escrita
            Stats.RecordWriteHit();
            set.UpdateLru(line);

            if (_config.WritePolicy == WritePolicy.WriteThrough)
            {
                // Write-through: escreve na MP imediatamente (sempre sincronizadas)
                Stats.RecordMainMemoryWrite();
            }
            else
            {
                // Write-back: marca dirty; MP será atualizada só na substituição
                line.Dirty = true;
            }
        }
        else
        {
            // MISS de escrita
            Stats.RecordWriteMiss();

            if (_config.WritePolicy == WritePolicy.WriteThrough)
            {
                // Write non-allocate: escreve direto na MP, NÃO carrega o bloco
                Stats.RecordMainMemoryWrite();
            }
            else
            {
                // Write-allocate: carrega o bloco, depois marca dirty
                Stats.RecordMainMemoryRead();
                var loaded = LoadBlock(set, tag);
                loaded.Dirty = true;
            }
        }
    }

    // ── Carregamento de bloco ────────────────────────────────────────────────

    /// <summary>
    /// Carrega um bloco na cache. Usa linha inválida se houver; caso contrário, substitui.
    /// </summary>
    private CacheLine LoadBlock(CacheSet set, uint tag)
    {
        var target = set.FindInvalidLine() ?? Evict(set);
        target.Valid      = true;
        target.Tag        = tag;
        target.Dirty      = false;
        set.UpdateLru(target);
        return target;
    }

    // ── Substituição (evicção) ───────────────────────────────────────────────

    /// <summary>
    /// Escolhe a vítima conforme a política configurada.
    /// Se write-back e dirty, escreve na MP antes de liberar a linha.
    /// </summary>
    private CacheLine Evict(CacheSet set)
    {
        var victim = _config.ReplacementPolicy == ReplacementPolicy.LRU
            ? set.GetLruVictim()
            : set.GetRandomVictim();

        if (victim.Dirty && _config.WritePolicy == WritePolicy.WriteBack)
            Stats.RecordMainMemoryWrite(); // write-back: flush do bloco sujo

        victim.Invalidate();
        return victim;
    }

    // ── Helper: decompõe endereço ────────────────────────────────────────────

    private (CacheSet set, uint tag) Locate(uint address)
    {
        uint index = _config.GetIndex(address);
        uint tag   = _config.GetTag(address);
        return (_sets[index], tag);
    }
}
