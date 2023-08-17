﻿using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Systems.EventBased;
using DougBot.Systems.TimeBased;
using DougBot.Systems.Twitch;

namespace DougBot;

public class Program
{
    private bool _firstStart = true;

    //Main Variables
    public static InteractionService Service { get; private set; }
    public static IServiceProvider ServiceProvider { get; }
    public static DiscordSocketClient Client { get; private set; }
    public static Random Random { get; private set; } = new();

    private static Task Main()
    {
        return new Program().MainAsync();
    }

    private async Task MainAsync()
    {
        //Start discord bot
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All ^ (GatewayIntents.GuildPresences | GatewayIntents.GuildScheduledEvents |
                                                   GatewayIntents.GuildInvites),
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100,
            UseInteractionSnowflakeDate = true,
            AlwaysDownloadUsers = true,
            AlwaysDownloadDefaultStickers = false
        };
        Client = new DiscordSocketClient(config);
        Client.Log += Log;
        Client.Ready += Ready;
        await Client.LoginAsync(TokenType.Bot, ConfigurationService.Instance.Token);
        await Client.StartAsync();
        //Block Task
        await Task.Delay(-1);
    }

    private async Task Ready()
    {
        if (_firstStart)
        {
            _firstStart = false;
            //Register Plugins
            _ = new Twitch().RunClient();
            _ = CheckYoutube.Monitor();
            _ = ReactionFilter.Monitor();
            _ = AuditLog.Monitor();
            _ = DMRelay.Monitor();
            _ = ContentModeration.Monitor();
            _ = ForumAutomod.Monitor();
            //Set status
            await Client.SetGameAsync("DMs for mod help", null, ActivityType.Listening);
            //Register Commands
            Service = new InteractionService(Client.Rest);
            Service.Log += Log;
            await Service.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);
            await Service.RegisterCommandsGloballyAsync();
            Client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(Client, interaction);
                await Service.ExecuteCommandAsync(ctx, ServiceProvider);
            };
            Service.SlashCommandExecuted += async (command, context, result) =>
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
                await AuditLog.LogEvent("***Command Ran***", context.Guild.Id.ToString(),
                    result.IsSuccess ? Color.Green : Color.Red, auditFields);
                if (!result.IsSuccess)
                {
                    if (result.ErrorReason != null &&
                        result.ErrorReason.Contains("was not present in the dictionary.") &&
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
        await Client.SetStatusAsync(UserStatus.DoNotDisturb);
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