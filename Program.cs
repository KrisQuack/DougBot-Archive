using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Scheduler;
using DougBot.Systems;

namespace DougBot;

public class Program
{
    //Main Variables
    private static InteractionService _Service;
    private static IServiceProvider _ServiceProvider;
    private static DiscordSocketClient _Client;

    private bool _FirstStart = true;

    private static Task Main()
    {
        return new Program().MainAsync();
    }

    private async Task MainAsync()
    {
        //Start discord bot
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            AlwaysResolveStickers = true,
            LogLevel = LogSeverity.Verbose,
            MessageCacheSize = 1000
        };
        _Client = new DiscordSocketClient(config);
        _Client.Log += Log;
        _Client.Ready += Ready;
        await _Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));
        await _Client.StartAsync();
        //Block Task
        await Task.Delay(-1);
    }

    private async Task Ready()
    {
        if (_FirstStart)
        {
            _FirstStart = false;
            //Register Plugins
            AuditLog.Monitor(_Client);
            Scheduler.Scheduler.Schedule(_Client);
            Events.Monitor(_Client);
            CleanForums.Clean(_Client);
            Youtube.CheckYoutube();
            Twitch.Twitch.RunClient();
            ReactionFilter.Monitor(_Client);
            //Set status
            await _Client.SetGameAsync("DMs for mod help", null, ActivityType.Listening);
            //Register Commands
            _Service = new InteractionService(_Client.Rest);
            _Service.Log += Log;
            await _Service.AddModulesAsync(Assembly.GetEntryAssembly(), _ServiceProvider);
            await _Service.RegisterCommandsGloballyAsync();
            _Client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_Client, interaction);
                await _Service.ExecuteCommandAsync(ctx, _ServiceProvider);
            };
            _Service.SlashCommandExecuted += async (command, context, result) =>
            {
                var data = context.Interaction.Data as SocketSlashCommandData;
                var auditFields = new List<EmbedFieldBuilder>
                {
                    new()
                    {
                        Name = "Command",
                        Value = command.Name,
                        IsInline = true
                    },
                    new()
                    {
                        Name = "User",
                        Value = context.User.Mention,
                        IsInline = true
                    },
                    new()
                    {
                        Name = "Channel",
                        Value = (context.Channel as SocketTextChannel).Mention,
                        IsInline = true
                    },
                    data.Options.Count > 0
                        ? new EmbedFieldBuilder
                        {
                            Name = "Parameters",
                            Value = string.Join("\n", data.Options.Select(x => $"{x.Name}: {x.Value}")),
                            IsInline = true
                        }
                        : new EmbedFieldBuilder
                        {
                            Name = "null",
                            Value = "null",
                            IsInline = true
                        },
                    result.ErrorReason != null
                        ? new EmbedFieldBuilder
                        {
                            Name = "Error",
                            Value = result.ErrorReason,
                            IsInline = true
                        }
                        : new EmbedFieldBuilder
                        {
                            Name = "null",
                            Value = "null",
                            IsInline = true
                        }
                };
                AuditLog.LogEvent("***Command Ran***", context.Guild.Id.ToString(),
                    result.IsSuccess ? Color.Green : Color.Red, auditFields);
                if (!result.IsSuccess)
                {
                    if (result.ErrorReason.Contains("was not present in the dictionary.") &&
                        command.Name == "timestamp")
                    {
                        await context.Interaction.RespondAsync(
                            "Invalid time format. Please use the format `12:00 GMT ` or `01 Jan 2022 12:00 GMT`\nIf this continues try https://discordtimestamp.com/",
                            ephemeral: true);
                    }
                    else
                    {
                        if (!context.Interaction.HasResponded)
                            await context.Interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
                        else
                            await context.Interaction.ModifyOriginalResponseAsync(m =>
                                m.Content = $"Error: {result.ErrorReason}");
                    }
                }
            };
        }

        //Status
        await _Client.SetStatusAsync(UserStatus.DoNotDisturb);
    }

    private static Task Log(LogMessage msg)
    {
        if (msg.Exception is CommandException cmdException)
        {
            AuditLog.LogEvent(
                $"{cmdException.Command.Name} failed to execute in {cmdException.Context.Channel} by user {cmdException.Context.User.Username}.",
                cmdException.Context.Guild.Id.ToString(),
                Color.Red);
            AuditLog.LogEvent(cmdException.ToString(), cmdException.Context.Guild.Id.ToString(), Color.Red);
        }
        else
        {
            Console.WriteLine($"[General/{msg.Severity}] {msg}");
        }

        return Task.CompletedTask;
    }
}