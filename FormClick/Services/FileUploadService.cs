// Archivo: Services/FileUploadService.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading.Tasks;

public class FileUploadService
{
    private readonly string _targetFolder;
    private readonly IHostEnvironment _hostEnvironment;

    public FileUploadService(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
        _targetFolder = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "uploads", "profiles");
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile file)
    {
        if (file.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_targetFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/profiles/{fileName}";
        }

        return null;
    }
}
