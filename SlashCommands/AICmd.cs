using Azure;
using Azure.AI.OpenAI;
using Discord;
using Discord.Interactions;
using DougBot.Models;

namespace DougBot.SlashCommands;

[Group("ai", "AI based commands")]
public class AIChatCmd : InteractionModuleBase
{
    [SlashCommand("analyse", "Analyses the current chat")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task Analyze([Summary("read", "How many messages to read (50)"), MaxValue(200)] int read = 50)
    {
        //Initial response
        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("Chat Analysis")
            .WithDescription("Processing")
            .WithFooter("Powered by OpenAI GPT-4");
        await RespondAsync(embeds: new []{ embed.Build() }, ephemeral: true);
        //Get values
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        var channel = Context.Channel as ITextChannel;
        var messages = await channel.GetMessagesAsync(200).FlattenAsync();
        //Filter messages to ignore, select number to read, and order by date
        var messageString = messages.Where(m =>
                !string.IsNullOrWhiteSpace(m.Content) &&
                !m.Author.IsBot
            ).Take(read).OrderBy(m => m.CreatedAt)
            .Aggregate("", (current, message) => current + $"{message.Author.Username}: {message.CleanContent}\n");
        //Send to API
        var client = new OpenAIClient(new Uri(dbGuild.OpenAiURL), new AzureKeyCredential(dbGuild.OpenAiToken));
        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System,
                        @"You are an assistant who's job is to analyze a given conversation and provide a summary of what it is about and if any rules have been broken.
                                You will be given a list of rules and a conversation, please provide the topic and who (if anyone) has broken a rule.

                                Consider the following rules for the conversation:
                                0) Follow Discord's Terms of Service, In addition to the following rules, you must comply with Discord's Community Guidelines. This includes no doxxing, no exchange of pirated material, and you must be 13 or over.
                                1) Follow Moderation and Common Sense, Mods have full discretion over enforcing rules. If you are asked by a moderator to stop doing something, stop. Disputes and appeals should be made in a ticket.
                                2) No Offensive Speech, We do not tolerate sexism, racism, homophobia, bigotry, hate speech, or any other forms of harassment.
                                3) Be Kind, Keep your conversations friendly and respectful. Messages deemed discourteous/disrespectful will be considered spam and removed.
                                4) No Spam or Shitposting, Follow channel rules - please read channel descriptions to know what goes in which channel. Out of context memes are not allowed outside of class-clowns-club. Exploiting bugs that crash Discord or posting media with flashing images/loud audio is strictly forbidden.
                                5) English-Only, Please only have conversations in English. Proper nouns (such as food names) and well-known phrases in other languages are allowed but you must still talk in English.
                                6) No Alt Accounts or Impersonation, Such activities cause confusion and obstruct moderation, even if they are done as a joke. Evading moderation through alt accounts will result in a permaban.
                                7) No Political Discussion, This is a server dedicated to a video-game man and people would like it to be light-hearted. Politics is often depressing and can lead to full-on debates, which we would like to avoid.
                                8) No Sexual Topics, This server is not 18+. We strictly forbid pornographic content, and its distribution/share. In addition, do not talk about sexual activities and references excessively or frequently to the point of making people uncomfortable. Occasional mature jokes/humor is allowed.
                                9) No Extremely Distressing topics, We strictly forbid violent imagery, and any other related content. In addition, distressing conversation about mental health/trauma/suicide should be avoided. If you feel the need to vent or ask for help, go over to supportive-af. However, we do not offer medical support, and we advise to seek professional help."
                        ),
                    new ChatMessage(ChatRole.User, 
                        @"Quack: Hey guys, I was thinking about the election and wanted to know what you thought about it? Who are you voting for?
                                Eira: We can't talk about politics here
                                Quack: Fuck the mods"
                        ),
                    new ChatMessage(ChatRole.Assistant, 
                        "The topic was about Elections, Quack broke rule 7 No Political Discussion by asking about the election and rule 3 for disrespecting moderation"
                    ),
                    new ChatMessage(ChatRole.User, 
                        @"Quack: What is your guys favourite Mug?
                                Eira: Mine is a big one shaped like a pumpkin, I use it for tea
                                Quack: TEA WHAT, that's gross I have a spreadsheet one I use for coffee
                                Quack: I also brew my coffee in a french press
                                Eira: That's cool :)"
                        ),
                    new ChatMessage(ChatRole.Assistant, 
                        "The topic was about Mugs and beverages contained in them, No rules were broken"
                        ),
                    new ChatMessage(ChatRole.User,
                        @"Quack: The weather outside is so bad
                                Eira: It is so sunny here, where are you?
                                Quack: I am in England where it always rains
                                Eddie: 🤮 England
                                Quack: What does that mean?
                                Eddie: England sucks they all have wonky teeth
                                Quack: That is not true
                                Seal: Yeah the UK is cringe they have awful food and weather and talk like idiots
                                Seal: Just like the french qui sentent le fromage"
                        ),
                    new ChatMessage(ChatRole.Assistant, 
                        "The topic was weather, Eddie broke rule 2 for offensive stereotypes against the British, Seal broke rule 2 for offensive speech against the French, Seal broke rule 5 for speaking in french"
                        ),
                    new ChatMessage(ChatRole.User, messageString)
                },
                MaxTokens = 500,
                Temperature = 0.5f,
                PresencePenalty = 0.5f,
                FrequencyPenalty = 0.5f
            };
            var response = await client.GetChatCompletionsAsync(
                deploymentOrModelName: "WahSpeech",
                chatCompletionsOptions);
            var completion = response.Value.Choices[0].Message.Content;
            embed.WithDescription(completion);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
        catch (Exception e)
        {
            var response = "Failed to analyse chat: " + e.Message;
            if (e.Message.Contains("content management policy."))
                response = "Failed to analyse chat: Content is not allowed by Azure's content management policy.";
            embed.WithDescription(response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
    }
}