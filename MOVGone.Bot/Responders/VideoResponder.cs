using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace MOVGone.Bot.Responders;

public class VideoResponder : IResponder<IMessageCreate>
{
    private readonly HttpClient _api;
    private readonly HttpClient _http;
    private readonly IDiscordRestGuildAPI _guilds;
    private readonly IDiscordRestChannelAPI _channels;
    
    public VideoResponder(IHttpClientFactory clientFactory, IDiscordRestGuildAPI guilds, IDiscordRestChannelAPI channels)
    {
        _guilds = guilds;
        _channels = channels;
        _api = clientFactory.CreateClient("ApiClient");
        _http = clientFactory.CreateClient();
    }
    
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        if (!gatewayEvent.GuildID.IsDefined(out var guildID))
            return Result.FromSuccess(); // We don't care about videos in DMs. Potential future feature, though?
        
        // TODO: Implement config
        
        if (!gatewayEvent.Attachments.Any(a => a.Url.EndsWith(".mov")))
            return Result.FromSuccess();
        
        var guild = await _guilds.GetGuildAsync(guildID, false, ct);
        
        if (!guild.IsSuccess)
            return Result.FromSuccess();

        var attachments = gatewayEvent.Attachments.Select(a => a.Url).ToArray();
        
        var max = GetMaxUpload(guild.Entity.GuildFeatures, guild.Entity.PremiumSubscriptionCount.IsDefined(out var subs) ? subs : 0);
        var shouldTranscode = await DetermineAttachmentSizeAsync(attachments, max, ct);

        if (!shouldTranscode) // Bail early; user has nitro and the upload is gonna be too big for us to fix.
            return Result.FromSuccess(); // React?

        var outbound = new List<FileData>();
        var counter = 0;
        
        await foreach (var result in GetTranscodedStreamsAsync(attachments, ct))
        {
            outbound.Add(new FileData(attachments[counter].Split('/')[^1], result, string.Empty));
            counter++;
        }

        var upload = await _channels.CreateMessageAsync
        (
            gatewayEvent.ChannelID,
            gatewayEvent.Content,
            messageReference: gatewayEvent.MessageReference,
            allowedMentions: new AllowedMentions(Array.Empty<MentionType>()),
            attachments: outbound.Select(OneOf.OneOf<FileData, IPartialAttachment>.FromT0).ToArray()
        );
        
        if (!upload.IsSuccess)
            return Result.FromSuccess();
        
        await _channels.DeleteMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, "Transcoded .MOV files", ct: ct);

        return Result.FromSuccess();
    }

    private int GetMaxUpload(IReadOnlyList<GuildFeature> features, int boosts)
    {
        var upload = 8 * 1024 * 1024; // 8MB
        
        upload = boosts >= 7  ? 50  * 1024 * 1024 :
                 boosts >= 14 ? 100 * 1024 * 1024 :
                 upload; // 50MB 
        
        if (features.Contains(GuildFeature.Partnered))
            upload = 100 * 1024 * 1024; // 100MB

        return upload;
    }
    
    private async Task<bool> DetermineAttachmentSizeAsync(string[] attachments, int upload, CancellationToken ct)
    {
        var req = await _api.PostAsJsonAsync("/api/validate", new { attachments, UploadLimit = upload }, ct);

        return req.StatusCode is HttpStatusCode.OK; // 413 means we won't be able to handle 
    }

    private async IAsyncEnumerable<Stream> GetTranscodedStreamsAsync(string[] links, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var link in links)
        {
            if (!link.EndsWith(".mov")) // return the original if it's not a mov
                yield return await _http.GetStreamAsync(link);
            
            var response = await _api.PostAsync($"/api/transcode", new StringContent(JsonSerializer.Serialize(link), Encoding.UTF8, "application/json"));
        
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to get transcoded stream");
        
            yield return await response.Content.ReadAsStreamAsync();
        }
    }
}