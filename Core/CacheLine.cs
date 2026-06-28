namespace CacheSimulator.Core;

/// <summary>
/// Representa uma linha (bloco) dentro da cache.
/// Não armazena os dados em si — apenas os metadados de controle.
/// </summary>
public class CacheLine
{
    /// <summary>Indica se a linha contém dados válidos.</summary>
    public bool Valid { get; set; }

    /// <summary>
    /// Indica se a linha foi modificada mas ainda não escrita na MP.
    /// Relevante apenas na política write-back.
    /// </summary>
    public bool Dirty { get; set; }

    /// <summary>Parte do endereço que identifica o bloco de memória.</summary>
    public uint Tag { get; set; }

    /// <summary>
    /// Contador para política LRU.
    /// Maior valor = acesso mais recente.
    /// </summary>
    public int LruCounter { get; set; }

    /// <summary>Reseta a linha para o estado inicial (inválida, limpa).</summary>
    public void Invalidate()
    {
        Valid      = false;
        Dirty      = false;
        Tag        = 0;
        LruCounter = 0;
    }
}
