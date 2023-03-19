using System.Net.Http.Headers;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.SlashCommands;

public class AIChatCmd : InteractionModuleBase
{

    [SlashCommand("aichat", "Send an AI message into chat")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task AIChat([Summary("read", "How many messages to read"), MaxValue(200)] int read = 10,
        [Summary("user", "If that bot should exclusively talk to one user")] IGuildUser? user = null)
    {
        await RespondAsync("Command received", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        var botUser = Context.Client.CurrentUser;
        //Add emotes list to prompt
        var rnd = new Random();
        var emotes = Context.Guild.Emotes.Where(x => !x.IsManaged && !x.Animated).OrderBy(_ => rnd.Next()).Take(25);
        var prompt = string.Join("\n",dbGuild.OpenAiPrompt).Replace("!se!", string.Join(",", emotes.Select(e => $"<:{e.Name}:{e.Id}>")));
        //Get chat messages
        var messages = await Context.Channel.GetMessagesAsync(200, CacheMode.CacheOnly).FlattenAsync();
        if(user != null)
            messages = messages.Where(m => m.Author.Id == user.Id || m.Author.Id == botUser.Id);
        //Filter messages to ignore, select number to read, and order by date
        messages = messages.Where(m =>
            !string.IsNullOrWhiteSpace(m.Content) &&
            (m.Flags.Value & MessageFlags.Ephemeral) == 0 &&
            !dbGuild.OpenAiUserBlacklist.Contains(m.Author.Id.ToString())
        ).Take(read).OrderBy(m => m.CreatedAt);
        //Process all messages
        var messageString = "";
        foreach (var message in messages)
        {
            //If message is a reply check if it was directed to Wah, if not then discard
            if (message.Reference != null)
            {
                var replyMessage = messages.SingleOrDefault(m => m.Id == (ulong)message.Reference.MessageId);
                if (replyMessage != null && replyMessage.Author.Id == botUser.Id)
                    messageString += $"{message.Author.Username}({message.Author.Id}): Wah, {message.Content}\n";
                else if (replyMessage != null)
                    messageString += $"{message.Author.Username}({message.Author.Id}): <@{replyMessage.Author.Id}> {message.Content}\n";
            }
            else
            {
                messageString += $"{message.Author.Username}({message.Author.Id}): {message.Content}\n";
            }
        }
        messageString = messageString.Replace($"@{botUser.Username}#{botUser.Discriminator}", "Wah,").Replace($"<@!{botUser.Id}>", "Wah").Replace("WAHAHA(1037302561058848799)", "Wah");
        messageString += "Wah:";
        //Send to API
        using var client = new HttpClient();
        var data = new
        {
            prompt = prompt + messageString,
            max_tokens = 500,
            temperature = 0.9,
            frequency_penalty = 0,
            presence_penalty = 0,
            top_p = 1,
            stop = new[] { "\n", "Wah:" }
        };
        var content = new StringContent(JsonSerializer.Serialize(data));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.Add("api-key", dbGuild.OpenAiToken);

        var response = await client.PostAsync(dbGuild.OpenAiURL, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent = responseContent.Replace("Wah:", "");
        if (!responseContent.Contains("choices") && !responseContent.Contains("text"))
        {
            await ModifyOriginalResponseAsync(r => r.Content = responseContent);
            return;
        }
        var json = JsonDocument.Parse(responseContent);
        var text = json.RootElement.GetProperty("choices")[0].GetProperty("text").GetString();
        //Respond
        if (string.IsNullOrWhiteSpace(text))
        {
            await ModifyOriginalResponseAsync(r => r.Content = "No response from API");
            return;
        }
        var builder = new ComponentBuilder()
            .WithButton("Send to chat", "aiChatApprove");
        await ModifyOriginalResponseAsync(r => r.Content = text.Replace(". ", ".\n"));
        await ModifyOriginalResponseAsync(m => m.Components = builder.Build());
    }

    [ComponentInteraction("aiChatApprove")]
    public async Task ApproveResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        IUserMessage response = null;
        foreach(var line in interaction.Message.Content.Split("\n"))
        {
            response = await ReplyAsync(line, allowedMentions: AllowedMentions.None);
            await Task.Delay(3000);
        }
        var auditFields = new List<EmbedFieldBuilder>
        {
            new()
            {
                Name = "Approved By",
                Value = Context.User.Mention,
                IsInline = true
            },
            new()
            {
                Name = "Channel",
                Value = (Context.Channel as SocketTextChannel).Mention,
                IsInline = true
            },
            new()
            {
                Name = "Message",
                Value = $"[{interaction.Message.Content}]({response.GetJumpUrl()})",
                IsInline = false
            }
        };
        AuditLog.LogEvent("***AI Message Approved***", Context.Guild.Id.ToString(), Color.Green, auditFields);
    }
}