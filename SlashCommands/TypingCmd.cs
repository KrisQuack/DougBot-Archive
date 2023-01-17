using Discord;
using Discord.Interactions;

namespace DougBot.SlashCommands;

public class TypingCmd : InteractionModuleBase
{
    [SlashCommand("typing", "Type in chat for a given amount of seconds")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task Typing([Summary(description: "Channel to type in")] ITextChannel channel,
        [Summary(description: "Amount of seconds to type")] [MaxValue(300)]
        int seconds)
    {
        await RespondAsync($"Beginning typing for {seconds} seconds in {channel.Mention} ", ephemeral: true);
        var type = channel.EnterTypingState();
        await Task.Delay(seconds * 1000);
        type.Dispose();
    }
}