using Discord.WebSocket;

namespace DougBot.Scheduler;

public static class Role
{
    public static async Task Remove(DiscordSocketClient client, ulong guidID, ulong userID, ulong roleID)
    {
        var guild = client.GetGuild(guidID);
        var user = guild.GetUser(userID);
        var role = guild.GetRole(roleID);
        await user?.RemoveRoleAsync(role);
    }

    public static async Task Add(DiscordSocketClient client, ulong guidID, ulong userID, ulong roleID)
    {
        var guild = client.GetGuild(guidID);
        var user = guild.GetUser(userID);
        var role = guild.GetRole(roleID);
        await user?.AddRoleAsync(role);
    }
}