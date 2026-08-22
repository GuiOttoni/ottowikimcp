using System.Diagnostics;

namespace OttoWikiMcp.McpServer.Services;

/// <summary>
/// Clona/atualiza um repositório git local (o "clone da wiki") chamando o `git` do sistema
/// diretamente, em vez de qualquer biblioteca .NET de git. Isso é proposital: significa que a
/// autenticação inteira fica por conta do Git Credential Manager já instalado na máquina —
/// contra uma wiki real do Azure DevOps, isso dispara um login interativo via navegador (OAuth
/// contra o Entra ID da organização) na primeira vez, sem precisar de PAT nenhum. Times que
/// bloqueiam a criação de PAT por política normalmente ainda permitem login normal via AAD, e é
/// exatamente esse caminho que o GCM usa.
///
/// Nesta POC, `WikiRepoUrl` aponta para uma pasta local (`file://...`) simulando a wiki — trocar
/// para a URL real do Azure DevOps (`https://dev.azure.com/{org}/{project}/_git/{project}.wiki`)
/// não exige nenhuma mudança de código, só de configuração.
/// </summary>
public sealed class GitWikiSync(IConfiguration config, ILogger<GitWikiSync> logger)
{
    private readonly string _repoUrl = config["Wiki:RepoUrl"]
        ?? throw new InvalidOperationException("Config 'Wiki:RepoUrl' não definida.");

    private readonly string _localPath = Path.GetFullPath(
        config["Wiki:LocalClonePath"] ?? "wiki-clone", AppContext.BaseDirectory);

    public string LocalPath => _localPath;

    public async Task EnsureClonedAndUpToDateAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(Path.Combine(_localPath, ".git")))
        {
            await RunGitAsync(_localPath, ct, "pull", "--ff-only");
            logger.LogInformation("Wiki atualizada (git pull) em {Path}", _localPath);
        }
        else
        {
            var parent = Path.GetDirectoryName(_localPath);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            await RunGitAsync(Path.GetTempPath(), ct, "clone", _repoUrl, _localPath);
            logger.LogInformation("Wiki clonada em {Path}", _localPath);
        }
    }

    private async Task RunGitAsync(string workingDir, CancellationToken ct, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Não foi possível iniciar o processo 'git'. Ele está instalado e no PATH?");

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            logger.LogError("git {Args} falhou ({ExitCode}): {Stderr}", string.Join(' ', args), process.ExitCode, stderr);
            throw new InvalidOperationException($"git {string.Join(' ', args)} falhou: {stderr}");
        }

        if (!string.IsNullOrWhiteSpace(stdout)) logger.LogDebug("git stdout: {Stdout}", stdout);
    }
}
