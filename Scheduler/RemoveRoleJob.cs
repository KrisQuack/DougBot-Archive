using Discord.WebSocket;
using Quartz;

namespace DougBot.Scheduler;

public class RemoveRoleJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;
        var client = Program._Client;
        var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
        var userId = Convert.ToUInt64(dataMap.GetString("userId"));
        var roleId = Convert.ToUInt64(dataMap.GetString("roleId"));

        //check for nulls and return if any are null
        if (guildId == 0 || userId == 0 || roleId == 0)
            return;
        
        var guild = client.GetGuild(guildId);
        var user = guild.GetUser(userId);
        var role = guild.GetRole(roleId);
        await user?.RemoveRoleAsync(role);
    }
}