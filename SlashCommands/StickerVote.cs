using Discord;
using Discord.Interactions;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.SlashCommands;

public class SticketVoteCmd : InteractionModuleBase
{
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [SlashCommand("stickervote", "Vote on what stickers to keep")]
    public async Task StickerVote()
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an option")
            .WithMinValues(1)
            .WithMaxValues(19)
            .AddOption("A CREW POSTER", "867504132537581600")
            .AddOption("ALPHABET CREW", "867509834596352010")
            .AddOption("Beans", "867181384631713812")
            .AddOption("Bored", "928092900951224350")
            .AddOption("Bunny Suit", "946607507843985478")
            .AddOption("Calculus", "868723101305802812")
            .AddOption("ChugChug", "1017875309195960401")
            .AddOption("Confused", "1027138198427680769")
            .AddOption("Death Stare", "946608647746781204")
            .AddOption("Diablo the Cheater", "864129126348095499")
            .AddOption("DougButTheresMore", "1030445617048211466")
            .AddOption("DougGrump", "1039674720272011284")
            .AddOption("DougMaid: Cry", "879898133780398121")
            .AddOption("DougPoint", "1039675113827729493")
            .AddOption("duckDab", "1066975462221422673")
            .AddOption("EddieCat", "1062183353547227236")
            .AddOption("EddieFoot", "893379075777896469")
            .AddOption("EddiePray2?", "1031280714597027950")
            .AddOption("Evil", "1022367600321253426");
        var menuBuilder2 = new SelectMenuBuilder()
            .WithPlaceholder("Select an option")
            .WithMinValues(1)
            .WithMaxValues(19)
            .AddOption("Gasp", "1052769550170591312")
            .AddOption("GRANDMA CREW", "867508606742822982")
            .AddOption("Henryed", "936049861197979709")
            .AddOption("HYPE", "885304356474327070")
            .AddOption("HYPER", "885304164710744104")
            .AddOption("I don't trust you", "867181264708829184")
            .AddOption("KEKW", "867181095261831198")
            .AddOption("Not Bad", "943565714374131742")
            .AddOption("NotLikeThis", "904143109741490216")
            .AddOption("Offer", "950149831568027668")
            .AddOption("Ostrich Egg", "867429939972538419")
            .AddOption("Pain", "934994500949016606")
            .AddOption("Parkzer Stare", "936074623425065032")
            .AddOption("parkzerPeek", "1054210974699225188")
            .AddOption("PauseChamp", "873706841417347153")
            .AddOption("Pepper arrives", "887469396535156767")
            .AddOption("Plotting", "888835692371787896")
            .AddOption("Power Couple", "1054635800605765732")
            .AddOption("Pray", "867903578832240641");
        var menuBuilder3 = new SelectMenuBuilder()
            .WithPlaceholder("Select an option")
            .WithMinValues(1)
            .WithMaxValues(18)
            .AddOption("Realization", "871988640228708362")
            .AddOption("Regret", "896632780585332736")
            .AddOption("Rosa Relax", "879922145919107103")
            .AddOption("Rosa's Birthday", "880630171705704459")
            .AddOption("Salute", "867906146999599144")
            .AddOption("SALUTE ASU", "895480223955558421")
            .AddOption("SHIGGYMIGGY AND THE GUNICORN", "872691309985546250")
            .AddOption("ShrugShrug", "943566088900345869")
            .AddOption("Simon", "915030030759911435")
            .AddOption("Simon and Henry", "919267399847526430")
            .AddOption("Simoned", "890765757552660521")
            .AddOption("STACHE", "904096703794262158")
            .AddOption("Superiority", "986809532053327912")
            .AddOption("THINK DOUG", "881428536295047188")
            .AddOption("Thumbs up", "874827958697734164")
            .AddOption("Wink", "892467409921912853")
            .AddOption("WOAH", "976537705913671690")
            .AddOption("Z CREW POSTER", "867504216469405736");
        var builder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder.WithCustomId("keepSticker1"))
            .WithSelectMenu(menuBuilder2.WithCustomId("keepSticker2"))
            .WithSelectMenu(menuBuilder3.WithCustomId("keepSticker3"));
        await ReplyAsync("Which stickers would you like to keep?", components: builder.Build());
        builder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder.WithCustomId("removeSticker1"))
            .WithSelectMenu(menuBuilder2.WithCustomId("removeSticker2"))
            .WithSelectMenu(menuBuilder3.WithCustomId("removeSticker3"))
            .WithButton("Check my votes", "checkVotes");
        await ReplyAsync("Which stickers would you like to remove?", components: builder.Build());
    }

    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [SlashCommand("stickervoteresults", "View results of the sticker vote")]
    public async Task stickerVoteResults()
    {
        var settings = await Guild.GetGuild(Context.Guild.Id.ToString());
        var keep = new Dictionary<string, int>();
        var remove = new Dictionary<string, int>();
        foreach (var user in settings.Users)
        {
            var keepStickers = user.Storage.Where(s => s.Key.StartsWith("keepSticker"));
            var removeStickers = user.Storage.Where(s => s.Key.StartsWith("removeSticker"));
            foreach (var sticker in keepStickers)
            {
                foreach (var s in sticker.Value.Split(','))
                {
                    var stickerName = Context.Guild.Stickers.FirstOrDefault(st => st.Id.ToString() == s).Name;
                    if (keep.ContainsKey(stickerName)) keep[stickerName]++;
                    else keep.Add(stickerName, 1);
                }
            }
            foreach (var sticker in removeStickers)
            {
                foreach (var s in sticker.Value.Split(','))
                {
                    var stickerName = Context.Guild.Stickers.FirstOrDefault(st => st.Id.ToString() == s).Name;
                    if (remove.ContainsKey(stickerName)) remove[stickerName]++;
                    else remove.Add(stickerName, 1);
                }
            }
        }
        //Create embed
        var embedKeep = new EmbedBuilder()
            .WithTitle("Keep Results")
            .WithDescription(string.Join("\n", keep.OrderByDescending(s => s.Value).Select(s => $"{s.Key}: {s.Value}")))
            .WithColor(Color.Green).Build();
        var embedRemove = new EmbedBuilder()
            .WithTitle("Remove Results")
            .WithDescription(string.Join("\n", remove.OrderByDescending(s => s.Value).Select(s => $"{s.Key}: {s.Value}")))
            .WithColor(Color.Red).Build();
        RespondAsync(embeds: new[] { embedKeep, embedRemove });
    }

    [ComponentInteraction("*Sticker*")]
    public async Task keepSticker(string type,string number, string[] selected)
    {
        try
        {
            var settings = await Guild.GetGuild(Context.Guild.Id.ToString());
            var user = settings.Users.FirstOrDefault(u => u.Id == Context.User.Id.ToString());
            if (user == null)
            {
                settings.Users.Add(new UserSetting { Id = Context.User.Id.ToString() });
                user = settings.Users.FirstOrDefault(u => u.Id == Context.User.Id.ToString());
                user.Storage = new Dictionary<string, string>();
            }
            //Remove vote if already exists
            if (user.Storage.ContainsKey($"{type}Sticker{number}")) user.Storage.Remove($"{type}Sticker{number}");
            //Add vote
            user.Storage.Add($"{type}Sticker{number}", string.Join(",", selected));
            settings.Update();
            RespondAsync("Selection submitted. You can change this any time", ephemeral: true);
        }
        catch (Exception e)
        {
            RespondAsync("An error occurred. Please try again later. This has been reported to Quack", ephemeral: true);
            //create fields
            var fields = new List<EmbedFieldBuilder>
            {
                new () {Name = "Exception", Value = e.Message},
                new () {Name = "Stack Trace", Value = e.StackTrace}
            };
            //Create Author
            var author = new EmbedAuthorBuilder
            {
                Name = $"{Context.User.Username}#{Context.User.Discriminator} ({Context.User.Id})",
                IconUrl = Context.User.GetAvatarUrl()
            };
            AuditLog.LogEvent("Sticker Vote Error", Context.Guild.Id.ToString(), Color.Red, fields, author);
        }
    }
    
    [ComponentInteraction("checkVotes")]
    public async Task checkVotes()
    {
        var settings = await Guild.GetGuild(Context.Guild.Id.ToString());
        var user = settings.Users.FirstOrDefault(u => u.Id == Context.User.Id.ToString());
        if (user == null)
        {
            RespondAsync("You have not voted yet", ephemeral: true);
            return;
        }

        var keep = new List<string>();
        foreach (var keepDict in user.Storage.Where(keepDict => keepDict.Key.StartsWith("keepSticker")))
        {
            var range = keepDict.Value.Split(',');
            Context.Guild.Stickers.Where(s => range.Contains(s.Id.ToString())).Select(s => s.Name).ToList()
                .ForEach(keep.Add);
        }

        var remove = new List<string>();
        foreach (var removeDict in user.Storage.Where(removeDict => removeDict.Key.StartsWith("removeSticker")))
        {
            var range = removeDict.Value.Split(',');
            Context.Guild.Stickers.Where(s => range.Contains(s.Id.ToString())).Select(s => s.Name).ToList()
                .ForEach(remove.Add);
        }

        //Create fields
        var fields = new List<EmbedFieldBuilder>();
        if (keep.Count > 0)
            fields.Add(new EmbedFieldBuilder()
                .WithName("Keep")
                .WithValue(string.Join(", ", keep)));
        if (remove.Count > 0)
            fields.Add(new EmbedFieldBuilder()
                .WithName("Remove")
                .WithValue(string.Join(", ", remove)));
        //Create embed
        var embed = new EmbedBuilder()
            .WithAuthor(Context.User)
            .WithFields(fields)
            .WithColor(Color.Blue);
        RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}