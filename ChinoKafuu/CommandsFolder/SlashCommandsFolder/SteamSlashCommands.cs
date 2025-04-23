using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using csteamworks.Services;
using ChinoBot.config;
using DSharpPlus.Interactivity.Extensions;
using csteamworks.Models.App;
using System.Text;
using csteamworks.Models.App.Components;
using csteamworks.Models.User;

namespace ChinoKafuu.CommandsFolder.SlashCommandsFolder
{
    public class SteamSlashCommands : ApplicationCommandModule
    {
        private const string STEAM_LOGO = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/83/Steam_icon_logo.svg/768px-Steam_icon_logo.svg.png";
        private const string STEAM_URL = "https://store.steampowered.com/";
        private readonly DiscordColor STEAM_COLOR = new DiscordColor(27, 40, 56);
        public readonly Config config;
        private readonly CsSteamUser steamUser = new CsSteamUser();
        private readonly CsSteamApp steamApp = new CsSteamApp();

        public SteamSlashCommands()
        {
            config = new Config();
            config.ReadConfigFile().GetAwaiter().GetResult();
        }

        [SlashCommand("steam-help", "Hiển thị trợ giúp về các lệnh Steam")]
        public async Task SteamHelpCommand(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("🎮 Danh sách các lệnh Steam")
                .WithDescription("Chino hỗ trợ các câu lệnh steam sau nè (≧ᗜ≦ ):")
                .WithColor(STEAM_COLOR)
                .WithThumbnail(STEAM_LOGO)
                .AddField("/steam-user", "Tìm profile trên Steam", true)
                .AddField("/steam-game", "Tìm thông tin game", true)
                .AddField("/steam-players", "Xem số người chơi hiện tại của game", true)
                .AddField("/steam-recent", "Xem các game đã chơi gần đây", true)
                .AddField("/steam-library", "Xem thư viện game", true)
                .AddField("/steam-top", "Xem top 100 games phổ biến", true)
                .WithFooter("Để sử dụng lệnh cụ thể, nhập /tên-lệnh");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("steam-user", "Tìm profile trên Steam")]
        public async Task SteamUserCommand(InteractionContext ctx, [Option("username", "Tên người dùng Steam")] string username)
        {
            await ctx.DeferAsync();
            try
            {
                var userId = await steamUser.GetUserID(config.steamApiKey, username);
                var userStats = await steamUser.GetUserStats(config.steamApiKey, userId.steamid);
                var playerSummary = await steamUser.GetPlayerSummaries(config.steamApiKey, userId.steamid);

                DiscordColor statusColor = GetStatusColor(userStats.OnlineStatus);

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Steam Profile", null, STEAM_LOGO)
                    .WithTitle("Tên steam:" + userStats.PlayerName)
                    .WithUrl(userStats.ProfileUrl)
                    .WithThumbnail(userStats.AvatarUrl)
                    .WithColor(statusColor)
                    .AddField(":identification_card: Steam ID", userId.steamid, true)
                    .AddField("📆 Ngày tham gia", userStats.AccountCreated != DateTime.MinValue ? userStats.AccountCreated.ToString("dd/MM/yyyy") : "Không có", true);

                if (!string.IsNullOrEmpty(playerSummary.realname))
                    embed.AddField("👤 Tên thật", playerSummary.realname, true);

                embed.AddField(":beginner: Trạng thái", userStats.OnlineStatus, true);

                string location = GetUserLocation(playerSummary);
                if (!string.IsNullOrEmpty(location))
                    embed.AddField("📍 Vị trí", location, true);

                if (!string.IsNullOrEmpty(playerSummary.gameextrainfo))
                    embed.AddField("🎮 Đang chơi", playerSummary.gameextrainfo, false);

                DateTime lastOnline = playerSummary.LastLogoffDate;
                if (lastOnline != DateTime.MinValue && userStats.OnlineStatus.ToLower() == "offline")
                    embed.AddField("⏱️ Truy cập lần cuối", $"{lastOnline:dd/MM/yyyy HH:mm}", true);

                embed.AddField("🎮 Tổng games", userStats.TotalGamesOwned.ToString(), true)
                    .AddField("⏱️ Tổng thời gian chơi", $"{userStats.TotalPlaytimeHours} giờ", true);

                embed.AddField("🕹️ Hoạt động gần đây", userStats.Recent2WeeksPlaytimeHours > 0 ?
                    $"{userStats.Recent2WeeksPlaytimeHours} giờ trong 2 tuần qua" : "Không có hoạt động", false);

                try
                {
                    var badges = await steamUser.GetPlayerBadges(config.steamApiKey, userId.steamid);
                    if (badges.Count > 0)
                    {
                        int totalXp = badges.Sum(b => b.XP);
                        int badgeCount = badges.Count;
                        embed.AddField("🏅 Huy hiệu", $"{badgeCount} huy hiệu ({totalXp} XP)", true);
                    }
                }
                catch { }

                if (userStats.MostPlayedGames != null && userStats.MostPlayedGames.Count > 0)
                {
                    embed.AddField("🏆 Top 3 game chơi nhiều nhất", "Các game được chơi nhiều nhất:", false);
                    
                    foreach (var game in userStats.MostPlayedGames.Take(3))
                    {
                        var playtime = TimeSpan.FromMinutes(game.playtime_forever);
                        string playTimeStr = $"{Math.Floor(playtime.TotalHours)}.{playtime.Minutes:D2}";
                        
                        embed.AddField($"🎮 {game.name}", $"{playTimeStr} giờ", true);
                    }
                }

                embed.WithFooter($"Steam • {STEAM_URL}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, e.Message);
            }
        }

        private string GetUserLocation(SteamUser user)
        {
            List<string> locationParts = new List<string>();
              
            if (!string.IsNullOrEmpty(user.loccountrycode))
                locationParts.Add(user.loccountrycode);
                
            return string.Join(", ", locationParts);
        }

        [SlashCommand("steam-game", "Tìm thông tin game")]
        public async Task SteamGameCommand(InteractionContext ctx, [Option("name", "Tên game")] string name)
        {
            await ctx.DeferAsync();
            try
            {
                var allApps = await steamApp.GetSteamAppIdData(name);
                if (allApps.Count == 0)
                {
                    await SendErrorEmbed(ctx, $"Không tìm thấy game: {name}");
                    return;
                }

                var filteredApps = FilterGameResults(allApps);
                
                if (filteredApps.Count == 0)
                    filteredApps = allApps;

                if (filteredApps.Count == 1)
                {
                    await DisplayGameDetails(ctx, filteredApps.First().appid);
                    return;
                }

                var mainGame = filteredApps.OrderBy(a => a.name.Length).FirstOrDefault();
                if (mainGame == null)
                {
                    await SendErrorEmbed(ctx, $"Không thể xác định game chính cho: {name}");
                    return;
                }

                var details = await steamApp.GetSteamAppDetails(mainGame.appid);
                int playerCount = 0;
                
                try
                {
                    playerCount = details.Data.CurrentPlayerCount;
                }
                catch { }
                
                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Steam Game", null, STEAM_LOGO)
                    .WithTitle(details.Data.Name)
                    .WithUrl(details.Data.StorePageUrl)
                    .WithColor(STEAM_COLOR);
                
                if (!string.IsNullOrEmpty(details.Data.ShortDescription))
                    embed.WithDescription(details.Data.ShortDescription);
                
                if (!string.IsNullOrEmpty(details.Data.HeaderImage))
                    embed.WithThumbnail(details.Data.HeaderImage);

                if (details.Data.Developers != null && details.Data.Developers.Count > 0)
                    embed.AddField("🧑‍💻 Nhà phát triển", string.Join(", ", details.Data.Developers), true);

                if (details.Data.Publishers != null && details.Data.Publishers.Count > 0)
                    embed.AddField("🏢 Nhà phát hành", string.Join(", ", details.Data.Publishers), true);

                string releaseDate = "Không có";
                try
                {
                    releaseDate = details.Data.ReleaseDate?.Date ?? "Không có";
                }
                catch { }

                string price = "Miễn phí";
                try
                {
                    price = details.Data.PriceOverview?.final_formatted ?? "Miễn phí";
                } 
                catch { }

                embed.AddField(":calendar_spiral: Ngày phát hành", releaseDate, true)
                    .AddField("💰 Giá", price, true)
                    .AddField("👥 Số người chơi hiện tại", playerCount > 0 ? playerCount.ToString("N0") : "Không có dữ liệu", true);

                if (details.Data.Genres != null && details.Data.Genres.Count > 0)
                {
                    try
                    {
                        embed.AddField("🎮 Thể loại", string.Join(", ", details.Data.Genres.Select(g => g.description)), false);
                    }
                    catch { }
                }

                var dlcList = new List<SteamApp>();
                
                if (details.Data.Dlc != null && details.Data.Dlc.Count > 0)
                {
                    foreach (var appId in details.Data.Dlc.Take(5))
                    {
                        try
                        {
                            var dlcDetails = await steamApp.GetSteamAppDetails(appId);
                            if (dlcDetails.Success)
                                dlcList.Add(new SteamApp { appid = appId, name = dlcDetails.Data.Name });
                        }
                        catch { }
                    }
                }
                else
                {
                    dlcList = filteredApps
                        .Where(a => a.appid != mainGame.appid && 
                               (a.name.IndexOf("dlc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                a.name.IndexOf("expansion", StringComparison.OrdinalIgnoreCase) >= 0))
                        .Take(5)
                        .ToList();
                }
                
                if (dlcList.Count > 0)
                {
                    var dlcField = "";
                    foreach (var dlc in dlcList)
                        dlcField += $"• [{dlc.name}](https://store.steampowered.com/app/{dlc.appid}) (ID: {dlc.appid})\n";
                    
                    embed.AddField("📦 DLC & Expansions", dlcField, false);
                }
                
                embed.AddField("🔍 Xem chi tiết", $"Chọn lựa chọn bên dưới để xem full thông tin", false);
                embed.WithFooter($"Steam • AppID: {mainGame.appid}");

                var options = new List<DiscordSelectComponentOption>();
                
                options.Add(new DiscordSelectComponentOption(
                    "Xem chi tiết đầy đủ", 
                    $"detail_{mainGame.appid}", 
                    $"Xem thông tin đầy đủ của {mainGame.name}"
                ));
                
                foreach (var dlc in dlcList)
                {
                    options.Add(new DiscordSelectComponentOption(
                        dlc.name.Length > 80 ? dlc.name.Substring(0, 77) + "..." : dlc.name,
                        $"dlc_{dlc.appid}",
                        $"Xem thông tin về {dlc.name}"
                    ));
                }
                
                try
                {
                    var selectMenu = new DiscordSelectComponent(
                        "game_options",
                        "Chọn để xem thêm thông tin...",
                        options
                    );
                    
                    var builder = new DiscordWebhookBuilder()
                        .AddEmbed(embed)
                        .AddComponents(selectMenu);
                        
                    var message = await ctx.EditResponseAsync(builder);
                    
                    try
                    {
                        var interactivity = ctx.Client.GetInteractivity();
                        var response = await interactivity.WaitForSelectAsync(
                            message, 
                            "game_options", 
                            TimeSpan.FromMinutes(2)
                        );
                        
                        if (!response.TimedOut && response.Result != null)
                        {
                            var selectedValue = response.Result.Values.FirstOrDefault();
                            
                            await response.Result.Interaction.CreateResponseAsync(
                                InteractionResponseType.DeferredMessageUpdate
                            );
                            
                            if (selectedValue != null)
                            {
                                if (selectedValue.StartsWith("detail_") && int.TryParse(selectedValue.Substring(7), out int gameId))
                                    await DisplayGameDetails(ctx, gameId);
                                else if (selectedValue.StartsWith("dlc_") && int.TryParse(selectedValue.Substring(4), out int dlcId))
                                    await DisplayGameDetails(ctx, dlcId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi xử lý tương tác dropdown: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tạo dropdown: {ex.Message}");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, $"Lỗi xảy ra khi tìm thông tin game: {e.Message}");
            }
        }
        
        private List<SteamApp> FilterGameResults(List<SteamApp> apps)
        {
            var excludeKeywords = new string[]
            {
                "soundtrack", "sound track", "ost",
                "bonus", "gesture", "emoticon", "emote",
                "wallpaper", "artbook", "art book", 
                "avatar", "skin", "costume", "outfit",
                "behind the scenes", "documentary",
                "manual", "guide", "tools", "editor",
                "demo", "playtest", "beta", "server",
                "pre-purchase", "pack"
            };
            
            var exceptionKeywords = new string[]
            {
                "dlc", "expansion", "season pass", "content pack",
                "complete", "gold", "definitive", "ultimate", "deluxe",
                "goty", "game of the year"
            };
            
            var filteredApps = new List<SteamApp>();
            
            foreach (var app in apps)
            {
                if (string.IsNullOrEmpty(app.name))
                    continue;
                
                bool isException = exceptionKeywords.Any(keyword => 
                    app.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                
                if (isException)
                {
                    filteredApps.Add(app);
                    continue;
                }
                
                bool shouldExclude = excludeKeywords.Any(keyword => 
                    app.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                
                if (!shouldExclude)
                    filteredApps.Add(app);
            }
            
            return filteredApps.OrderBy(a => a.name.Length).ToList();
        }

        // Dang xem xet them that su can ko tai luoi qua
        [SlashCommand("steam-gameid", "Xem thông tin game theo ID")]
        public async Task SteamGameIdCommand(InteractionContext ctx, 
            [Option("id", "ID của game trên Steam")] long id)
        {
            await ctx.DeferAsync();
            try
            {
                await DisplayGameDetails(ctx, (int)id);
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, $"Lỗi xảy ra khi tìm thông tin game ID {id}: {e.Message}");
            }
        }

        private async Task DisplayGameDetails(InteractionContext ctx, int appId)
        {
            try
            {
                var details = await steamApp.GetSteamAppDetails(appId);

                if (!details.Success)
                {
                    await SendErrorEmbed(ctx, $"Không thể lấy thông tin chi tiết của game với ID: {appId}");
                    return;
                }

                var data = details.Data;
                int playerCount = 0;
                
                try
                {
                    playerCount = data.CurrentPlayerCount;
                }
                catch { }
                
                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Steam Game", null, STEAM_LOGO)
                    .WithTitle(data.Name)
                    .WithUrl(data.StorePageUrl)
                    .WithColor(STEAM_COLOR);

                var gameSummary = CreateGameSummary(data, playerCount);
                embed.WithDescription(gameSummary);
                
                if (!string.IsNullOrEmpty(data.HeaderImage))
                    embed.WithThumbnail(data.HeaderImage);

                try
                {
                    if (data.Developers?.Count > 0)
                        embed.AddField("🧑‍💻 Nhà phát triển", string.Join(", ", data.Developers), true);

                    if (data.Publishers?.Count > 0)
                        embed.AddField("🏢 Nhà phát hành", string.Join(", ", data.Publishers), true);

                    string releaseDate = "Không có";
                    try
                    {
                        releaseDate = data.ReleaseDate?.Date ?? "Không có";
                    }
                    catch { }

                    string price = "Miễn phí";
                    try
                    {
                        price = data.PriceOverview?.final_formatted ?? "Miễn phí";
                    } 
                    catch { }

                    embed.AddField(":calendar_spiral: Ngày phát hành", releaseDate, true)
                        .AddField("💰 Giá", price, true)
                        .AddField("👥 Số người chơi hiện tại", playerCount > 0 ? playerCount.ToString("N0") : "Không có dữ liệu", true);

                    try
                    {
                        embed.AddField("⭐ Đánh giá", data.Metacritic?.score != null ? $"{data.Metacritic.score}/100" : "Không có", true);
                    }
                    catch
                    {
                        embed.AddField("⭐ Đánh giá", "Không có", true);
                    }

                    if (data.Categories?.Count > 0)
                    {
                        try
                        {
                            embed.AddField("🎲 Steam hỗ trợ", string.Join(", ", data.Categories.Select(c => c.description).Take(5)), false);
                        }
                        catch { }
                    }

                    if (data.Genres?.Count > 0)
                    {
                        try
                        {
                            embed.AddField("🎮 Thể loại", string.Join(", ", data.Genres.Select(g => g.description)), false);
                        }
                        catch { }
                    }

                    if (data.Tags?.Count > 0)
                    {
                        try
                        {
                            embed.AddField("🏷️ Tags", string.Join(", ", data.Tags.Keys.Take(10)), false);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xử lý thông tin game: {ex.Message}");
                }

                try
                {
                    if (data.PcRequirements != null)
                    {
                        try
                        {
                            if (data.PcRequirements.MinimumRequirements != null)
                            {
                                var minReqsText = FormatSystemRequirements(data.PcRequirements.MinimumRequirements);
                                if (!string.IsNullOrEmpty(minReqsText))
                                    embed.AddField("💻 Yêu cầu tối thiểu", minReqsText, false);
                            }
                            
                            if (data.PcRequirements.RecommendedRequirements != null)
                            {
                                var recReqsText = FormatSystemRequirements(data.PcRequirements.RecommendedRequirements);
                                if (!string.IsNullOrEmpty(recReqsText))
                                    embed.AddField("🖥️ Yêu cầu đề nghị", recReqsText, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Lỗi khi phân tích yêu cầu hệ thống: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi truy cập PcRequirements: {ex.Message}");
                }

                embed.WithFooter($"Steam • AppID: {appId}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, $"Lỗi xảy ra khi hiển thị chi tiết game: {e.Message}");
                Console.WriteLine($"Lỗi chi tiết: {e}");
            }
        }
        
        private string CreateGameSummary(SteamAppDetail gameData, int playerCount)
        {
            var summary = new StringBuilder();
            
            if (!string.IsNullOrEmpty(gameData.ShortDescription))
            {
                summary.AppendLine(gameData.ShortDescription);
                summary.AppendLine();
            }
            
            summary.AppendLine("**• Thông tin chính •**");
            
            if (gameData.Developers?.Count > 0)
                summary.AppendLine($"👨‍💻 **Phát triển bởi:** {string.Join(", ", gameData.Developers)}");
                
            if (gameData.Publishers?.Count > 0 && 
                (gameData.Developers == null || !gameData.Developers.SequenceEqual(gameData.Publishers)))
                summary.AppendLine($"🏢 **Phát hành bởi:** {string.Join(", ", gameData.Publishers)}");
            
            if (gameData.ReleaseDate != null && !string.IsNullOrEmpty(gameData.ReleaseDate.Date))
                summary.AppendLine($"📅 **Phát hành:** {gameData.ReleaseDate.Date}");
                
            string price = "Miễn phí";
            if (gameData.PriceOverview != null && !string.IsNullOrEmpty(gameData.PriceOverview.final_formatted))
                price = gameData.PriceOverview.final_formatted;
            summary.AppendLine($"💰 **Giá:** {price}");
            
            if (playerCount > 0)
                summary.AppendLine($"👥 **Người chơi hiện tại:** {playerCount.ToString("N0")} người");
                
            if (gameData.Metacritic?.score != null)
                summary.AppendLine($"⭐ **Đánh giá Metacritic:** {gameData.Metacritic.score}/100");
            
            if (gameData.Genres != null && gameData.Genres.Count > 0)
            {
                var genreNames = gameData.Genres.Select(g => g.description).Take(5);
                summary.AppendLine($"🎯 **Thể loại:** {string.Join(", ", genreNames)}");
            }
            
            if (gameData.Platforms != null)
            {
                List<string> platforms = new List<string>();
                
                if (gameData.Platforms.windows) platforms.Add("Windows");
                if (gameData.Platforms.mac) platforms.Add("Mac");
                if (gameData.Platforms.linux) platforms.Add("Linux");
                
                if (platforms.Count > 0)
                    summary.AppendLine($"💻 **Nền tảng:** {string.Join(", ", platforms)}");
            }
            
            return summary.ToString();
        }

        [SlashCommand("steam-players", "Xem số người chơi hiện tại của game")]
        public async Task SteamPlayersCommand(InteractionContext ctx, [Option("name", "Tên game")] string name)
        {
            await ctx.DeferAsync();
            try
            {
                var apps = await steamApp.GetSteamAppIdData(name);
                if (apps.Count == 0)
                {
                    await SendErrorEmbed(ctx, $"Không tìm thấy game: {name}");
                    return;
                }

                var app = apps.First();
                var playerCount = await steamApp.GetCurrentPlayerCount(app.appid);
                
                var details = await steamApp.GetSteamAppDetails(app.appid);
                string gameName = details.Success ? details.Data.Name : app.name;
                string imageUrl = details.Success ? details.Data.HeaderImage : null;

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Steam Players", null, STEAM_LOGO)
                    .WithTitle(gameName)
                    .WithColor(STEAM_COLOR)
                    .AddField("👥 Số người chơi hiện tại", playerCount.ToString("N0"), true);

                if (!string.IsNullOrEmpty(imageUrl))
                    embed.WithThumbnail(imageUrl);

                if (details.Success)
                    embed.WithUrl(details.Data.StorePageUrl);

                embed.WithFooter($"Steam • AppID: {app.appid}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, e.Message);
            }
        }

        [SlashCommand("steam-recent", "Xem các game đã chơi gần đây")]
        public async Task SteamRecentCommand(InteractionContext ctx, [Option("username", "Tên người dùng Steam")] string username)
        {
            await ctx.DeferAsync();
            try
            {
                var userId = await steamUser.GetUserID(config.steamApiKey, username);
                var userStats = await steamUser.GetUserStats(config.steamApiKey, userId.steamid);
                
                if (userStats.RecentlyPlayedGames == null || userStats.RecentlyPlayedGames.Count == 0)
                {
                    await SendErrorEmbed(ctx, $"Không tìm thấy game đã chơi gần đây của người dùng: {username}");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Steam Recent Games", null, STEAM_LOGO)
                    .WithTitle($"🕹️ Game đã chơi gần đây của {userStats.PlayerName}")
                    .WithColor(STEAM_COLOR)
                    .WithThumbnail(userStats.AvatarUrl)
                    .AddField("⏱️ Tổng thời gian chơi 2 tuần qua", $"{userStats.Recent2WeeksPlaytimeHours} giờ", false);

                foreach (var game in userStats.RecentlyPlayedGames.Take(10))
                {
                    var playtime = TimeSpan.FromMinutes(game.playtime_2weeks);
                    var totalHours = Math.Floor(playtime.TotalHours);
                    embed.AddField($"🎮 {game.name}", $"{totalHours}.{playtime.Minutes:D2} giờ trong 2 tuần qua", false);
                }

                embed.WithFooter($"Steam • ID: {userId.steamid}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, e.Message);
            }
        }

        [SlashCommand("steam-library", "Xem thư viện game")]
        public async Task SteamLibraryCommand(InteractionContext ctx, [Option("username", "Tên người dùng Steam")] string username)
        {
            await ctx.DeferAsync();
            try
            {
                var userId = await steamUser.GetUserID(config.steamApiKey, username);
                var userStats = await steamUser.GetUserStats(config.steamApiKey, userId.steamid);

                if (userStats.TotalGamesOwned == 0)
                {
                    await SendErrorEmbed(ctx, $"Không tìm thấy thư viện game của người dùng: {username}");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Steam Library", null, STEAM_LOGO)
                    .WithTitle($"📚 Thư viện game của {userStats.PlayerName}")
                    .WithColor(STEAM_COLOR)
                    .WithThumbnail(userStats.AvatarUrl)
                    .AddField("🎮 Tổng số game", userStats.TotalGamesOwned.ToString("N0"), true)
                    .AddField("⏱️ Tổng thời gian chơi", $"{userStats.TotalPlaytimeHours} giờ", true)
                    .AddField("📊 Tỷ lệ đã chơi", $"{userStats.PercentageOfLibraryPlayed}%", true)
                    .AddField("⏲️ Trung bình mỗi game", $"{userStats.AveragePlaytimePerGameHours} giờ", true);

                if (userStats.MostPlayedGames != null && userStats.MostPlayedGames.Count > 0)
                {
                    embed.AddField("🏆 Top game đã chơi", "Các game chơi nhiều nhất:", false);
                    
                    foreach (var game in userStats.MostPlayedGames.Take(5))
                    {
                        var playtime = TimeSpan.FromMinutes(game.playtime_forever);
                        var totalHours = Math.Floor(playtime.TotalHours);
                        embed.AddField($"🎮 {game.name}", $"{totalHours}.{playtime.Minutes:D2} giờ", true);
                    }
                }

                embed.WithFooter($"Steam • ID: {userId.steamid}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, e.Message);
            }
        }

        [SlashCommand("steam-top", "Xem top games phổ biến")]
        public async Task SteamTopGamesCommand(InteractionContext ctx, [Option("count", "Số lượng game hiển thị (1-10)")] long count = 5)
        {
            await ctx.DeferAsync();
            try
            {
                int gameCount = Math.Min(Math.Max((int)count, 1), 10);
                
                List<SteamApp> allTopGames;
                try
                {
                    allTopGames = await steamApp.GetSteamTopGames(100);
                }
                catch
                {
                    try
                    {
                        allTopGames = await steamApp.GetSteamTopGames(20);
                    }
                    catch(Exception ex)
                    {
                        await SendErrorEmbed(ctx, $"Không thể lấy danh sách top games: {ex.Message}");
                        return;
                    }
                }
                
                if (allTopGames.Count == 0)
                {
                    await SendErrorEmbed(ctx, "Không thể lấy danh sách top games");
                    return;
                }

                await DisplayTopGamesPage(ctx, allTopGames, gameCount, 0);
            }
            catch (Exception e)
            {
                await SendErrorEmbed(ctx, $"Lỗi xảy ra khi xem top games: {e.Message}");
            }
        }
        
        private async Task DisplayTopGamesPage(InteractionContext ctx, List<SteamApp> allGames, int gamesPerPage, int currentPage)
        {
            int startIndex = currentPage * gamesPerPage;
            int endIndex = Math.Min(startIndex + gamesPerPage, allGames.Count);
            
            var pageGames = allGames.Skip(startIndex).Take(gamesPerPage).ToList();

            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Steam Top Games", null, STEAM_LOGO)
                .WithTitle("🏆 Top Games phổ biến trên Steam")
                .WithColor(STEAM_COLOR)
                .WithThumbnail(STEAM_LOGO);
            
            for (int i = 0; i < pageGames.Count; i++)
            {
                var game = pageGames[i];
                string developer = !string.IsNullOrEmpty(game.developer) ? $" | 🧑‍💻 {game.developer}" : "";
                string publisher = !string.IsNullOrEmpty(game.publisher) ? $" | 🏢 {game.publisher}" : "";
                int rank = startIndex + i + 1;
                
                embed.AddField($"#{rank} - {game.name}", 
                    $":video_game: AppID: {game.appid}{developer}{publisher}\n[Xem trên Steam](https://store.steampowered.com/app/{game.appid})", 
                    false);
            }
            
            int totalPages = (int)Math.Ceiling(allGames.Count / (double)gamesPerPage);
            embed.WithFooter($"Steam • Trang {currentPage + 1}/{totalPages} • Hiển thị {startIndex + 1}-{endIndex} của {allGames.Count} games");
            
            if (totalPages <= 1)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }
            
            var builder = new DiscordWebhookBuilder();
            
            var buttons = new List<DiscordButtonComponent>
            {
                new DiscordButtonComponent(
                    ButtonStyle.Primary, 
                    "btn_first", 
                    "⏮️", 
                    currentPage == 0),
                    
                new DiscordButtonComponent(
                    ButtonStyle.Secondary, 
                    "btn_prev", 
                    "◀️", 
                    currentPage == 0),
                    
                new DiscordButtonComponent(
                    ButtonStyle.Success, 
                    "btn_info", 
                    $"{currentPage + 1}/{totalPages}", 
                    true),
                    
                new DiscordButtonComponent(
                    ButtonStyle.Secondary, 
                    "btn_next", 
                    "▶️", 
                    currentPage >= totalPages - 1),
                    
                new DiscordButtonComponent(
                    ButtonStyle.Primary, 
                    "btn_last", 
                    "⏭️", 
                    currentPage >= totalPages - 1)
            };
            
            builder.AddEmbed(embed)
                .AddComponents(buttons);
            
            var message = await ctx.EditResponseAsync(builder);
            
            try
            {
                var interactivity = ctx.Client.GetInteractivity();
                var result = await interactivity.WaitForButtonAsync(message, TimeSpan.FromMinutes(2));
                
                if (result.TimedOut) return;
                
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                
                switch (result.Result.Id)
                {
                    case "btn_first":
                        await DisplayTopGamesPage(ctx, allGames, gamesPerPage, 0);
                        break;
                    case "btn_prev":
                        await DisplayTopGamesPage(ctx, allGames, gamesPerPage, Math.Max(0, currentPage - 1));
                        break;
                    case "btn_next":
                        await DisplayTopGamesPage(ctx, allGames, gamesPerPage, Math.Min(totalPages - 1, currentPage + 1));
                        break;
                    case "btn_last":
                        await DisplayTopGamesPage(ctx, allGames, gamesPerPage, totalPages - 1);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xử lý tương tác nút: {ex.Message}");
            }
        }

        private DiscordColor GetStatusColor(string onlineStatus)
        {
            if (string.IsNullOrEmpty(onlineStatus))
                return STEAM_COLOR;
                
            switch (onlineStatus.ToLower())
            {
                case "online":
                    return DiscordColor.Green;
                case "away":
                case "snooze":
                    return DiscordColor.Yellow;
                case "busy":
                    return DiscordColor.Red;
                case "offline":
                    return DiscordColor.Gray;
                default:
                    return STEAM_COLOR;
            }
        }
        
        private async Task SendErrorEmbed(InteractionContext ctx, string errorMessage)
        {
            var errorEmbed = new DiscordEmbedBuilder()
                .WithTitle("❌ Lỗi xảy ra")
                .WithDescription(errorMessage)
                .WithColor(DiscordColor.Red);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
        }

        private string FormatSystemRequirements(SystemRequirementsDetail requirements)
        {
            var result = new StringBuilder();
            
            if (!string.IsNullOrEmpty(requirements.OS))
                result.AppendLine($"• **Hệ điều hành:** {requirements.OS}");
                
            if (!string.IsNullOrEmpty(requirements.Processor))
                result.AppendLine($"• **CPU:** {requirements.Processor}");
                
            if (!string.IsNullOrEmpty(requirements.Memory))
                result.AppendLine($"• **RAM:** {requirements.Memory}");
                
            if (!string.IsNullOrEmpty(requirements.Graphics))
                result.AppendLine($"• **GPU:** {requirements.Graphics}");
                
            if (!string.IsNullOrEmpty(requirements.DirectX))
                result.AppendLine($"• **DirectX:** {requirements.DirectX}");
                
            if (!string.IsNullOrEmpty(requirements.Storage))
                result.AppendLine($"• **Dung lượng:** {requirements.Storage}");
                
            if (!string.IsNullOrEmpty(requirements.Network))
                result.AppendLine($"• **Mạng:** {requirements.Network}");
                
            if (!string.IsNullOrEmpty(requirements.SoundCard))
                result.AppendLine($"• **Âm thanh:** {requirements.SoundCard}");
                
            if (!string.IsNullOrEmpty(requirements.AdditionalNotes))
                result.AppendLine($"• **Ghi chú thêm:** {requirements.AdditionalNotes}");
                
            string formatted = result.ToString().Trim();
            
            if (formatted.Length > 1024)
                formatted = formatted.Substring(0, 1020) + "...";
                
            return formatted;
        }
    }
}
