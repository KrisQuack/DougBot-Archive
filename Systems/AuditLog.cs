using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Scheduler;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace DougBot.Systems;

public static class AuditLog
{
    public static async Task Monitor()
    {
        var client = Program._Client;
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
            if(guildUser == null)
                return;
            //Set Fields with roles
            var fields = new List<EmbedFieldBuilder>
                { new() { Name = "Roles", Value = string.Join("\n", guildUser.Roles.Select(r => r.Mention)) } };
            //Set Author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{user.Username}#{user.Discriminator} ({user.Id})",
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
                Name = $"{user.Username}#{user.Discriminator} ({user.Id})",
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
                Name = $"{user.Username}#{user.Discriminator} ({user.Id})",
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
                Name = $"{user.Username}#{user.Discriminator} ({user.Id})",
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

            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{after.Username}#{after.Discriminator} ({after.Id})",
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
            //If nickname changed add field
            if (before.Value.Nickname != after.Nickname)
                fields.Add(new EmbedFieldBuilder
                    { Name = "Nickname", Value = $"{before.Value.Nickname} -> {after.Nickname}" });
            //If roles changed add field
            if (before.Value.Roles.Count != after.Roles.Count)
            {
                var beforeRoles = before.Value.Roles.Select(r => r.Mention);
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
            if (before.Value.GuildAvatarId != after.GuildAvatarId)
            {
                using var httpClient = new HttpClient();
                //get root path
                var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //old avatar
                var attachmentBytes = await httpClient.GetByteArrayAsync(before.Value.GetGuildAvatarUrl());
                var path = Path.Combine(rootPath, $"{before.Value.GuildAvatarId}_before.png");
                await File.WriteAllBytesAsync(path, attachmentBytes);
                attachments.Add(path);
                //new avatar
                attachmentBytes = await httpClient.GetByteArrayAsync(after.GetGuildAvatarUrl());
                path = Path.Combine(rootPath, $"{after.GuildAvatarId}_after.png");
                await File.WriteAllBytesAsync(path, attachmentBytes);
                attachments.Add(path);
                //add field 
                fields.Add(new EmbedFieldBuilder { Name = "Guild avatar updated", Value = "See attachments below" });
            }

            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{after.Username}#{after.Discriminator} ({after.Id})",
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
            if (!message.HasValue && await BlacklistCheck(channelObj))
                return;
            //Download message attachments from url via httpclient
            var attachments = new List<string>();
            using var httpClient = new HttpClient();
            //get root path
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (IAttachment? attachment in message.Value.Attachments)
            {
                var attachmentBytes = await httpClient.GetByteArrayAsync(attachment.Url);
                var path = Path.Combine(rootPath, attachment.Filename);
                await File.WriteAllBytesAsync(path, attachmentBytes);
                attachments.Add(path);
            }

            //Set fields
            var fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Content",
                    Value = !string.IsNullOrEmpty(message.Value.Content)
                        ? message.Value.Content
                        : "(Sticker/Embed/Media)"
                }
            };
            //if message has attachments add field
            if (message.Value.Attachments.Count > 0)
                fields.Add(new EmbedFieldBuilder
                {
                    Name = "Attachments", Value = string.Join("\n", message.Value.Attachments.Select(a => a.Filename))
                });
            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name =
                    $"{message.Value.Author.Username}#{message.Value.Author.Discriminator} ({message.Value.Author.Id})",
                IconUrl = message.Value.Author.GetAvatarUrl()
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
            if (!before.HasValue || before.Value.Content == after.Content || await BlacklistCheck(channelObj))
                return;
            //Set fields
            var fields = new List<EmbedFieldBuilder>
            {
                new() { Name = "Before", Value = before.Value.Content },
                new() { Name = "After", Value = after.Content }
            };
            //Set author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{after.Author.Username}#{after.Author.Discriminator} ({after.Author.Id})",
                IconUrl = after.Author.GetAvatarUrl()
            };
            //Log event
            await LogEvent($"[Message]({after.GetJumpUrl()}) Updated in {channelObj.Mention}",
                channelObj.Guild.Id.ToString(), Color.LightOrange, fields, author);
        });
        return Task.CompletedTask;
    }

    public static Task LogEvent(string Content, string GuildId, Color EmbedColor,
        List<EmbedFieldBuilder> Fields = null, EmbedAuthorBuilder Author = null, List<string> attachments = null)
    {
        _ = Task.Run(async () =>
        {
            var dbGuild = await Guild.GetGuild(GuildId);
            var embed = new EmbedBuilder()
                .WithDescription(Content)
                .WithColor(EmbedColor)
                .WithCurrentTimestamp();
            if (Fields != null)
                foreach (var field in Fields.Where(f => f.Name != "null"))
                    embed.AddField(field);
            if (Author != null)
                embed.WithAuthor(Author);

            var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
            var sendMessageJob = JobBuilder.Create<SendMessageJob>()
                .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", GuildId)
                .UsingJobData("guildId", GuildId)
                .UsingJobData("channelId", dbGuild.LogChannel)
                .UsingJobData("message", "")
                .UsingJobData("embedBuilders", embedJson)
                .UsingJobData("ping", "true")
                .UsingJobData("attachments", null)
                .Build();
            var sendMessageTrigger = TriggerBuilder.Create()
                .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", GuildId)
                .StartNow()
                .Build();
            await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
        });
        return Task.CompletedTask;
    }

    private static async Task<bool> BlacklistCheck(SocketTextChannel channel)
    {
        var dbGuild = await Guild.GetGuild(channel.Guild.Id.ToString());
        return dbGuild.LogBlacklistChannels.Contains(channel.Id.ToString()) ||
               dbGuild.LogBlacklistChannels.Contains(channel.CategoryId.ToString());
    }
}