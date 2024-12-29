using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;

public class S3Service {
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketImageProfile;
    private readonly string _bucketImageTemplate;

    public S3Service(){
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-2";
        _bucketImageProfile = Environment.GetEnvironmentVariable("AWS_BUCKET_IMAGE_PROFILE") ?? "";
        _bucketImageTemplate = Environment.GetEnvironmentVariable("AWS_BUCKET_IMAGE_TEMPLATE") ?? "";

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(_bucketImageProfile) || string.IsNullOrEmpty(_bucketImageTemplate))
            throw new InvalidOperationException("AWS credentials or bucket name not configured.");

        var config = new AmazonS3Config {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder) {
        try {
            var request = new TransferUtilityUploadRequest {
                InputStream = fileStream,
                Key = fileName,
                ContentType = contentType,
                BucketName = folder == "ImageProfile" ? _bucketImageProfile : _bucketImageTemplate
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(request);

            return $"https://{(folder == "ImageProfile" ? _bucketImageProfile : _bucketImageTemplate)}.s3.amazonaws.com/{fileName}";
        } catch (Exception ex) {
            throw new InvalidOperationException("File upload failed.", ex);
        }
    }
}
