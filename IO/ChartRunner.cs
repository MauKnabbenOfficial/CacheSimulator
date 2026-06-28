using System.Diagnostics;

namespace CacheSimulator.IO;

/// <summary>
/// Localiza o interpretador Python e executa charts/graficos.py,
/// passando a pasta de resultados como argumento.
/// </summary>
public static class ChartRunner
{
    private const string ScriptRelativePath = "charts/graficos.py";

    // ── Ponto de entrada principal ────────────────────────────────────────────

    /// <summary>
    /// Executa o script Python de geração de gráficos.
    /// </summary>
    /// <param name="resultsDir">Caminho da pasta onde estão os CSVs e onde os gráficos serão salvos.</param>
    /// <returns>true se o script concluiu com código 0; false caso contrário.</returns>
    public static bool Run(string resultsDir)
    {
        string? scriptPath = FindScript();
        if (scriptPath is null)
        {
            Console.Error.WriteLine(
                "  [erro] graficos.py não encontrado. " +
                "Verifique se a pasta 'charts/' existe na raiz do projeto.");
            return false;
        }

        string? python = FindPython();
        if (python is null)
        {
            Console.Error.WriteLine(
                "  [erro] Python não encontrado. Instale Python 3 e adicione ao PATH.");
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName               = python,
            Arguments              = $"\"{scriptPath}\" \"{resultsDir}\"",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };

        using var proc = Process.Start(psi);
        if (proc is null) return false;

        // Exibe output em tempo real
        proc.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.WriteLine("  " + e.Data); };
        proc.ErrorDataReceived  += (_, e) => { if (e.Data is not null) Console.Error.WriteLine("  " + e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        return proc.ExitCode == 0;
    }

    // ── Localização do script ─────────────────────────────────────────────────

    /// <summary>
    /// Sobe na árvore de diretórios a partir do executável procurando charts/graficos.py.
    /// Funciona tanto em debug (bin/Debug/net10.0/) quanto em publish.
    /// </summary>
    private static string? FindScript()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int depth = 0; depth < 6 && dir is not null; depth++, dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, ScriptRelativePath);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    // ── Localização do Python ─────────────────────────────────────────────────

    /// <summary>
    /// Tenta 'py' (Windows Launcher), depois 'python3', depois 'python'.
    /// Retorna o primeiro que responder com exit code 0 a --version.
    /// </summary>
    private static string? FindPython()
    {
        foreach (string candidate in new[] { "py", "python3", "python" })
        {
            if (PythonResponds(candidate))
                return candidate;
        }
        return null;
    }

    private static bool PythonResponds(string executable)
    {
        try
        {
            var psi = new ProcessStartInfo(executable, "--version")
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(3000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
