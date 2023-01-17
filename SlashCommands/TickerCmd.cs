using Discord;
using Discord.Interactions;

namespace DougBot.SlashCommands;

public class TickerCmd : InteractionModuleBase
{
    [SlashCommand("ticker", "Create,Upsert,Delete a ticker")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task Ticker(
        [Summary(description: "Action to be performed")]
        [Choice("Add Ticker", "add")]
        [Choice("Update Ticker", "update")]
        [Choice("Remove Ticker", "remove")]
        string action,
        [Summary(description: "Channel name to be created or updated")]
        string name = "",
        [Summary(description: "Channel to Update/Remove")]
        IVoiceChannel channel = null)
    {
        switch (action)
        {
            case "add":
                var newChannel = await Context.Guild.CreateVoiceChannelAsync(name, x => { x.Bitrate = 8000; });
                //Deny access to the channel for the 0 role
                await newChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole,
                    new OverwritePermissions(viewChannel: PermValue.Allow, connect: PermValue.Deny));
                break;
            case "update":
                if (channel.Bitrate != 8000)
                {
                    await RespondAsync("Invalid channel", ephemeral: true);
                    return;
                }

                await channel.ModifyAsync(x => x.Name = name);
                break;
            case "remove":
                if (channel.Bitrate != 8000)
                {
                    await RespondAsync("Invalid channel", ephemeral: true);
                    return;
                }

                await channel.DeleteAsync();
                break;
        }

        await RespondAsync("Complete", ephemeral: true);
    }
}