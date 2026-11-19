
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PEDINHIOS.FPSBoost;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer timer =
        new() { Interval = TimeSpan.FromSeconds(2) };

    private ulong lastIdle;
    private ulong lastKernel;
    private ulong lastUser;
    private bool hasCpuSample;

    public MainWindow()
    {
        InitializeComponent();

        LoadSystemInfo();
        RefreshProcesses();
        UpdateStats();

        timer.Tick += (_, _) => UpdateStats();
        timer.Start();
    }

    // Windows API: read CPU usage
    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint Low;
        public uint High;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(
        out FILETIME idle,
        out FILETIME kernel,
        out FILETIME user);

    private static ulong ToUInt64(FILETIME value)
    {
        return ((ulong)value.High << 32) | value.Low;
    }

    // Windows API: read physical RAM
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(
        ref MEMORYSTATUSEX lpBuffer);

    private void LoadSystemInfo()
    {
        try
        {
            string cpuName =
                Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString",
                    null
                )?.ToString() ?? "Unknown CPU";

            CpuName.Text = "CPU: " + cpuName;
        }
        catch
        {
            CpuName.Text = "CPU: Windows Processor";
        }

        var memory = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        if (GlobalMemoryStatusEx(ref memory))
        {
            double total = memory.ullTotalPhys / 1073741824d;
            RamTotal.Text = $"RAM vật lý: {total:0.0} GB";
        }
        else
        {
            RamTotal.Text = "RAM: Không đọc được";
        }

        OsInfo.Text = "Windows: " + Environment.OSVersion.VersionString;
    }

    private void UpdateStats()
    {
        try
        {
            if (GetSystemTimes(
                out FILETIME idle,
                out FILETIME kernel,
                out FILETIME user))
            {
                ulong currentIdle = ToUInt64(idle);
                ulong currentKernel = ToUInt64(kernel);
                ulong currentUser = ToUInt64(user);

                if (hasCpuSample)
                {
                    ulong idleDelta = currentIdle - lastIdle;
                    ulong kernelDelta = currentKernel - lastKernel;
                    ulong userDelta = currentUser - lastUser;

                    ulong total = kernelDelta + userDelta;

                    if (total > 0)
                    {
                        double usage =
                            100.0 * (total - idleDelta) / total;

                        CpuValue.Text =
                            $"{Math.Clamp(usage, 0, 100):0}%";
                    }
                }

                lastIdle = currentIdle;
                lastKernel = currentKernel;
                lastUser = currentUser;
                hasCpuSample = true;
            }

            var memory = new MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            if (GlobalMemoryStatusEx(ref memory))
            {
                double total = memory.ullTotalPhys / 1073741824d;
                double free = memory.ullAvailPhys / 1073741824d;

                RamValue.Text =
                    $"{total - free:0.0} / {total:0.0} GB";
            }
        }
        catch
        {
            CpuValue.Text = "--%";
            RamValue.Text = "-- / -- GB";
        }

        RefreshProcesses();
    }

    private static Process[] FindPlayers()
    {
        try
        {
            return Process.GetProcessesByName("HD-Player");
        }
        catch
        {
            return Array.Empty<Process>();
        }
    }

    private void RefreshProcesses()
    {
        var processes = FindPlayers();

        if (processes.Length == 0)
        {
            BlueStacksStatus.Text = "● BlueStacks chưa chạy";
            BlueStacksStatus.Foreground = Brushes.Orange;
            ProcessInfo.Text =
                "Không tìm thấy HD-Player.exe. Hãy mở BlueStacks trước.";
            return;
        }

        BlueStacksStatus.Text =
            $"● Đã phát hiện {processes.Length} tiến trình BlueStacks";

        BlueStacksStatus.Foreground = Brushes.LightGreen;

        ProcessInfo.Text = string.Join(
            Environment.NewLine,
            Array.ConvertAll(processes, p =>
            {
                try
                {
                    return $"PID {p.Id} • Priority: {p.PriorityClass} • RAM: {p.WorkingSet64 / 1048576d:0} MB";
                }
                catch
                {
                    return $"PID {p.Id} • Không đọc được trạng thái";
                }
            })
        );

        foreach (var p in processes)
            p.Dispose();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshProcesses();
    }

    private void Boost_Click(object sender, RoutedEventArgs e)
    {
        var processes = FindPlayers();

        if (processes.Length == 0)
        {
            ActionStatus.Text =
                "Không tìm thấy BlueStacks. Hãy mở giả lập trước.";
            return;
        }

        int success = 0;

        foreach (var p in processes)
        {
            try
            {
                p.PriorityClass = ProcessPriorityClass.AboveNormal;
                success++;
            }
            catch (Exception ex)
            {
                ActionStatus.Text =
                    "Không thể đổi priority: " + ex.Message;
            }
            finally
            {
                p.Dispose();
            }
        }

        if (success > 0)
        {
            ActionStatus.Text =
                $"Đã BOOST thành công {success} tiến trình BlueStacks.";
        }

        RefreshProcesses();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var processes = FindPlayers();
        int success = 0;

        foreach (var p in processes)
        {
            try
            {
                p.PriorityClass = ProcessPriorityClass.Normal;
                success++;
            }
            catch
            {
            }
            finally
            {
                p.Dispose();
            }
        }

        ActionStatus.Text = success == 0
            ? "Không có tiến trình để khôi phục."
            : $"Đã khôi phục Normal cho {success} tiến trình.";

        RefreshProcesses();
    }

    private void Guide_Click(object sender, RoutedEventArgs e)
    {
        string fps =
            (FpsCombo.SelectedItem as ComboBoxItem)?
            .Content?.ToString() ?? "60 FPS";

        MessageBox.Show(
            $"Mục tiêu: {fps}\n\n" +
            "Mở BlueStacks → Settings → Performance.\n" +
            "Bật High frame rate nếu có.\n" +
            "Chọn FPS mà game và phần cứng hỗ trợ.\n" +
            "Điều chỉnh CPU/RAM vừa đủ.\n" +
            "Lưu thay đổi và khởi động lại nếu cần.\n\n" +
            "Ứng dụng không tự mở khóa FPS.",
            "PEDINHIOS • BlueStacks",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    protected override void OnClosed(EventArgs e)
    {
        timer.Stop();
        base.OnClosed(e);
    }
}
