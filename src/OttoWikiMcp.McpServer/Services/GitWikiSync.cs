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

    /// <summary>
    /// Faz <c>git add -A</c> + <c>git commit</c> no clone local. Contra uma wiki real do Azure
    /// DevOps, isso só publica a mudança de verdade depois de um <c>git push</c> adicional (fora
    /// de escopo desta POC local, que roda contra um "remoto" em disco) — ver o guia de
    /// implementação para o passo de push num ambiente real.
    /// </summary>
    public async Task CommitAllAsync(string message, CancellationToken ct = default)
    {
        await RunGitAsync(_localPath, ct, "add", "-A");
        var status = await RunGitAsync(_localPath, ct, "status", "--porcelain");
        if (string.IsNullOrWhiteSpace(status))
        {
            logger.LogInformation("Nada para commitar (conteúdo salvo é igual ao já existente).");
            return;
        }
        // -c user.name/user.email (em vez de depender de `git config --global` já estar setado no
        // ambiente) — o container de produção nunca teve identidade git configurada, e sem isso
        // TODO commit falha com "Author identity unknown". Passar via -c cobre qualquer ambiente
        // (container novo, máquina de dev sem git configurado) sem exigir setup prévio.
        await RunGitAsync(_localPath, ct,
            "-c", "user.name=OttoWikiMcp", "-c", "user.email=ottowikimcp@localhost",
            "commit", "-m", message);
    }

    private async Task<string> RunGitAsync(string workingDir, CancellationToken ct, params string[] args)
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
        return stdout;
    }
}
