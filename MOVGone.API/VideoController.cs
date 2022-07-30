using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace NoMOV.API;

[Route("api")]
[ApiController]
public class VideoController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly ProcessStartInfo _ffmpeg;
    
    public VideoController(IHttpClientFactory http)
    {
        _http = http;
        
        _ffmpeg = new("./ffmpeg", "-hide_banner -level quiet -i - -c:v libx264 -crf 27 -preset veryslow -c:a aac -b:a 30k -f mp4 -")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }



    public record Videos(IEnumerable<string> Attachments, int UploadLimit);
    
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] Videos data)
    {
        var client = _http.CreateClient();

        int totalUpload = 0;

        foreach (var url in data.Attachments)
        {
            var req = new HttpRequestMessage(HttpMethod.Head, url);
            var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            
            if (!res.IsSuccessStatusCode)
            {
                return NotFound();
            }
            
            var contentLength = res.Content.Headers.ContentLength;

            if (contentLength is null)
                return NotFound();

            // Assume a modest 10% compression on .MOV files
            totalUpload += (int) (url.EndsWith(".mov") ? contentLength - contentLength / 10 : contentLength);
        }
        
        
        return totalUpload <= data.UploadLimit ? Ok() : new StatusCodeResult((int)HttpStatusCode.RequestEntityTooLarge);
    }

    [HttpPost("transcode")]
    public async Task<IActionResult> Transcode([FromBody] string url)
    {
        var client = _http.CreateClient();

        var content = await client.GetStreamAsync(url);
        
        var ffmpeg = Process.Start(_ffmpeg);

        await content.CopyToAsync(ffmpeg.StandardInput.BaseStream);
        
        var ms = new MemoryStream();
        
        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(ms);
        
        ffmpeg.Kill();
        
        return File(ms, "video/mp4");
    }
}