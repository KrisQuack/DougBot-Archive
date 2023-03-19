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
        [Summary("user", "If that bot should exclusively talk to one user")] IGuildUser? user = null,
        [Summary("generations", "How many generations to run the AI for"), MaxValue(10)] int generations = 3)
    {
        await RespondAsync("Processing may take a few seconds", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        var botUser = Context.Client.CurrentUser;
        //Add emotes list to prompt
        var rnd = new Random();
        var emotes = Context.Guild.Emotes.Where(x => !x.IsManaged && !x.Animated).OrderBy(_ => rnd.Next()).Take(100);
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
        messageString = messageString.Replace($"@{botUser.Username}#{botUser.Discriminator}", "Wah,").Replace($"<@!{botUser.Id}>", "Wah").Replace($"{botUser.Username}({botUser.Id})", $"Wah({botUser.Id}):");
        messageString += "Wah({botUser.Id}):";
        //Send to API
        var builder = new ComponentBuilder();
        var fields = new List<EmbedFieldBuilder>();
        for (var i = 0; i < generations; i++)
        {
            try
            {
                using var client = new HttpClient();
                var data = new
                {
                    prompt = prompt + messageString,
                    max_tokens = 500,
                    temperature = 0.9,
                    frequency_penalty = 0,
                    presence_penalty = 0,
                    top_p = 1,
                    stop = new[] { "\n", $"Wah({botUser.Id}):","Wah:" }
                };
                var content = new StringContent(JsonSerializer.Serialize(data));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.Add("api-key", dbGuild.OpenAiToken);

                var response = await client.PostAsync(dbGuild.OpenAiURL, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent = responseContent.Replace("Wah:", "");
                if (!responseContent.Contains("choices") && !responseContent.Contains("text")) continue;
                var json = JsonDocument.Parse(responseContent);
                var text = json.RootElement.GetProperty("choices")[0].GetProperty("text").GetString();
                //Respond
                if (string.IsNullOrWhiteSpace(text)) continue; 
                builder.WithButton($"{i}", $"aiChatApprove{i}");
                fields.Add(new EmbedFieldBuilder
                {
                    Name = $"{i}",
                    Value = text.Replace(".", ".\n"),
                    IsInline = false
                });
                //sleep as to not hammer the API
                await Task.Delay(1000);
            }
            catch (Exception e){Console.WriteLine(e);}
        }
        var embed = new EmbedBuilder
        {
            Title = "AI Chat",
            Description = "Please select a response to send to chat",
            Fields = fields,
            Color = Color.Blue
        };
        await ModifyOriginalResponseAsync(r => r.Embeds = new[] { embed.Build() });
        await ModifyOriginalResponseAsync(r => r.Components = builder.Build());
    }

    [ComponentInteraction("aiChatApprove*")]
    public async Task ApproveResponse(string index)
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        IUserMessage response = null;
        var messages = interaction.Message.Embeds.FirstOrDefault().Fields.FirstOrDefault(f => f.Name == index).Value.Split("\n");
        foreach(var message in messages)
        {
            response = await ReplyAsync(message, allowedMentions: AllowedMentions.None);
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