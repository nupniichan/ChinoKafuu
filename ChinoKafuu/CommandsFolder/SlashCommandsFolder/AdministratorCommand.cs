using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Diagnostics;
using System.Management;


namespace ChinoBot.CommandsFolder.SlashCommandsFolder
{
    public class AdministratorCommand : ApplicationCommandModule
    {
        [SlashCommand("admin-help","Xem các câu lệnh được hỗ trợ")]
        public async Task AdminHelp(InteractionContext ctx)
        {
            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Danh sách các lệnh được hỗ trợ")
                .WithDescription("Dưới đây là danh sách các lệnh được Chino hỗ trợ hiện tại:")
                .WithColor(DiscordColor.Azure)
                .AddField("/ban", "Ban một người nào đó khỏi server")
                .AddField("/kick", "Đá người đó ra khỏi server")
                .AddField("/mute", "Cấm chat ( tính theo giây )")
                .AddField("/clear", "Xoá tin nhắn trong kênh")
                .AddField("/poll", "Tạo khảo sát ( chỉ có yes hoặc no )")
                .AddField("/system-help", "Xem các câu lệnh về hệ thống")
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        [SlashCommand("ban", "Bạn muốn ban ai đó khỏi server?")]
        public async Task BanCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                     [Option("reason", "Tại sao bạn muốn ban người đó?")] string reason = null)
        {
            await ctx.DeferAsync();

            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }

            if (ctx.User.Id == user.Id)
            {
                await SendSelfActionError(ctx, "ban");
                return;
            }

            try
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                await ctx.Guild.BanMemberAsync(member, 0, reason);

                var banEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.User.Username} đã ban {member.Username}")
                    .WithDescription($"Lý do: {reason ?? "Không có lý do"}")
                    .WithColor(DiscordColor.Green)
                    .WithTimestamp(DateTime.UtcNow);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(banEmbed));
            }
            catch (Exception ex)
            {
                await SendErrorEmbed(ctx, $"Không thể ban thành viên: {ex.Message}");
            }
        }

        [SlashCommand("kick", "Bạn muốn đá ai đó ra khỏi server?")]
        public async Task KickCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                      [Option("reason", "Tại sao bạn muốn kick người đó?")] string reason = null)
        {
            await ctx.DeferAsync();

            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }

            if (ctx.User.Id == user.Id)
            {
                await SendSelfActionError(ctx, "kick");
                return;
            }

            try
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                await member.RemoveAsync();

                var kickEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.User.Username} đã kick {member.Username}")
                    .WithDescription($"Lý do: {reason ?? "Không có lý do"}")
                    .WithColor(DiscordColor.Green)
                    .WithTimestamp(DateTime.UtcNow);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(kickEmbed));
            }
            catch (Exception ex)
            {
                await SendErrorEmbed(ctx, $"Không thể kick thành viên: {ex.Message}");
            }
        }

        [SlashCommand("mute", "Bạn muốn cấm chat ai đó?")]
        public async Task MuteCommand(InteractionContext ctx, [Option("user", "Người đó tên là gì?")] DiscordUser user,
                                      [Option("time", "Trong thời gian bao lâu? Tính theo giây")] long time,
                                      [Option("reason", "Tại sao bạn muốn mute người đó?")] string reason = null)
        {
            await ctx.DeferAsync();

            if (!ctx.Member.Permissions.HasPermission(Permissions.Administrator))
            {
                await SendPermissionError(ctx);
                return;
            }

            if (ctx.User.Id == user.Id)
            {
                await SendSelfActionError(ctx, "mute");
                return;
            }

            if (time <= 0 || time > 2419200) // Discord limit 28 days so if you hate them, then ban them instead lol
            {
                await SendErrorEmbed(ctx, "Thời gian không hợp lệ. Vui lòng nhập thời gian từ 1 đến 2419200 giây.");
                return;
            }

            try
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                var timeDuration = DateTime.Now + TimeSpan.FromSeconds(time);
                await member.TimeoutAsync(timeDuration);

                var muteEmbed = new DiscordEmbedBuilder()
                    .WithTitle($"{ctx.User.Username} đã cấm chat {member.Username}")
                    .WithDescription($"Lý do: {reason ?? "Không có lý do"}")
                    .WithColor(DiscordColor.Green)
                    .WithTimestamp(DateTime.UtcNow);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(muteEmbed));
            }
            catch (Exception ex)
            {
                await SendErrorEmbed(ctx, $"Không thể mute thành viên: {ex.Message}");
            }
        }

        [SlashCommand("clear", "Xóa tin nhắn trong kênh.")]
        public async Task PurgeCommand(InteractionContext ctx, [Option("count", "Số lượng tin nhắn cần xóa")] long count)
        {
            await ctx.DeferAsync(true); 

            if (!ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Bạn không có quyền quản lý tin nhắn."));
                return;
            }

            if (count < 1 || count > 100)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Số lượng tin nhắn cần xóa phải từ 1 đến 100."));
                return;
            }

            try
            {
                var messages = await ctx.Channel.GetMessagesAsync((int)count);

                await ctx.Channel.DeleteMessagesAsync(messages);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"✅ Đã xóa {count} tin nhắn trong kênh {ctx.Channel.Name}."));
            }
            catch (Exception ex)
            {

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"❌ Đã xảy ra lỗi: {ex.Message}"));
            }
        }

        [SlashCommand("poll", "Tạo cuộc khảo sát cho người dùng (Admin).")]
        public async Task PollCommand(InteractionContext ctx, [Option("question", "Câu hỏi khảo sát")] string question)
        {
            if (!ctx.Member.Permissions.HasPermission(DSharpPlus.Permissions.Administrator))
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("❌ Lỗi")
                    .WithDescription("Bạn không có quyền sử dụng lệnh này.")
                    .WithColor(DiscordColor.Red);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed));
                return;
            }

            var embedPoll = new DiscordEmbedBuilder()
                .WithTitle($"Khảo sát: {question}")
                .WithDescription("Sử dụng emoji :thumbsup: hoặc :thumbsdown: để trả lời.")
                .WithColor(DiscordColor.Goldenrod);

            var response = new DiscordInteractionResponseBuilder()
                .AddEmbed(embedPoll);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);

            var pollMessage = await ctx.GetOriginalResponseAsync();

            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
        }
        private async Task SendPermissionError(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription("Stop, định làm gì à?")
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private async Task SendSelfActionError(InteractionContext ctx, string action)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription($"Sao bro tại tự làm hại bản thân thế?")
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private async Task SendErrorEmbed(InteractionContext ctx, string message)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Lỗi")
                .WithDescription(message)
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

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
                .WithDescription("Có lỗi xảy ra" + errorMessage)
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
