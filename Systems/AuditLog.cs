using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class AuditLog
{
    public static async Task Monitor(DiscordSocketClient client)
    {
        client.MessageUpdated += async (before,after, channel) => Task.Run(() => MessageUpdatedHandler(before, after, channel));
        client.MessageDeleted += async (message, channel) => Task.Run(() => MessageDeletedHandler(message, channel));
        client.GuildMemberUpdated += async (before, after) => Task.Run(() => GuildMemberUpdatedHandler(before, after));
        client.UserUpdated += async (before, after) => Task.Run(() => UserUpdatedHandler(before, after));
        client.UserJoined += async (user) => Task.Run(() => UserJoinedHandler(user));
        client.UserLeft += async (guild, user) => Task.Run(() => UserLeftHandler(guild, user));
        client.UserBanned += async (user, guild) => Task.Run(() => UserBannedHandler(user, guild));
        client.UserUnbanned += async (user, guild) => Task.Run(() => UserUnbannedHandler(user, guild));
        
        
        Console.WriteLine("AuditLog Initialized");
    }

    private static async Task UserLeftHandler(SocketGuild guild, SocketUser user)
    {
        //Set Author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{user.Username}({user.Id}) left",
            IconUrl = user.GetAvatarUrl()
        };
        //Log event
        await LogEvent($"User Joined", guild.Id.ToString(), Color.Red, null, author);
    }
    
    private static async Task UserBannedHandler(SocketUser user, SocketGuild guild)
    {
        //Set Author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{user.Username}({user.Id}) banned",
            IconUrl = user.GetAvatarUrl()
        };
        //Log event
        await LogEvent($"User Banned", guild.Id.ToString(), Color.Red, null, author);
    }
    
    private static async Task UserUnbannedHandler(SocketUser user, SocketGuild guild)
    {
        //Set Author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{user.Username}({user.Id}) unbanned",
            IconUrl = user.GetAvatarUrl()
        };
        //Log event
        await LogEvent($"User Unbanned", guild.Id.ToString(), Color.Green, null, author);
    }

    private static async Task UserJoinedHandler(SocketGuildUser user)
    {
        //Set Author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{user.Username}({user.Id}) joined",
            IconUrl = user.GetAvatarUrl()
        };
        //Log event
        await LogEvent($"User Joined", user.Guild.Id.ToString(), Color.Green, null, author);
    }

    private static async Task UserUpdatedHandler(SocketUser before, SocketUser after)
    {
        var fields = new List<EmbedFieldBuilder>();
        //If username changed add field
        if (before.Username != after.Username)
            fields.Add(new () { Name = "Username", Value = $"{before.Username} -> {after.Username}" });
        //If status changed add field
        if (before.Status != after.Status)
            fields.Add(new () { Name = "Status", Value = $"{before.Status} -> {after.Status}" });
        //If avatar changed add field
        if (before.GetAvatarUrl() != after.GetAvatarUrl())
            fields.Add(new () { Name = "Avatar", Value = $"{before.GetAvatarUrl()} -> {after.GetAvatarUrl()}" });
        //If mutual guilds changed add field
        if (before.MutualGuilds.Count != after.MutualGuilds.Count)
        {
            var beforeGuilds = before.MutualGuilds.Select(g => g.Name);
            var afterGuilds = after.MutualGuilds.Select(g => g.Name);
            var addedGuilds = afterGuilds.Except(beforeGuilds);
            var removedGuilds = beforeGuilds.Except(afterGuilds);
            var addedGuildsString = string.Join(", ", addedGuilds);
            var removedGuildsString = string.Join(", ", removedGuilds);
            fields.Add(new () { Name = "Mutual Guilds", Value = $"Added: {addedGuildsString} Removed: {removedGuildsString}" });
        }
        //Set author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{after.Username}({after.Id}) was updated",
            IconUrl = after.GetAvatarUrl()
        };
        //Log event for each mutual guild
        foreach (var guild in after.MutualGuilds)
            await LogEvent($"User Updated", guild.Id.ToString(), Color.LightOrange, fields, author);
    }

    private static async Task GuildMemberUpdatedHandler(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        var fields = new List<EmbedFieldBuilder>();
        //If nickname changed add field
        if (before.Value.Nickname != after.Nickname)
            fields.Add(new () { Name = "Nickname", Value = $"{before.Value.Nickname} -> {after.Nickname}" });
        //If roles changed add field
        if (before.Value.Roles.Count != after.Roles.Count)
        {
            var beforeRoles = before.Value.Roles.Select(r => r.Name);
            var afterRoles = after.Roles.Select(r => r.Name);
            var addedRoles = afterRoles.Except(beforeRoles);
            var removedRoles = beforeRoles.Except(afterRoles);
            fields.Add(new () { Name = "Roles Added", Value = addedRoles.Count() > 0 ? string.Join("\n", addedRoles) : "None" });
            fields.Add(new () { Name = "Roles Removed", Value = removedRoles.Count() > 0 ? string.Join("\n", removedRoles) : "None" });
        }
        //Set author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{after.Username}({after.Id}) was updated",
            IconUrl = after.GetAvatarUrl()
        };
        //Log event
        await LogEvent($"Member Updated", after.Guild.Id.ToString(), Color.LightOrange, fields, author);
    }

    private static async Task MessageDeletedHandler(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        //Download message attachments
        var attachments = message.Value.Attachments;
        //Set fields
        var fields = new List<EmbedFieldBuilder>
        {
            new () { Name = "Content", Value = message.Value.Content },
            new () { Name = "Attachments", Value = attachments.Count > 0 ? string.Join("\n", attachments.Select(a => a.Url)) : "None" }
        };
        //Set author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{message.Value.Author.Username}({message.Value.Author.Id}) deleted a message in {channel.Value.Name}",
            IconUrl = message.Value.Author.GetAvatarUrl()
        };
        //Log event
        await LogEvent($"Message Deleted", (channel.Value as SocketTextChannel).Guild.Id.ToString(), Color.Red, fields, author);
    }

    private static async Task MessageUpdatedHandler(Cacheable<IMessage, ulong> before, SocketMessage after, IChannel channel)
    {
        //Set fields
        var fields = new List<EmbedFieldBuilder>
        {
            new () { Name = "Before", Value = before.Value.Content },
            new () { Name = "After", Value = after.Content }
        };
        //Set author
        var author = new EmbedAuthorBuilder
        {
            Name = $"{after.Author.Username}({after.Author.Id}) updated a message in {channel.Name}",
            IconUrl = after.Author.GetAvatarUrl(),
            Url = after.GetJumpUrl()
        };
        //Log event
        await LogEvent($"Message Updated", (channel as SocketTextChannel).Guild.Id.ToString(), Color.LightOrange, fields, author);
    }

    public static async Task LogEvent(string Content, string GuildId, Color EmbedColor,
        List<EmbedFieldBuilder> Fields = null, EmbedAuthorBuilder Author = null)
    {
        var dbGuild = await Guild.GetGuild(GuildId);
        var embed = new EmbedBuilder()
            .WithDescription(Content)
            .WithColor(EmbedColor)
            .WithCurrentTimestamp();
        if (Fields != null)
            foreach (var field in Fields.Where(f => f.Name != "null"))
                embed.AddField(field);
        if(Author != null)
            embed.WithAuthor(Author);

        var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        var dict = new Dictionary<string, string>
        {
            { "guildId", GuildId },
            { "channelId", dbGuild.LogChannel },
            { "message", "" },
            { "embedBuilders", embedJson },
            { "ping", "true" }
        };
        await new Queue("SendMessage", 2, dict, null).Insert();
    }
}