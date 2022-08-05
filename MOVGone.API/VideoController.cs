using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace NoMOV.API;

[Route("api")]
[ApiController]
public class VideoController : Controller
{
    private readonly IHttpClientFactory _http;
    
    private readonly string _tempPath = 
        OperatingSystem.IsLinux() 
        ? "/tmp"
        : Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "MOVGone");
    
    public VideoController(IHttpClientFactory http) => _http = http;

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
        if (OperatingSystem.IsWindows())
            Directory.CreateDirectory(_tempPath);
        
        var id = Guid.NewGuid().ToString();
        var path = $"{_tempPath}/{id}.mp4";
        
        using var client = _http.CreateClient();
        using var content = await client.GetStreamAsync(url);
        using var fs = new FileStream(path, FileMode.Create);

        await content.CopyToAsync(fs);
        await fs.FlushAsync();

        using var ffmpeg = StartFFMpeg(path);

        var ms = new MemoryStream();
        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(ms);

        ms.Seek(0, SeekOrigin.Begin);

        await ffmpeg.WaitForExitAsync();
        
        return File(ms, "video/mp4");
    }

    private Process StartFFMpeg(string path)
    {
        var ffmpeg = Process.Start(new ProcessStartInfo("./ffmpeg", 
            $"-hide_banner -v quiet -f mov -i {path} " +
            $"-c:v libx264 -crf 27 -f mp4 -movflags +frag_keyframe+empty_moov+faststart " +
            $" pipe:1")
        {
            RedirectStandardOutput = true
        });
        
        return ffmpeg;
    }
}