using System.Globalization;
using Discord;
using Discord.Interactions;

namespace DougBot.SlashCommands;

public class TimeStampCmd : InteractionModuleBase
{
    private static readonly Dictionary<string, string> s_timeZoneOffsets = new()
    {
        { "ACDT", "+10:30" },
        { "ACST", "+09:30" },
        { "ADT", "-03:00" },
        { "AEDT", "+11:00" },
        { "AEST", "+10:00" },
        { "AHDT", "-09:00" },
        { "AHST", "-10:00" },
        { "AST", "-04:00" },
        { "AT", "-02:00" },
        { "AWDT", "+09:00" },
        { "AWST", "+08:00" },
        { "BAT", "+03:00" },
        { "BDST", "+02:00" },
        { "BET", "-11:00" },
        { "BST", "-03:00" },
        { "BT", "+03:00" },
        { "BZT2", "-03:00" },
        { "CADT", "+10:30" },
        { "CAST", "+09:30" },
        { "CAT", "-10:00" },
        { "CCT", "+08:00" },
        { "CDT", "-05:00" },
        { "CED", "+02:00" },
        { "CET", "+01:00" },
        { "CEST", "+02:00" },
        { "CST", "-06:00" },
        { "EAST", "+10:00" },
        { "EDT", "-04:00" },
        { "EED", "+03:00" },
        { "EET", "+02:00" },
        { "EEST", "+03:00" },
        { "EST", "-05:00" },
        { "FST", "+02:00" },
        { "FWT", "+01:00" },
        { "GMT", "+00:00" },
        { "GST", "+10:00" },
        { "HDT", "-09:00" },
        { "HST", "-10:00" },
        { "IDLE", "+12:00" },
        { "IDLW", "-12:00" },
        { "IST", "+05:30" },
        { "IT", "+03:30" },
        { "JST", "+09:00" },
        { "JT", "+07:00" },
        { "MDT", "-06:00" },
        { "MED", "+02:00" },
        { "MET", "+01:00" },
        { "MEST", "+02:00" },
        { "MEWT", "+01:00" },
        { "MST", "-07:00" },
        { "MT", "+08:00" },
        { "NDT", "-02:30" },
        { "NFT", "-03:30" },
        { "NT", "-11:00" },
        { "NST", "+06:30" },
        { "NZ", "+11:00" },
        { "NZST", "+12:00" },
        { "NZDT", "+13:00" },
        { "NZT", "+12:00" },
        { "PDT", "-07:00" },
        { "PST", "-08:00" },
        { "ROK", "+09:00" },
        { "SAD", "+10:00" },
        { "SAST", "+09:00" },
        { "SAT", "+09:00" },
        { "SDT", "+10:00" },
        { "SST", "+02:00" },
        { "SWT", "+01:00" },
        { "USZ3", "+04:00" },
        { "USZ4", "+05:00" },
        { "USZ5", "+06:00" },
        { "USZ6", "+07:00" },
        { "UT", "-00:00" },
        { "UTC", "-00:00" },
        { "UZ10", "+11:00" },
        { "WAT", "-01:00" },
        { "WET", "-00:00" },
        { "WST", "+08:00" },
        { "YDT", "-08:00" },
        { "YST", "-09:00" },
        { "ZP4", "+04:00" },
        { "ZP5", "+05:00" },
        { "ZP6", "+06:00" }
    };

    [SlashCommand("timestamp", "Converts a timeestamp into discord tiemcodes")]
    [EnabledInDm(false)]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task TimeStamp(
        [Summary(description: "The time to convert in format 12:00 GMT or 01 Jan 2022 12:00 GMT")]
        string dateString)
    {
        var timeZonePos = dateString.LastIndexOf(' ') + 1;
        var tz = dateString.Substring(timeZonePos);
        dateString = dateString.Substring(0, dateString.Length - tz.Length);
        dateString += s_timeZoneOffsets[tz];
        //Try to parse the date and if it fails try to parse the time
        var parsedTime = DateTime.UtcNow;
        if (!DateTime.TryParseExact(dateString, "dd MMM yyyy HH:mm zzz", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsedTime))
            if (!DateTime.TryParseExact(dateString, "HH:mm zzz", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out parsedTime))
                throw new ArgumentException(
                    "Invalid time format. Please use the format `12:00 GMT ` or `01 Jan 2022 12:00 GMT`" + tz == "PT"
                        ? "\n\nYou input PT, please select PST or PDT respectively."
                        : "");
        var parsedUnixTime = ((DateTimeOffset)parsedTime).ToUnixTimeSeconds().ToString();
        var embed = new EmbedBuilder()
            .WithTitle("Time Stamp")
            .AddField("Relative Time", "`<t:" + parsedUnixTime + ":R>` : <t:" + parsedUnixTime + ":R>")
            .AddField("Absolute Time", "`<t:" + parsedUnixTime + ":F>` : <t:" + parsedUnixTime + ":F>")
            .AddField("Short Date", "`<t:" + parsedUnixTime + ":f>` : <t:" + parsedUnixTime + ":f>")
            .AddField("Long TIme", "`<t:" + parsedUnixTime + ":T>` : <t:" + parsedUnixTime + ":T>")
            .AddField("Short Time", "`<t:" + parsedUnixTime + ":t>` : <t:" + parsedUnixTime + ":t>")
            .WithColor(Color.Blue)
            .Build();
        await RespondAsync($"`<t:{parsedUnixTime}:t> <t:{parsedUnixTime}:R>`", new[] { embed });
    }
}