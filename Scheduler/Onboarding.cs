using Discord.WebSocket;

namespace DougBot.Scheduler;

public static class Onboarding
{
    public static async Task FreshmanCheck(DiscordSocketClient client, ulong guildId, ulong userId)
    {
        var guild = client.GetGuild(guildId);
        var user = guild.GetUser(userId);
        var role = guild.GetRole(935020318408462398);
        if (!user.Roles.Contains(role))
        {
            await user.KickAsync("Did not verify, Likely DMs disabled");
        }
    }
}