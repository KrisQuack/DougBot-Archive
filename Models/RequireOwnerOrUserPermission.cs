namespace DougBot.Models;
using Discord;
using Discord.Interactions;

public class RequireOwnerOrUserPermission : RequireUserPermissionAttribute
{
    public GuildPermission? GuildPermission { get; }
    public RequireOwnerOrUserPermission(GuildPermission guildPermission) : base(guildPermission)
    {
        GuildPermission = guildPermission;
    }
    public ChannelPermission? ChannelPermission { get; }
    public RequireOwnerOrUserPermission(ChannelPermission channelPermission) : base(channelPermission)
    {
        ChannelPermission = channelPermission;
    }
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        var guildUser = context.User as IGuildUser;
        var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
        //Skip check for owner
        if (application.Owner.Id == context.User.Id)
            return await Task.FromResult(PreconditionResult.FromSuccess());
        //Otherwise continue check for permissions
        if (GuildPermission.HasValue)
        {
            if (guildUser == null)
                return await Task.FromResult(PreconditionResult.FromError(NotAGuildErrorMessage ?? "Command must be used in a guild channel."));
            if (!guildUser.GuildPermissions.Has(GuildPermission.Value))
                return await Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? $"User requires guild permission {GuildPermission.Value}."));
        }

        if (ChannelPermission.HasValue)
        {
            ChannelPermissions perms;
            if (context.Channel is IGuildChannel guildChannel)
                perms = guildUser.GetPermissions(guildChannel);
            else
                perms = ChannelPermissions.All(context.Channel);

            if (!perms.Has(ChannelPermission.Value))
                return await Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? $"User requires channel permission {ChannelPermission.Value}."));
        }

        return await Task.FromResult(PreconditionResult.FromSuccess());
    }
}