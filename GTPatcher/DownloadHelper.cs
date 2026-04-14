using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GTPatcher.Utilities;

/// <summary>
/// Provides helper methods for downloading game files from Steam Depot or direct URLs.
/// </summary>
public static class DownloadHelper
{
    /// <summary>
    /// Downloads game files from Steam Depot using DepotDownloader.
    /// </summary>
    /// <param name="manifestId">The Steam manifest ID to download</param>
    /// <param name="directory">Target directory for installation</param>
    /// <param name="steamUsername">Steam account username</param>
    /// <param name="branch">Steam branch name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exit code from DepotDownloader (0 = success)</returns>
    public static async Task<int> DownloadManifestAsync(
        ulong manifestId, 
        string directory, 
        string steamUsername, 
        string branch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentException.ThrowIfNullOrEmpty(steamUsername);
        ArgumentException.ThrowIfNullOrEmpty(branch);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Console.WriteLine($"Created installation directory: {directory}");
        }

        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "DepotDownloader.exe" 
            : "DepotDownloader";

        var arguments = $"-app {Constants.APP_ID} " +
                       $"-depot {Constants.DEPOT_ID} " +
                       $"-manifest {manifestId} " +
                       $"-branch {branch} " +
                       $"-username \"{steamUsername}\" " +
                       $"-remember-password " +
                       $"-dir \"{directory}\"";

        Console.WriteLine($"Starting DepotDownloader with manifest {manifestId}");
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executableName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        process.OutputDataReceived += (sender, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"DepotDownloader: {e.Data}");
        };
        
        process.ErrorDataReceived += (sender, e) => 
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"DepotDownloader Error: {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        
        Console.WriteLine($"DepotDownloader completed with exit code: {process.ExitCode}");
        return process.ExitCode;
    }

    /// <summary>
    /// Downloads and extracts game files from a direct URL.
    /// </summary>
    /// <param name="directory">Target directory for installation</param>
    /// <param name="url">Direct download URL</param>
    /// <param name="httpClient">HTTP client for downloads</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>0 on success, 1 on failure</returns>
    public static async Task<int> DownloadUrlAsync(
        string directory, 
        string url, 
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentNullException.ThrowIfNull(httpClient);

        try
        {
            var zipPath = Path.Combine(directory, "game.zip");
            
            Console.WriteLine($"Downloading game archive from {url}");
            
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            await stream.CopyToAsync(fileStream, cancellationToken);
            
            Console.WriteLine($"Download complete, extracting to {directory}");
            
            ZipFile.ExtractToDirectory(zipPath, directory, overwriteFiles: true);
            File.Delete(zipPath);
            
            // Flatten directory structure if needed
            await FlattenDirectoryStructureAsync(directory, cancellationToken);
            
            Console.WriteLine("Download and extraction completed successfully");
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Download cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download from URL: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Flattens directory structure by moving contents from subdirectories to root.
    /// </summary>
    private static async Task FlattenDirectoryStructureAsync(string directory, CancellationToken cancellationToken)
    {
        var subdirectories = Directory.GetDirectories(directory);
        
        foreach (var subDir in subdirectories)
        {
            if (Directory.GetFiles(subDir, "*.exe").Length > 0)
            {
                Console.WriteLine($"Flattening directory structure from {subDir}");
                await CopyDirectoryAsync(subDir, directory, cancellationToken);
                Directory.Delete(subDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Asynchronously copies all contents from source to destination directory.
    /// </summary>
    private static async Task CopyDirectoryAsync(string sourceFolder, string destFolder, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(destFolder))
            Directory.CreateDirectory(destFolder);

        var files = Directory.GetFiles(sourceFolder);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dest = Path.Combine(destFolder, Path.GetFileName(file));
            await Task.Run(() => File.Copy(file, dest, overwrite: true), cancellationToken);
        }

        var folders = Directory.GetDirectories(sourceFolder);
        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dest = Path.Combine(destFolder, Path.GetFileName(folder));
            await CopyDirectoryAsync(folder, dest, cancellationToken);
        }
    }
}
