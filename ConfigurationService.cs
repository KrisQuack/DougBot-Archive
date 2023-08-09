using Discord.WebSocket;
using System.Collections;

namespace DougBot
{
    public class ConfigurationService
    {
        private static ConfigurationService _instance;
        public static ConfigurationService Instance => _instance ??= new ConfigurationService();

        private readonly IDictionary _envVariables;
        private readonly List<YoutubeConfig> _youtubeConfigs;


        private ConfigurationService()
        {
            _envVariables = Environment.GetEnvironmentVariables();
            _youtubeConfigs = LoadYoutubeConfigs();
        }

        public string Token => Get("token");
        public string AiToken => Get("ai_token");
        public string ContentModerationUrl => Get("content_moderation_url");
        public string ContentModerationToken => Get("content_moderation_token");
        public string AiUrl => Get("ai_url");
        private string GuildId => Get("guild_id");
        public SocketGuild Guild => Program.Client.GetGuild(ulong.Parse(GuildId));
        private string ReactionFilterEmotesList => Get("reaction_filter_emotes");
        public List<string> ReactionFilterEmotes => ReactionFilterEmotesList.Split(',').ToList();
        private string ReactionFilterChannelIds => Get("reaction_filter_channels");
        public List<SocketTextChannel> ReactionFilterChannels => ReactionFilterChannelIds.Split(',').Select(id => Guild.GetTextChannel(ulong.Parse(id))).ToList();
        public string TwitchChannelName => Get("twitch_channel_name");
        public string TwitchChannelId => Get("twitch_channel_id");
        public string TwitchBotName => Get("twitch_bot_name");
        public string TwitchClientId => Get("twitch_client_id");
        public string TwitchClientSecret => Get("twitch_client_secret");
        public string TwitchBotRefreshToken => Get("twitch_bot_refresh_token");
        public string TwitchChannelRefreshToken => Get("twitch_channel_refresh_token");
        private string DmReceiptChannelId => Get("dm_receipt_channel");
        public SocketTextChannel DmReceiptChannel => Guild.GetTextChannel(ulong.Parse(DmReceiptChannelId));
        private string ReportChannelId => Get("report_channel");
        public SocketTextChannel ReportChannel => Guild.GetTextChannel(ulong.Parse(ReportChannelId));
        public string OpenAiToken => Get("open_ai_token");
        public string OpenAiUrl => Get("open_ai_url");
        private string LogChannelId => Get("log_channel");
        public SocketTextChannel LogChannel => Guild.GetTextChannel(ulong.Parse(LogChannelId));
        private string LogBlacklistChannelIds => Get("log_blacklist_channels");
        public List<SocketTextChannel> LogBlacklistChannels => LogBlacklistChannelIds.Split(',').Select(id => Guild.GetTextChannel(ulong.Parse(id))).ToList();
        public List<YoutubeConfig> YoutubeConfigs => _youtubeConfigs;

        private List<YoutubeConfig> LoadYoutubeConfigs()
        {
            List<YoutubeConfig> configs = new List<YoutubeConfig>();
            int i = 1;
            while (true)
            {
                var id = Get($"youtube_id_{i}");
                if (string.IsNullOrEmpty(id))
                    break;

                configs.Add(new YoutubeConfig
                {
                    Id = id,
                    MentionRole = Get($"youtube_mention_role_{i}"),
                    PostChannel = Get($"youtube_post_channel_{i}"),
                    LastVideoId = null,
                    GuildId = Get($"youtube_guild_id_{i}")
                });

                i++;
            }
            return configs;
        }

        private string Get(string key)
        {
            return _envVariables[key]?.ToString();
        }
    }

    public class YoutubeConfig
    {
        public string Id { get; set; }
        public string MentionRole { get; set; }
        public string PostChannel { get; set; }
        public string? LastVideoId { get; set; }
        public string GuildId { get; set; }
    }
}
