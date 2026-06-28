namespace CacheSimulator.IO;

/// <summary>Representa um único acesso à memória lido do arquivo .cache.</summary>
public record MemoryAccess(uint Address, bool IsWrite);

/// <summary>
/// Lê e interpreta o arquivo de endereços no formato:
///   [endereço hex 32 bits] [R|W]
/// Exemplo:
///   0020a858 R
///   05fea840 W
/// </summary>
public static class AddressFileReader
{
    public static IEnumerable<MemoryAccess> Read(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

        int lineNumber = 0;
        foreach (var rawLine in File.ReadLines(filePath))
        {
            lineNumber++;
            var trimmed = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                Console.Error.WriteLine($"[Aviso] Linha {lineNumber} ignorada (formato inválido): \"{rawLine}\"");
                continue;
            }

            if (!uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out uint address))
            {
                Console.Error.WriteLine($"[Aviso] Linha {lineNumber}: endereço inválido \"{parts[0]}\"");
                continue;
            }

            bool isWrite = parts[1].Equals("W", StringComparison.OrdinalIgnoreCase);

            yield return new MemoryAccess(address, isWrite);
        }
    }

    /// <summary>Conta o total de entradas do arquivo sem processá-las (para exibir progresso).</summary>
    public static int CountLines(string filePath) =>
        File.ReadLines(filePath).Count(l => !string.IsNullOrWhiteSpace(l));
}
