using System;
using System.Collections.Generic;

namespace DougBot.Models;

public partial class Youtube
{
    public string Id { get; set; } = null!;

    public string? MentionRole { get; set; }

    public string? PostChannel { get; set; }

    public string? LastVideoId { get; set; }

    public string GuildId { get; set; } = null!;

    public virtual Guild Guild { get; set; } = null!;
}
