using Discord;
using Discord.WebSocket;

namespace DougBot.Systems.EventBased;

public static class AuditLog
{
    public static async Task Monitor()
    {
        var client = Program.Client;
        client.MessageUpdated += MessageUpdatedHandler;
        client.MessageDeleted += MessageDeletedHandler;
        client.GuildMemberUpdated += GuildMemberUpdatedHandler;
        client.UserUpdated += UserUpdatedHandler;
        client.UserJoined += UserJoinedHandler;
        client.UserLeft += UserLeftHandler;
        client.UserBanned += UserBannedHandler;
        client.UserUnbanned += UserUnbannedHandler;


        Console.WriteLine("AuditLog Initialized");
    }

    private static Task UserLeftHandler(SocketGuild guild, SocketUser user)
    {
        _ = Task.Run(async () =>
        {
            //Get guild user
            var guildUser = guild.GetUser(user.Id);
            if (guildUser == null)
                return;
            //Set Fields with roles
            var fields = new List<EmbedFieldBuilder>
                { new() { Name = "Roles", Value = string.Join("\n", guildUser.Roles.Select(r => r.Mention)) } };
            //Set Author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{user.Username} ({user.Id})",
                IconUrl = user.GetAvatarUrl()
            };
            //Log event
            await LogEvent("User Left", guild.Id.ToString(), Color.Red, fields, author);
        });
        return Task.CompletedTask;
    }

    private static Task UserBannedHandler(SocketUser user, SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            //Set Author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{user.Username} ({user.Id})",
                IconUrl = user.GetAvatarUrl()
            };
            //Log event
            await LogEvent("User Banned", guild.Id.ToString(), Color.Red, null, author);
        });
        return Task.CompletedTask;
    }

    private static Task UserUnbannedHandler(SocketUser user, SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            //Set Author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{user.Username} ({user.Id})",
                IconUrl = user.GetAvatarUrl()
            };
            //Log event
            await LogEvent("User Unbanned", guild.Id.ToString(), Color.Green, null, author);
        });
        return Task.CompletedTask;
    }

    private static Task UserJoinedHandler(SocketGuildUser user)
    {
        _ = Task.Run(async () =>
        {
            //Set Author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{user.Username} ({user.Id})",
                IconUrl = user.GetAvatarUrl()
            };
            //Log event
            await LogEvent("User Joined", user.Guild.Id.ToString(), Color.Green, null, author);
        });
        return Task.CompletedTask;
    }

    private static Task UserUpdatedHandler(SocketUser before, SocketUser after)
    {
        _ = Task.Run(async () =>
        {
            var fields = new List<EmbedFieldBuilder>();
            //If username changed add field
            if (before.Username != after.Username)
                fields.Add(new EmbedFieldBuilder
                    { Name = "Username", Value = $"{before.Username} -> {after.Username}" });
            //If guild avatar changed add field
            var attachments = new List<string>();
            if (before.AvatarId != after.AvatarId)
            {
                attachments = new List<string> { before.GetAvatarUrl(), after.GetAvatarUrl() };
                //add field 
                fields.Add(new EmbedFieldBuilder { Name = "Guild avatar updated", Value = "See attachments below" });
            }

            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{after.Username} ({after.Id})",
                IconUrl = after.GetAvatarUrl()
            };
            //Log event for each mutual guild
            if (fields.Count > 0)
                foreach (var guild in after.MutualGuilds)
                    await LogEvent("User Updated", guild.Id.ToString(), Color.LightOrange, fields, author, attachments);
        });
        return Task.CompletedTask;
    }

    private static Task GuildMemberUpdatedHandler(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        _ = Task.Run(async () =>
        {
            var fields = new List<EmbedFieldBuilder>();
            var beforeObj = await before.GetOrDownloadAsync();
            if (beforeObj == null)
                return;
            //If nickname changed add field
            if (beforeObj.Nickname != after.Nickname)
                fields.Add(new EmbedFieldBuilder
                    { Name = "Nickname", Value = $"{beforeObj.Nickname} -> {after.Nickname}" });
            //If roles changed add field
            if (beforeObj.Roles.Count != after.Roles.Count)
            {
                var beforeRoles = beforeObj.Roles.Select(r => r.Mention);
                var afterRoles = after.Roles.Select(r => r.Mention);
                var addedRoles = afterRoles.Except(beforeRoles);
                var removedRoles = beforeRoles.Except(afterRoles);
                if (addedRoles.Any())
                    fields.Add(new EmbedFieldBuilder { Name = "Roles Added", Value = string.Join("\n", addedRoles) });
                if (removedRoles.Any())
                    fields.Add(new EmbedFieldBuilder
                        { Name = "Roles Removed", Value = string.Join("\n", removedRoles) });
            }

            //If guild avatar changed add field
            var attachments = new List<string>();
            if (beforeObj.GuildAvatarId != after.GuildAvatarId)
            {
                attachments = new List<string> { beforeObj.GetGuildAvatarUrl(), after.GetGuildAvatarUrl() };
                //add field 
                fields.Add(new EmbedFieldBuilder { Name = "Guild avatar updated", Value = "See attachments below" });
            }

            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{after.Username} ({after.Id})",
                IconUrl = after.GetAvatarUrl()
            };
            //Log event if fields are not empty
            if (fields.Count > 0)
                await LogEvent("Member Updated", after.Guild.Id.ToString(), Color.LightOrange, fields, author,
                    attachments);
        });
        return Task.CompletedTask;
    }

    private static Task MessageDeletedHandler(Cacheable<IMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel)
    {
        _ = Task.Run(async () =>
        {
            var channelObj = await channel.GetOrDownloadAsync() as SocketTextChannel;
            var messageObj = await message.GetOrDownloadAsync();
            if (messageObj == null || await BlacklistCheck(channelObj))
                return;
            //Download message attachments from url
            var attachments = messageObj.Attachments.Select(a => a.Url).ToList();

            //Set fields
            var fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Content",
                    Value = !string.IsNullOrEmpty(messageObj.Content)
                        ? messageObj.Content
                        : "(Sticker/Embed/Media)"
                }
            };
            //if message has attachments add field
            if (messageObj.Attachments.Count > 0)
                fields.Add(new EmbedFieldBuilder
                {
                    Name = "Attachments",
                    Value = string.Join("\n", messageObj.Attachments.Select(a => a.Filename))
                });
            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name =
                    $"{messageObj.Author.Username} ({messageObj.Author.Id})",
                IconUrl = messageObj.Author.GetAvatarUrl()
            };
            //Log event
            await LogEvent($"Message Deleted in {(channel.Value as SocketTextChannel).Mention}",
                (channel.Value as SocketTextChannel).Guild.Id.ToString(), Color.Red, fields, author, attachments);
        });
        return Task.CompletedTask;
    }

    private static Task MessageUpdatedHandler(Cacheable<IMessage, ulong> before, SocketMessage after,
        IChannel channel)
    {
        _ = Task.Run(async () =>
        {
            var channelObj = channel as SocketTextChannel;
            var beforeObj = await before.GetOrDownloadAsync();
            if (beforeObj.Content == after.Content || await BlacklistCheck(channelObj))
                return;
            //Set fields
            var fields = new List<EmbedFieldBuilder>
            {
                new() { Name = "Before", Value = beforeObj.Content },
                new() { Name = "After", Value = after.Content }
            };
            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{after.Author.Username} ({after.Author.Id})",
                IconUrl = after.Author.GetAvatarUrl()
            };
            //Log event
            await LogEvent($"[Message]({after.GetJumpUrl()}) Updated in {channelObj.Mention}",
                channelObj.Guild.Id.ToString(), Color.LightOrange, fields, author);
        });
        return Task.CompletedTask;
    }

    public static async Task ErrorHandler(Exception e, string module)
    {
    }

    public static async Task LogEvent(string content, string guildId, Color embedColor,
        List<EmbedFieldBuilder> fields = null, EmbedAuthorBuilder author = null, List<string> attachments = null)
    {
        var embed = new EmbedBuilder()
            .WithDescription(content)
            .WithColor(embedColor)
            .WithCurrentTimestamp();
        if (fields != null)
            foreach (var field in fields.Where(f => f.Name != "null"))
                embed.AddField(field);
        if (author != null)
            embed.WithAuthor(author);
        await ConfigurationService.Instance.LogChannel.SendMessageAsync(embed: embed.Build());
    }

    private static async Task<bool> BlacklistCheck(SocketTextChannel channel)
    {
        return ConfigurationService.Instance.LogBlacklistChannels.Contains(channel) ||
               ConfigurationService.Instance.LogBlacklistChannels.Contains(channel);
    }
}