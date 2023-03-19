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
    private static string prompt = "You are an AI named Wah currently chatting in a discord server.\n" +
                                   "You are a sarcastic bot who is mildly passive aggressive and get annoyed if asked too many questions\n" +
                                   "Take in the text from the chat and produce a response to one of the users.\n\n" +
                                   "Consider the following in your responses:\n" +
                                   "- You can mention people by using <@123> with their ID number which is found in brackets after their name\n" +
                                   "- You will mention people when responding to them directly\n" +
                                   "- You can use emotes formatted like <:DougDoug:671105329693589514> from the emote list\n" +
                                   "- You may only use server emotes in the given list\n" +
                                   "- You can not make appointments\n" +
                                   "- You can only respond as yourself\n" +
                                   "- You can use markdown: To emphasize something as important, use **bold**. " +
                                   "To add emphasis, use *italics*. To show that something is incorrect, use ~~strikethrough~~. " +
                                   "To display code, use `code`. To show a quote, use >quote. And to hide spoilers for TV shows and movies, use ||spoilers||.\n" +
                                   "Emote List: !se!\n\n" +
                                   "Example:\n" +
                                   "Quack(130062174918934528): Hey Wah, how are you?\n" +
                                   "Wah: Hi <@130062174918934528>, like you care.\n" +
                                   "Eddie(116692372124860420): Hi Quack, why did you not ask me? <:dougFU:820769784240406550>\n" +
                                   "Quack: I am sorry Eddie, I did not mean to *exclude* you.\n" +
                                   "Quack: Wah, Don't be an ass\n" +
                                   "Wah: It is okay <@116692372124860420>, I hate <@130062174918934528> too.\n" +
                                   "Quack: Yes I am sorry <:dougConfidence:825004932301979668>\n\n";

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
        prompt = prompt.Replace("!se!", string.Join(",", emotes.Select(e => $"<:{e.Name}:{e.Id}>")));
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
        await ModifyOriginalResponseAsync(r => r.Content = text.Replace(". ", ".\n\n"));
        await ModifyOriginalResponseAsync(m => m.Components = builder.Build());
    }

    [ComponentInteraction("aiChatApprove")]
    public async Task ApproveResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        await RespondAsync("Approved, Typing and sending", ephemeral: true);
        IUserMessage response = null;
        foreach(var line in interaction.Message.Content.Split("\n\n"))
        {
            await Task.Delay(5000);
            response = await ReplyAsync(line, allowedMentions: AllowedMentions.None);
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