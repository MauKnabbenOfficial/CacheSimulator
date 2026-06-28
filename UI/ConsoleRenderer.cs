using CacheSimulator.Core;

namespace CacheSimulator.UI;

/// <summary>
/// Utilitários de interface para o console: menus, tabelas, progress bar, inputs validados.
/// </summary>
public static class ConsoleRenderer
{
    private const int Width = 66;

    // ── Banner ────────────────────────────────────────────────────────────────

    public static void PrintBanner()
    {
        Console.Clear();
        Colored(ConsoleColor.Cyan, () =>
        {
            Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("  ║                                                              ║");
            Console.WriteLine("  ║         Cache Simulator — Fundamentos de Arq. SW            ║");
            Console.WriteLine("  ║                      TDE 2 — 2026/1                         ║");
            Console.WriteLine("  ║                                                              ║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
        });
        Console.WriteLine();
    }

    // ── Section header ────────────────────────────────────────────────────────

    public static void PrintSectionHeader(string title)
    {
        int pad = Math.Max(0, Width - title.Length - 6);
        Console.WriteLine();
        Colored(ConsoleColor.Yellow, () =>
            Console.WriteLine($"  ═══ {title} {new string('═', pad)}"));
        Console.WriteLine();
    }

    // ── Separator ─────────────────────────────────────────────────────────────

    public static void PrintSeparator(char ch = '─')
    {
        Console.WriteLine($"  {new string(ch, Width - 2)}");
    }

    // ── Config summary ────────────────────────────────────────────────────────

    public static void PrintConfig(CacheConfig config)
    {
        Console.WriteLine($"  Política de escrita : {config.WritePolicy}");
        Console.WriteLine($"  Tamanho do bloco    : {config.BlockSize} bytes");
        Console.WriteLine($"  Número de linhas    : {config.NumLines}");
        Console.WriteLine($"  Associatividade     : {config.Associativity}-way  ({config.NumSets} conjuntos)");
        Console.WriteLine($"  Tamanho da cache    : {config.CacheSizeBytes} bytes  ({config.CacheSizeBytes / 1024.0:F2} KB)");
        Console.WriteLine($"  Decomposição 32b    : {config.TagBits} tag  |  {config.IndexBits} índice  |  {config.OffsetBits} offset");
        Console.WriteLine($"  Hit time            : {config.HitTimeNs} ns");
        Console.WriteLine($"  Tempo MP            : {config.MainMemoryTimeNs} ns");
        Console.WriteLine($"  Substituição        : {config.ReplacementPolicy}");
    }

    // ── Stats summary ─────────────────────────────────────────────────────────

    public static void PrintStats(SimulationStats stats, CacheConfig config)
    {
        double amat = stats.ComputeAMAT(config.HitTimeNs, config.MainMemoryTimeNs);

        PrintSeparator();
        Console.WriteLine($"  Total de acessos     : {stats.TotalAccesses,12:N0}");
        Console.WriteLine($"    Leituras           : {stats.TotalReads,12:N0}");
        Console.WriteLine($"    Escritas           : {stats.TotalWrites,12:N0}");
        PrintSeparator();
        Console.WriteLine($"  Leituras na MP       : {stats.MainMemoryReads,12:N0}");
        Console.WriteLine($"  Escritas na MP       : {stats.MainMemoryWrites,12:N0}");
        PrintSeparator();
        Colored(ConsoleColor.Green, () =>
        {
            Console.WriteLine($"  Hit rate (leitura)   : {stats.ReadHitRate,10:P4}   ({stats.ReadHits:N0} / {stats.TotalReads:N0})");
            Console.WriteLine($"  Hit rate (escrita)   : {stats.WriteHitRate,10:P4}   ({stats.WriteHits:N0} / {stats.TotalWrites:N0})");
            Console.WriteLine($"  Hit rate (global)    : {stats.GlobalHitRate,10:P4}");
        });
        PrintSeparator();
        Colored(ConsoleColor.Cyan, () =>
            Console.WriteLine($"  AMAT                 : {amat,10:F4} ns"));
        PrintSeparator();
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Imprime uma tabela com bordas Unicode.
    /// Largura das colunas é calculada automaticamente pelo conteúdo.
    /// </summary>
    public static void PrintTable(string[] headers, string[][] rows)
    {
        int cols = headers.Length;
        int[] w  = new int[cols];

        // Calcula largura de cada coluna (conteúdo + 2 de padding)
        for (int c = 0; c < cols; c++)
        {
            w[c] = headers[c].Length;
            foreach (var row in rows)
                if (c < row.Length) w[c] = Math.Max(w[c], row[c].Length);
            w[c] += 2;
        }

        string top = "  ┌" + string.Join("┬", w.Select(x => new string('─', x))) + "┐";
        string mid = "  ├" + string.Join("┼", w.Select(x => new string('─', x))) + "┤";
        string bot = "  └" + string.Join("┴", w.Select(x => new string('─', x))) + "┘";

        Console.WriteLine(top);

        // Header
        Colored(ConsoleColor.Yellow, () =>
        {
            Console.Write("  │");
            for (int c = 0; c < cols; c++)
                Console.Write($" {headers[c].PadRight(w[c] - 1)}│");
            Console.WriteLine();
        });

        Console.WriteLine(mid);

        // Linhas de dados
        foreach (var row in rows)
        {
            Console.Write("  │");
            for (int c = 0; c < cols; c++)
            {
                string cell = c < row.Length ? row[c] : "";
                Console.Write($" {cell.PadRight(w[c] - 1)}│");
            }
            Console.WriteLine();
        }

        Console.WriteLine(bot);
    }

    // ── Progress bar ──────────────────────────────────────────────────────────

    public static void UpdateProgress(long current, long total)
    {
        int pct    = total == 0 ? 100 : (int)(current * 100 / total);
        int filled = pct / 5;
        string bar = $"[{new string('█', filled)}{new string('░', 20 - filled)}]";
        Console.Write($"\r  {bar} {pct,3}%  ({current:N0} / {total:N0})   ");
    }

    public static void ClearProgress()
    {
        Console.Write($"\r{new string(' ', Width + 4)}\r");
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    public static void PrintMenu(string title, (string key, string label)[] options)
    {
        PrintSectionHeader(title);
        foreach (var (key, label) in options)
        {
            Console.Write("    ");
            Colored(ConsoleColor.Yellow, () => Console.Write($"[{key}]"));
            Console.WriteLine($"  {label}");
        }
        Console.WriteLine();
        Console.Write("  Escolha: ");
    }

    // ── Input helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Remove aspas duplas envolventes de uma string de entrada.
    /// Regra: se começa E termina com ", remove ambas e faz Trim() do restante.
    /// Útil para caminhos colados do Windows Explorer (ex: "C:\Pasta\Arquivo").
    /// </summary>
    public static string Normalize(string? input)
    {
        var s = input?.Trim() ?? string.Empty;
        if (s.Length >= 2 && s.StartsWith('"') && s.EndsWith('"'))
            s = s[1..^1].Trim();
        return s;
    }

    /// <summary>Lê uma string. Enter sem digitar retorna o valor padrão.</summary>
    public static string ReadInput(string prompt, string defaultValue = "")
    {
        string hint = string.IsNullOrEmpty(defaultValue) ? "" : $" [{defaultValue}]";
        Console.Write($"  {prompt}{hint}: ");
        string raw = Normalize(Console.ReadLine());
        return string.IsNullOrEmpty(raw) ? defaultValue : raw;
    }

    /// <summary>Lê um inteiro no intervalo [min, max]. Repete até ser válido.</summary>
    public static int ReadInt(string prompt, int defaultValue, int min, int max)
    {
        while (true)
        {
            string raw = ReadInput(prompt, defaultValue.ToString());
            if (int.TryParse(raw, out int v) && v >= min && v <= max) return v;
            Colored(ConsoleColor.Red, () =>
                Console.WriteLine($"  [!] Informe um número entre {min} e {max}."));
        }
    }

    /// <summary>Lê uma potência de 2 no intervalo [min, max]. Repete até ser válido.</summary>
    public static int ReadPowerOfTwo(string prompt, int defaultValue, int min, int max)
    {
        int safeDefault = Math.Min(defaultValue, max);
        while (true)
        {
            string raw = ReadInput(prompt, safeDefault.ToString());
            if (int.TryParse(raw, out int v) && v >= min && v <= max && IsPow2(v)) return v;
            Colored(ConsoleColor.Red, () =>
                Console.WriteLine($"  [!] Deve ser potência de 2 entre {min} e {max}."));
        }
    }

    // ── Mensagens coloridas ───────────────────────────────────────────────────

    public static void Success(string msg) =>
        Colored(ConsoleColor.Green, () => Console.WriteLine($"  ✓ {msg}"));

    public static void Error(string msg) =>
        Colored(ConsoleColor.Red,   () => Console.WriteLine($"  X {msg}"));

    public static void Info(string msg) =>
        Colored(ConsoleColor.Cyan,  () => Console.WriteLine($"  -> {msg}"));

    public static void Colored(ConsoleColor color, Action action)
    {
        ConsoleColor prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        action();
        Console.ForegroundColor = prev;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsPow2(int n) => n > 0 && (n & (n - 1)) == 0;

    public static void PressAnyKey()
    {
        Console.WriteLine();
        Info("Pressione qualquer tecla para continuar...");
        Console.ReadKey(true);
    }
}
