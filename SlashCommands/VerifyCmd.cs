using Discord;
using Discord.Interactions;

namespace DougBot.SlashCommands;

[Group("verify", "Verify you are not a bot")]
[EnabledInDm(false)]
public class VerifyCmd : InteractionModuleBase
{
    [SlashCommand("setup", "setup the verification system")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Setup()
    {
        //Create an embed explaining the system
        var embed = new EmbedBuilder()
            .WithTitle("Verification System")
            .WithDescription(
                "To verify you are not a bot please click the **Verify** button below. You will be sent an image with a random number of bell peppers. Click the **Submit** button under the image and enter the number of bell peppers you saw in the image. If you are not a bot you will be verified.")
            .Build();
        //create component to verify
        var component = new ComponentBuilder()
            .WithButton("Verify", "verifyrequest")
            .Build();
        //Send the embed
        await ReplyAsync(embed: embed, components: component);
    }

    [ComponentInteraction("verifyrequest", true)]
    public async Task VerifyRequest()
    {
        //Get a random file from the Media folder
        var files = Directory.GetFiles("Media/Verify");
        var file = files[Program.Random.Next(0, files.Length)];
        //get file name without extension
        var fileName = Path.GetFileNameWithoutExtension(file);
        //Create component to submit response
        var component = new ComponentBuilder()
            .WithButton("Submit", $"verifyresponse:{fileName}")
            .Build();
        await RespondWithFileAsync(file, $"verify{Context.User.Id}.jpeg", components: component, ephemeral: true);
    }

    [ComponentInteraction("verifyresponse:*", true)]
    public async Task VerifyResponse(string fileName)
    {
        //Show the verification modal
        await RespondWithModalAsync<VerifyModal>($"verifymodal:{fileName}");
    }

    [ModalInteraction("verifymodal:*", true)]
    public async Task VerifyProcess(string fileName, VerifyModal modal)
    {
        if (modal.Peppers == fileName)
            await RespondAsync("You are not a bot!", ephemeral: true);
        else
            await RespondAsync("You are a bot!", ephemeral: true);
    }
}

public class VerifyModal : IModal
{
    [ModalTextInput("peppers", TextInputStyle.Short,
        "Please enter the amount of bell peppers you saw in the image")]
    public string Peppers { get; set; }

    public string Title => "Verification";
}