using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Management;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ChinoKafuu.CommandsFolder.SlashCommandsFolder
{
    public class SystemStatusSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("system-help", "Xem các câu lệnh được hỗ trợ")]
        public async Task SystemHelp(InteractionContext ctx)
        {
            try
            {
                if (!ctx.Member.Permissions.HasPermission(DSharpPlus.Permissions.Administrator))
                {
                    var errorEmbed = new DiscordEmbedBuilder()
                        .WithTitle("❌ Lỗi")
                        .WithDescription("Bạn không có quyền sử dụng lệnh này.")
                        .WithColor(DiscordColor.Red)
                        .WithFooter("Liên hệ quản trị viên nếu bạn cần quyền truy cập.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Danh sách các lệnh được hỗ trợ")
                    .WithDescription("Dưới đây là các lệnh hệ thống mà Chino hỗ trợ hiện tại:")
                    .WithColor(DiscordColor.Azure)
                    .AddField("`/process-performance`", "Hiển thị tiến trình của Chino (CPU, RAM).", true)
                    .AddField("`/system-performance`", "Hiển thị hiệu suất hệ thống (CPU, RAM, cho admin).", true)
                    .AddField("`/full-system-performance`", "Thông tin đầy đủ về hiệu suất hệ thống.", true)
                    .WithFooter("Sử dụng lệnh `/tên-lệnh` để thực thi.")
                    .WithTimestamp(DateTime.Now);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorMessage(ctx, e.Message);
            }
        }

        [SlashCommand("process-performance", "Hiển thị hiệu suất tiến trình Chino (CPU, RAM).")]
        public async Task ProcessPerformanceCommand(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync();

                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var cpuUsagePercentage = GetCpuUsageForProcess(currentProcess);
                var memoryUsage = currentProcess.WorkingSet64 / 1024 / 1024;

                var embed = new DiscordEmbedBuilder()
                    .WithTitle("📊 Hiệu suất tiến trình Chino")
                    .AddField("Sử dụng CPU (Chino)", $"{cpuUsagePercentage:F2}%", true)
                    .AddField("Sử dụng RAM (Chino)", $"{memoryUsage:F2} MB", true)
                    .WithColor(DiscordColor.Blue)
                    .WithFooter($"Thời gian kiểm tra: {DateTime.Now}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorMessage(ctx, e.Message);
            }
        }

        [SlashCommand("system-performance", "Hiển thị hiệu suất hệ thống (CPU, RAM, cho admin).")]
        public async Task SystemPerformanceCommand(InteractionContext ctx)
        {
            try
            {
                if (!ctx.Member.Permissions.HasPermission(DSharpPlus.Permissions.Administrator))
                {
                    var errorEmbed = new DiscordEmbedBuilder()
                        .WithTitle("❌ Lỗi")
                        .WithDescription("Bạn không có quyền sử dụng lệnh này.")
                        .WithColor(DiscordColor.Red)
                        .WithFooter("Liên hệ quản trị viên nếu bạn cần quyền truy cập.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                    return;
                }

                await ctx.DeferAsync();

                var cpuUsage = GetWmiValue("Win32_Processor", "LoadPercentage");
                var totalMemory = Convert.ToDouble(GetWmiValue("Win32_ComputerSystem", "TotalPhysicalMemory")) / 1024 / 1024;
                var freeMemory = Convert.ToDouble(GetWmiValue("Win32_OperatingSystem", "FreePhysicalMemory")) / 1024;

                var systemEmbed = new DiscordEmbedBuilder()
                    .WithTitle("📊 Hiệu suất hệ thống (Windows)")
                    .AddField("Sử dụng CPU", $"{cpuUsage}%", true)
                    .AddField("RAM Tổng", $"{totalMemory:F2} MB", true)
                    .AddField("RAM Trống", $"{freeMemory:F2} MB", true)
                    .AddField("RAM Sử dụng", $"{(totalMemory - freeMemory):F2} MB", true)
                    .WithColor(DiscordColor.Green)
                    .WithFooter($"Thời gian kiểm tra: {DateTime.Now}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(systemEmbed));
            }
            catch (Exception e)
            {
                await SendErrorMessage(ctx, e.Message);
            }
        }

        [SlashCommand("full-system-performance", "Hiển thị thông tin hệ thống đầy đủ (Cross-platform).")]
        public async Task FullSystemPerformanceCommand(InteractionContext ctx)
        {
            try
            {
                if (!ctx.Member.Permissions.HasPermission(DSharpPlus.Permissions.Administrator))
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle("❌ Lỗi")
                        .WithDescription("Bạn không có quyền sử dụng lệnh này.")
                        .WithColor(DiscordColor.Red)
                        .WithFooter("Liên hệ quản trị viên nếu bạn cần quyền truy cập.");

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }

                await ctx.DeferAsync();

                var cpuInfo = GetWmiValue("Win32_Processor", "Name");
                var cpuUsage = GetCpuUsage();

                var totalRam = Convert.ToDouble(GetWmiValue("Win32_ComputerSystem", "TotalPhysicalMemory")) / 1024 / 1024;
                var availableRam = Convert.ToDouble(GetWmiValue("Win32_OperatingSystem", "FreePhysicalMemory")) / 1024;
                var usedRam = totalRam - availableRam;

                var gpuInfo = GetGpuUsage();
                var diskInfo = GetDiskUsage();
                var wifiStatus = GetWifiStatus();

                var fullSystemPerformanceEmbed = new DiscordEmbedBuilder()
                    .WithTitle("📊 Thông tin hiệu suất hệ thống đầy đủ")
                    .AddField("CPU", $"{cpuInfo} - {cpuUsage:F2}% sử dụng", false)
                    .AddField("RAM Tổng", $"{totalRam:F2} MB", true)
                    .AddField("RAM Trống", $"{availableRam:F2} MB", true)
                    .AddField("RAM Đang sử dụng", $"{usedRam:F2} MB", true)
                    .AddField("GPU", $"{gpuInfo}", false)
                    .AddField("Ổ đĩa", diskInfo, false)
                    .AddField("Kết nối WiFi", wifiStatus, true)
                    .WithColor(DiscordColor.Blurple)
                    .WithFooter($"Thời gian kiểm tra: {DateTime.Now}")
                    .WithTimestamp(DateTime.Now);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(fullSystemPerformanceEmbed));
            }
            catch (Exception e)
            {
                await SendErrorMessage(ctx, e.Message);
            }
        }

        private string GetWmiValue(string className, string propertyName)
        {
            var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            foreach (var queryObj in searcher.Get())
            {
                return queryObj[propertyName]?.ToString() ?? "Không xác định";
            }
            return "Không xác định";
        }

        private double GetCpuUsageForProcess(Process process)
        {
            var startCpuTime = process.TotalProcessorTime.TotalMilliseconds;
            var startTime = DateTime.UtcNow;

            System.Threading.Thread.Sleep(1000); 

            var endCpuTime = process.TotalProcessorTime.TotalMilliseconds;
            var endTime = DateTime.UtcNow;

            var cpuUsedMs = endCpuTime - startCpuTime;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;

            return (cpuUsedMs / totalMsPassed) * 100;
        }

        private double GetCpuUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); 
            System.Threading.Thread.Sleep(1000); 
            return cpuCounter.NextValue();
        }

        private string GetGpuUsage()
        {
            var gpuInfo = string.Empty;
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var queryObj in searcher.Get())
            {
                var gpuName = queryObj["Name"]?.ToString() ?? "Không xác định";
                var gpuUsage = GetGpuLoad(gpuName);
                gpuInfo += $"{gpuName} - {gpuUsage} % sử dụng\n";
            }
            return string.IsNullOrEmpty(gpuInfo) ? "Không có GPU hoặc không xác định" : gpuInfo;
        }

        private string GetGpuLoad(string gpuName)
        {
            return "Chưa thể đo được"; // I will do it later
        }

        private string GetDiskUsage()
        {
            var diskInfo = string.Empty;
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3");
            foreach (var queryObj in searcher.Get())
            {
                var diskName = queryObj["DeviceID"]?.ToString() ?? "Không xác định";
                var diskSize = Convert.ToDouble(queryObj["Size"] ?? 0) / 1024 / 1024 / 1024; 
                var diskFree = Convert.ToDouble(queryObj["FreeSpace"] ?? 0) / 1024 / 1024 / 1024; 
                var diskUsage = 100 - (diskFree / diskSize * 100);
                diskInfo += $"{diskName} - {diskUsage:F2}% sử dụng ({diskSize:F2} GB total, {diskFree:F2} GB trống)\n";
            }
            return string.IsNullOrEmpty(diskInfo) ? "Không xác định" : diskInfo;
        }

        private string GetWifiStatus()
        {
            var wifiInfo = "Không có kết nối WiFi";
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL");
            foreach (var queryObj in searcher.Get())
            {
                var connectionType = queryObj["NetConnectionID"]?.ToString();
                if (connectionType != null && connectionType.Contains("Wi-Fi"))
                {
                    wifiInfo = $"Kết nối mạng: {connectionType}";
                }
            }
            return wifiInfo;
        }
        private async Task SendErrorMessage(InteractionContext ctx, string errorMessage)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription("Có lỗi xảy ra" +errorMessage)
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
