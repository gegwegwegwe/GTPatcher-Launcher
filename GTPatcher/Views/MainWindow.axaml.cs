using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using GTPatcher.Utilities;
using GTPatcher.Types;
using static Constants;

namespace GTPatcher.Views
{
    public partial class MainWindow : Window
    {
        private const string RegistryKeyPath = @"HKEY_CURRENT_USER\SOFTWARE\GTPatcher";
        private string _settingsPath = string.Empty;
        private Settings _settings = new();
        private List<Patch>? _buildsList;
        private readonly HttpClient _httpClient = new();
        private CancellationTokenSource? _downloadCts;

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
            
            // Hide progress controls initially
            if (DownloadProgress != null) DownloadProgress.IsVisible = false;
            if (ProgressText != null) ProgressText.IsVisible = false;
        }

        private void InitializeApplication()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GTPatcher"
            );

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _settingsPath = Path.Combine(appDataPath, "settings.json");
            
            if (File.Exists(_settingsPath))
            {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_settingsPath)) ?? new Settings();
            }

            PathTextBox.Text = _settings.Path;
            UserTextBox.Text = _settings.Username;
            
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue 
            { 
                NoCache = true,
                NoStore = true 
            };
            
            LoadBuildsAsync();
        }

        private async void LoadBuildsAsync()
        {
            try
            {
                UpdateStatusBar("Loading builds...");
                
                var json = await _httpClient.GetStringAsync(INDEX_JSON);
                _buildsList = JsonConvert.DeserializeObject<List<Patch>>(json, new JsonSerializerSettings 
                { 
                    NullValueHandling = NullValueHandling.Ignore 
                });

                if (_buildsList == null || _buildsList.Count == 0)
                {
                    await ShowMessageBoxAsync("Error", "Failed to retrieve builds list. Check your internet connection!");
                    UpdateStatusBar("Failed to load builds");
                    return;
                }

                Builds.ItemsSource = _buildsList.Select(p => p.PatchName).ToList();
                Builds.SelectedItem = _buildsList.First().PatchName;
                
                UpdateBuildInfo();
                UpdateStatusBar($"Loaded {_buildsList.Count} builds - Ready");
            }
            catch (Exception ex)
            {
                await ShowMessageBoxAsync("Error", $"Failed to load builds: {ex.Message}\nCheck your internet connection!");
                UpdateStatusBar("Failed to load builds");
            }
        }

        private void UpdateBuildInfo()
        {
            if (_buildsList == null) return;
            
            var selectedBuild = _buildsList.FirstOrDefault(x => x.PatchName == Builds.SelectedItem as string);
            if (selectedBuild == null) return;
            
            manifestIdLabel.Text = selectedBuild.ManifestId > 0 
                ? selectedBuild.ManifestId.ToString() 
                : "No Steam manifest for this build";
                
            descriptionLabel.Text = !string.IsNullOrEmpty(selectedBuild.PatchDescription) 
                ? selectedBuild.PatchDescription 
                : "No description for this patch";
        }

        private void SaveSettings()
        {
            _settings.Path = PathTextBox.Text;
            _settings.Username = UserTextBox.Text;
            
            try
            {
                File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(_settings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private void PathTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            SaveSettings();
        }

        private void UserTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            SaveSettings();
        }

        private void Builds_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateBuildInfo();
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserTextBox.Text))
            {
                await ShowMessageBoxAsync("Cannot download game", "Please enter your Steam account username in the Settings tab.");
                return;
            }

            if (string.IsNullOrWhiteSpace(PathTextBox.Text))
            {
                await ShowMessageBoxAsync("Cannot download game", "Please set an installation path in the Settings tab.");
                return;
            }

            if (_buildsList == null)
            {
                await ShowMessageBoxAsync("Error", "Builds list not loaded. Please try again.");
                return;
            }

            var selectedBuild = _buildsList.FirstOrDefault(x => x.PatchName == Builds.SelectedItem as string);
            if (selectedBuild == null)
            {
                await ShowMessageBoxAsync("Cannot download game", "Please select a game version first.");
                return;
            }

            var specificBuildPath = Path.Combine(PathTextBox.Text, selectedBuild.PatchShorthand);
            
            try
            {
                if (!Directory.Exists(specificBuildPath))
                {
                    Directory.CreateDirectory(specificBuildPath);
                    
                    UpdateStatusBar("Downloading game files...");
                    SetProgressVisible(true);
                    PlayButton.IsEnabled = false;
                    
                    _downloadCts = new CancellationTokenSource();
                    
                    var installResult = await InstallGameAsync(selectedBuild, specificBuildPath, _downloadCts.Token);
                    
                    if (installResult != 0)
                    {
                        PlayButton.IsEnabled = true;
                        SetProgressVisible(false);
                        await ShowMessageBoxAsync("Download Failed", 
                            "Something went wrong during download!\n" +
                            "Most likely your username or password is incorrect.\n" +
                            "The incomplete installation has been deleted.");
                        
                        if (Directory.Exists(specificBuildPath))
                        {
                            Directory.Delete(specificBuildPath, true);
                        }
                        UpdateStatusBar("Download failed");
                        return;
                    }
                    
                    UpdateStatusBar("Patching game files...");
                    await PatchAssemblyAsync(selectedBuild, Path.Combine(specificBuildPath, $"{selectedBuild.GameName}_Data", "Managed"));
                    
                    UpdateStatusBar("Ready to play!");
                    SetProgressVisible(false);
                }

                var exePath = Path.Combine(specificBuildPath, $"{selectedBuild.GameName}.exe");
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (File.Exists(exePath))
                    {
                        UpdateStatusBar("Launching game...");
                        Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    await ShowMessageBoxAsync("Manual Action Required", 
                        "Linux builds require manual setup:\n" +
                        "1. Add the build as a non-Steam game in Steam\n" +
                        "2. Configure it to run under Proton\n" +
                        "3. Launch from your Steam library");
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatusBar("Download cancelled");
                SetProgressVisible(false);
                await ShowMessageBoxAsync("Cancelled", "The download was cancelled.");
            }
            catch (Exception ex)
            {
                UpdateStatusBar("Error occurred");
                SetProgressVisible(false);
                await ShowMessageBoxAsync("Error", $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                PlayButton.IsEnabled = true;
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }

        private async Task<int> InstallGameAsync(Patch selectedBuild, string installPath, CancellationToken cancellationToken)
        {
            if (selectedBuild.IsSteam)
            {
                return await DownloadHelper.DownloadManifestAsync(
                    (ulong)selectedBuild.ManifestId, 
                    installPath, 
                    UserTextBox.Text, 
                    selectedBuild.Branch,
                    cancellationToken
                );
            }
            else
            {
                return await DownloadHelper.DownloadUrlAsync(
                    installPath, 
                    selectedBuild.GameLink,
                    _httpClient,
                    cancellationToken
                );
            }
        }

        private async Task PatchAssemblyAsync(Patch selectedBuild, string managedPath)
        {
            var originalDllPath = Path.Combine(managedPath, "Assembly-CSharp.dll");
            var backupPath = Path.Combine(managedPath, "Assembly-CSharp.bak");
            var patchPath = Path.Combine(managedPath, "patch.xdelta");

            if (!File.Exists(originalDllPath))
            {
                throw new FileNotFoundException("Assembly-CSharp.dll not found", originalDllPath);
            }

            // Create backup
            File.Copy(originalDllPath, backupPath, overwrite: true);

            try
            {
                // Download patch file
                using var patchStream = await _httpClient.GetStreamAsync(selectedBuild.PatchLink);
                await using var patchFile = new FileStream(patchPath, FileMode.Create, FileAccess.Write);
                await patchStream.CopyToAsync(patchFile);

                // Apply patch
                await using var inputStream = new FileStream(backupPath, FileMode.Open);
                await using var patchFileStream = new FileStream(patchPath, FileMode.Open);
                await using var outputStream = new FileStream(originalDllPath, FileMode.Create);

                using var decoder = new PleOps.XdeltaSharp.Decoder.Decoder(inputStream, patchFileStream, outputStream);
                decoder.Run();
            }
            finally
            {
                // Cleanup patch file
                if (File.Exists(patchPath))
                {
                    File.Delete(patchPath);
                }
            }
        }

        private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOptions
            {
                Title = "Select Installation Path"
            });

            if (folders.Count > 0)
            {
                var path = folders[0].Path.LocalPath;
                PathTextBox.Text = path;
                SaveSettings();
            }
        }

        private async Task ShowMessageBoxAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowAsync();
        }

        private void UpdateStatusBar(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
                
                // Update color based on status
                StatusText.Foreground = message.Contains("failed") || message.Contains("error") || message.Contains("Failed")
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFD32F2F"))
                    : message.Contains("Ready") || message.Contains("Loaded")
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF4CAF50"))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF2196F3"));
            }
            
            if (StatusBarText != null)
            {
                StatusBarText.Text = message;
            }
        }

        private void SetProgressVisible(bool visible)
        {
            if (DownloadProgress != null)
            {
                DownloadProgress.IsVisible = visible;
                DownloadProgress.IsIndeterminate = visible;
            }
            
            if (ProgressText != null)
            {
                ProgressText.IsVisible = visible;
                ProgressText.Text = visible ? "Downloading..." : "";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _httpClient.Dispose();
            _downloadCts?.Dispose();
        }
    }
}
