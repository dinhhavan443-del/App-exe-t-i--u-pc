using System.Diagnostics;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualBasic.Devices;

namespace PEDINHIOS.FPSBoost;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly ComputerInfo computer = new();
    private PerformanceCounter? cpuCounter;

    public MainWindow()
    {
        InitializeComponent();
        try { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); cpuCounter.NextValue(); } catch { }
        LoadSystemInfo();
        RefreshProcesses();
        timer.Tick += (_, _) => UpdateStats();
        timer.Start();
    }

    private void LoadSystemInfo()
    {
        try
        {
            using var s = new ManagementObjectSearcher("select Name from Win32_Processor");
            CpuName.Text = "CPU: " + (s.Get().Cast<ManagementObject>().FirstOrDefault()?["Name"]?.ToString() ?? "Không xác định");
        }
        catch { CpuName.Text = "CPU: Không đọc được"; }
        double total = computer.TotalPhysicalMemory / 1024d / 1024 / 1024;
        RamTotal.Text = $"RAM vật lý: {total:0.0} GB";
        OsInfo.Text = "Windows: " + Environment.OSVersion.VersionString;
    }

    private void UpdateStats()
    {
        try
        {
            CpuValue.Text = $"{Math.Clamp(cpuCounter?.NextValue() ?? 0, 0, 100):0}%";
            double total = computer.TotalPhysicalMemory / 1024d / 1024 / 1024;
            double free = computer.AvailablePhysicalMemory / 1024d / 1024 / 1024;
            RamValue.Text = $"{total - free:0.0} / {total:0.0} GB";
        }
        catch { }
        RefreshProcesses();
    }

    private static Process[] FindPlayers()
    {
        try { return Process.GetProcessesByName("HD-Player"); }
        catch { return Array.Empty<Process>(); }
    }

    private void RefreshProcesses()
    {
        var ps = FindPlayers();
        if (ps.Length == 0)
        {
            BlueStacksStatus.Text = "● BlueStacks chưa chạy";
            BlueStacksStatus.Foreground = Brushes.Orange;
            ProcessInfo.Text = "Không tìm thấy HD-Player.exe. Hãy mở BlueStacks trước.";
            return;
        }
        BlueStacksStatus.Text = $"● Đã phát hiện {ps.Length} tiến trình BlueStacks";
        BlueStacksStatus.Foreground = Brushes.LightGreen;
        ProcessInfo.Text = string.Join(Environment.NewLine, ps.Select(p =>
        {
            try { return $"PID {p.Id} • Priority: {p.PriorityClass} • RAM: {p.WorkingSet64 / 1048576d:0} MB"; }
            catch { return $"PID {p.Id} • Không đọc được trạng thái"; }
        }));
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshProcesses();

    private void Boost_Click(object sender, RoutedEventArgs e)
    {
        var ps = FindPlayers();
        if (ps.Length == 0) { ActionStatus.Text = "Không tìm thấy BlueStacks. Hãy mở giả lập trước."; return; }
        int ok = 0;
        foreach (var p in ps) try { p.PriorityClass = ProcessPriorityClass.AboveNormal; ok++; } catch (Exception ex) { ActionStatus.Text = "Không thể đổi priority: " + ex.Message; }
        if (ok > 0) ActionStatus.Text = $"Đã đặt AboveNormal cho {ok}/{ps.Length} tiến trình. Thay đổi mất khi HD-Player.exe đóng.";
        RefreshProcesses();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var ps = FindPlayers(); int ok = 0;
        foreach (var p in ps) try { p.PriorityClass = ProcessPriorityClass.Normal; ok++; } catch { }
        ActionStatus.Text = ok == 0 ? "Không có tiến trình để khôi phục." : $"Đã khôi phục Normal cho {ok} tiến trình.";
        RefreshProcesses();
    }

    private void Guide_Click(object sender, RoutedEventArgs e)
    {
        string fps = (FpsCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "60 FPS";
        MessageBox.Show($"Mục tiêu: {fps}\n\nMở BlueStacks → Settings → Performance.\nBật High frame rate nếu có và chọn FPS game hỗ trợ.\nPhân bổ CPU/RAM vừa đủ, chừa tài nguyên cho Windows.\nLưu thay đổi và khởi động lại nếu được yêu cầu.\n\nỨng dụng không tự ghi cấu hình riêng của từng phiên bản BlueStacks.",
            "PEDINHIOS • BlueStacks", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnClosed(EventArgs e)
    {
        timer.Stop(); cpuCounter?.Dispose(); base.OnClosed(e);
    }
}