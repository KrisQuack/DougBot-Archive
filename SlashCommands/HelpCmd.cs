using Discord;
using Discord.Interactions;
using DougBot.Models;

namespace DougBot.SlashCommands;

public class HelpCmd : InteractionModuleBase
{
    [SlashCommand("help", "Help Command")]
    [EnabledInDm(false)]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task Help()
    {
        var embeds = new List<Embed>();
        //Commands
        var commandsEmbed = new EmbedBuilder()
            .WithTitle("Commands")
            .WithColor(Color.DarkBlue);
        foreach (var module in Program.Service.Modules)
        foreach (var command in module.SlashCommands)
        {
            var result = await command.CheckPreconditionsAsync(Context, Program.ServiceProvider);
            if (result.IsSuccess)
                commandsEmbed.Fields.Add(new EmbedFieldBuilder
                {
                    Name = $"/{module.SlashGroupName} {command.Name}",
                    Value =
                        $"{command.Description}\n{string.Join("\n", command.FlattenedParameters.Select(p => $"\t{p.Name}: {p.Description}"))}",
                    IsInline = false
                });
        }

        embeds.Add(commandsEmbed.Build());
        //Features
        var featuresEmbed = new EmbedBuilder()
            .WithTitle("Features")
            .WithColor(Color.DarkBlue)
            .WithFields(new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Logging",
                    Value =
                        $"The bot will log most events happening on the server and to its users in <#{ConfigurationService.Instance.LogChannel.Id}>",
                    IsInline = false
                },
                new()
                {
                    Name = "Bot DMs",
                    Value = $"The bot will relay and DMs sent to the mod team into <#{ConfigurationService.Instance.DmReceiptChannel.Id}>",
                    IsInline = false
                },
                new()
                {
                    Name = "Reporting",
                    Value =
                        $"Users can right click a message or another member of the server and report them, this will be posted to <#{ConfigurationService.Instance.ReportChannel.Id}>",
                    IsInline = false
                }
            });
        embeds.Add(featuresEmbed.Build());
        //Send
        await RespondAsync(embeds: embeds.ToArray(), ephemeral: true);
    }
}