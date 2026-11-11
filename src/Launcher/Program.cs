using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace Switch2PcLauncher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class AppSettings
{
    public string? EnginePath { get; set; }
    public List<string> GameFolders { get; set; } = new();
    public List<string> GameFiles { get; set; } = new();
}

internal sealed class MainForm : Form
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".xci", ".nsp", ".nro" };

    private static readonly Color BackgroundColor = Color.FromArgb(17, 21, 32);
    private static readonly Color PanelColor = Color.FromArgb(27, 33, 48);
    private static readonly Color AccentColor = Color.FromArgb(83, 142, 255);
    private readonly string settingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Switch2PC");
    private readonly string settingsFile;
    private readonly DataGridView gamesGrid = new();
    private readonly Label statusLabel = new();
    private AppSettings settings;

    public MainForm()
    {
        settingsFile = Path.Combine(settingsDirectory, "settings.json");
        settings = LoadSettings();

        Text = "Switch 2 PC Emulator";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 560);
        Size = new Size(1120, 720);
        BackColor = BackgroundColor;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;

        BuildInterface();
        RefreshLibrary();
    }

    private void BuildInterface()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 104,
            BackColor = PanelColor,
            Padding = new Padding(26, 17, 20, 12)
        };

        var title = new Label
        {
            Text = "SWITCH 2 PC",
            AutoSize = true,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(24, 12)
        };
        var subtitle = new Label
        {
            Text = "EMULADOR COMUNITARIO  •  BIBLIOTECA LOCAL  •  MODO SIN CONEXIÓN",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(158, 174, 201),
            Location = new Point(27, 61)
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 94,
            BackColor = BackgroundColor,
            Padding = new Padding(18, 17, 12, 10),
            WrapContents = true
        };
        toolbar.Controls.Add(CreateButton("Añadir archivos", AddGameFiles));
        toolbar.Controls.Add(CreateButton("Añadir carpeta", AddGameFolder));
        toolbar.Controls.Add(CreateButton("Localizar motor", LocateEngine));
        toolbar.Controls.Add(CreateButton("Ajustes del motor", OpenEngineSettings));
        toolbar.Controls.Add(CreateButton("Configurar teclado", ShowKeyboardHelp));

        gamesGrid.Dock = DockStyle.Fill;
        gamesGrid.BackgroundColor = BackgroundColor;
        gamesGrid.BorderStyle = BorderStyle.None;
        gamesGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        gamesGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        gamesGrid.EnableHeadersVisualStyles = false;
        gamesGrid.ColumnHeadersDefaultCellStyle.BackColor = PanelColor;
        gamesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(190, 205, 228);
        gamesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        gamesGrid.DefaultCellStyle.BackColor = BackgroundColor;
        gamesGrid.DefaultCellStyle.ForeColor = Color.White;
        gamesGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(43, 68, 111);
        gamesGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        gamesGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(21, 27, 40);
        gamesGrid.RowHeadersVisible = false;
        gamesGrid.ReadOnly = true;
        gamesGrid.AllowUserToAddRows = false;
        gamesGrid.AllowUserToDeleteRows = false;
        gamesGrid.AllowUserToResizeRows = false;
        gamesGrid.MultiSelect = false;
        gamesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gamesGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        gamesGrid.RowTemplate.Height = 38;
        gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "GameTitle",
            HeaderText = "JUEGO",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 28
        });
        gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Format",
            HeaderText = "FORMATO",
            Width = 105
        });
        gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FilePath",
            HeaderText = "UBICACIÓN LOCAL",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 55
        });
        gamesGrid.CellDoubleClick += (_, _) => LaunchSelectedGame();

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = PanelColor,
            Padding = new Padding(18, 12, 18, 10)
        };

        statusLabel.AutoSize = false;
        statusLabel.Dock = DockStyle.Left;
        statusLabel.Width = 620;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = Color.FromArgb(174, 190, 216);

        var playButton = CreateButton("▶  JUGAR", LaunchSelectedGame, true);
        playButton.Dock = DockStyle.Right;
        playButton.Width = 160;
        playButton.Height = 44;

        var refreshButton = CreateButton("Actualizar", RefreshLibrary);
        refreshButton.Dock = DockStyle.Right;
        refreshButton.Width = 112;
        refreshButton.Height = 44;
        refreshButton.Margin = new Padding(0, 0, 10, 0);

        bottom.Controls.Add(statusLabel);
        bottom.Controls.Add(playButton);
        bottom.Controls.Add(refreshButton);

        Controls.Add(gamesGrid);
        Controls.Add(bottom);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private Button CreateButton(string text, Action action, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Width = primary ? 160 : 150,
            Height = 42,
            Margin = new Padding(0, 0, 10, 9),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? AccentColor : Color.FromArgb(42, 53, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFile))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsFile));
                if (loaded is not null)
                {
                    loaded.GameFolders ??= new List<string>();
                    loaded.GameFiles ??= new List<string>();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron leer los ajustes locales. Se usarán valores nuevos.\n\n{ex.Message}",
                "Switch 2 PC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        return new AppSettings();
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(settingsDirectory);
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudieron guardar los ajustes.\n\n{ex.Message}",
                "Switch 2 PC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddGameFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Selecciona copias de juegos que tengas derecho a utilizar",
            Filter = "Archivos compatibles (*.xci;*.nsp;*.nro)|*.xci;*.nsp;*.nro|Todos los archivos (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        foreach (var file in dialog.FileNames)
            AddGamePath(file);

        SaveSettings();
        RefreshLibrary();
    }

    private void AddGameFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecciona una carpeta local con tus juegos",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var folder = Path.GetFullPath(dialog.SelectedPath);
        if (!settings.GameFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            settings.GameFolders.Add(folder);

        SaveSettings();
        RefreshLibrary();
    }

    private void AddGamePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath) || !SupportedExtensions.Contains(Path.GetExtension(fullPath)))
                return;

            if (!settings.GameFiles.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
                settings.GameFiles.Add(fullPath);
        }
        catch
        {
            // Ignore inaccessible or malformed paths; the rest of the library can still load.
        }
    }

    private void RefreshLibrary()
    {
        var games = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in settings.GameFiles.ToArray())
        {
            if (File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path)))
                games.Add(path);
        }

        foreach (var folder in settings.GameFolders.ToArray())
        {
            if (!Directory.Exists(folder))
                continue;

            try
            {
                foreach (var path in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                {
                    if (SupportedExtensions.Contains(Path.GetExtension(path)))
                        games.Add(path);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Some subfolders may be protected; skip them without interrupting the launcher.
            }
            catch (IOException)
            {
                // Skip a folder that becomes unavailable while scanning.
            }
        }

        gamesGrid.Rows.Clear();
        foreach (var path in games.OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
        {
            var index = gamesGrid.Rows.Add(
                Path.GetFileNameWithoutExtension(path),
                Path.GetExtension(path).TrimStart('.').ToUpperInvariant(),
                path);
            gamesGrid.Rows[index].Tag = path;
        }

        statusLabel.Text = $"{games.Count} juego(s) en la biblioteca  •  Los archivos permanecen en sus carpetas originales.";
    }

    private string? FindEngine()
    {
        if (!string.IsNullOrWhiteSpace(settings.EnginePath) && File.Exists(settings.EnginePath))
            return settings.EnginePath;

        var engineDirectory = Path.Combine(AppContext.BaseDirectory, "engine");
        if (Directory.Exists(engineDirectory))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "eglantine.exe", "eden.exe" };

            try
            {
                var candidate = Directory.EnumerateFiles(engineDirectory, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault(path => names.Contains(Path.GetFileName(path)));
                if (candidate is not null)
                {
                    settings.EnginePath = candidate;
                    SaveSettings();
                    return candidate;
                }
            }
            catch (IOException)
            {
                // Fall through to the manual picker.
            }
            catch (UnauthorizedAccessException)
            {
                // Fall through to the manual picker.
            }
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Selecciona el ejecutable del motor Eglantine",
            Filter = "Ejecutable de Windows (*.exe)|*.exe",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return null;

        settings.EnginePath = Path.GetFullPath(dialog.FileName);
        SaveSettings();
        return settings.EnginePath;
    }

    private void LocateEngine()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Localiza Eglantine.exe",
            Filter = "Ejecutable de Windows (*.exe)|*.exe",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        settings.EnginePath = Path.GetFullPath(dialog.FileName);
        SaveSettings();
        statusLabel.Text = "Motor configurado: " + settings.EnginePath;
    }

    private void LaunchSelectedGame()
    {
        if (gamesGrid.CurrentRow?.Tag is not string gamePath || !File.Exists(gamePath))
        {
            MessageBox.Show("Primero selecciona un juego de la biblioteca.", "Switch 2 PC",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        StartEngine(gamePath);
    }

    private void OpenEngineSettings()
    {
        StartEngine(null);
    }

    private void StartEngine(string? gamePath)
    {
        var enginePath = FindEngine();
        if (string.IsNullOrWhiteSpace(enginePath))
            return;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = enginePath,
                WorkingDirectory = Path.GetDirectoryName(enginePath) ?? AppContext.BaseDirectory,
                UseShellExecute = false
            };

            // Eglantine documents loading a game by passing its file path as the first argument.
            if (!string.IsNullOrWhiteSpace(gamePath))
                startInfo.ArgumentList.Add(gamePath);

            Process.Start(startInfo);
            statusLabel.Text = gamePath is null
                ? "Se ha abierto el motor para configurar sus ajustes."
                : "Juego enviado al motor: " + Path.GetFileName(gamePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo iniciar el motor. Comprueba que el paquete esté completo.\n\n{ex.Message}",
                "Error al iniciar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowKeyboardHelp()
    {
        MessageBox.Show(
            "Para jugar con teclado:\n\n" +
            "1. Pulsa «Ajustes del motor» para abrir Eglantine.\n" +
            "2. En el emulador, entra en Emulation → Configure → Controls.\n" +
            "3. Selecciona el teclado como dispositivo de entrada y asigna las teclas a los botones.\n" +
            "4. Guarda el perfil de entrada y vuelve a esta biblioteca para iniciar el juego.\n\n" +
            "El lanzador no impone un mapa de teclas: se utiliza la configuración de entrada del motor.",
            "Configuración del teclado", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
            return;

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                if (!settings.GameFolders.Contains(path, StringComparer.OrdinalIgnoreCase))
                    settings.GameFolders.Add(path);
            }
            else
            {
                AddGamePath(path);
            }
        }

        SaveSettings();
        RefreshLibrary();
    }
}
