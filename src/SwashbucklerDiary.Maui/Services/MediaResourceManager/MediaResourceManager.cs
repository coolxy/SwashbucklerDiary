﻿using Microsoft.Extensions.Logging;
using SwashbucklerDiary.Maui.BlazorWebView;
using SwashbucklerDiary.Rcl.Essentials;
using SwashbucklerDiary.Rcl.Services;
using SwashbucklerDiary.Shared;

namespace SwashbucklerDiary.Maui.Services
{
    public class MediaResourceManager : Rcl.Services.MediaResourceManager
    {
        private readonly HttpClient _httpClient;

        private readonly string _customPathPrefix = MauiBlazorWebViewHandler.AppFilePathMap[FileSystem.AppDataDirectory] + "/";

        protected override string? CustomPathPrefix => _customPathPrefix;

        public MediaResourceManager(IPlatformIntegration platformIntegration,
            IAppFileManager appFileManager,
            IAlertService alertService,
            II18nService i18nService,
            ILogger<MediaResourceManager> logger)
            : base(platformIntegration, appFileManager, alertService, i18nService, logger)
        {
            _httpClient = new HttpClient();
        }

        protected override Task<string?> CreateMediaResourceFileAsync(MediaResource mediaResource, string? sourceFilePath)
        {
            var targetDirectoryPath = Path.Combine(FileSystem.AppDataDirectory, MediaResourceFolders[mediaResource]);
            return CreateMediaResourceFileAsync(targetDirectoryPath, sourceFilePath);
        }

        public override async Task<string?> CreateMediaResourceFileAsync(string targetDirectoryPath, string? sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                return null;
            }

            using Stream stream = File.OpenRead(sourceFilePath);
            var fn = stream.CreateMD5() + Path.GetExtension(sourceFilePath);
            var targetFilePath = Path.Combine(targetDirectoryPath, fn);

            if (!File.Exists(targetFilePath))
            {
                if (sourceFilePath.StartsWith(FileSystem.CacheDirectory))
                {
                    stream.Close();
                    await _appFileManager.FileMoveAsync(sourceFilePath, targetFilePath);
                }
                else
                {
                    //将流的位置重置为起始位置
                    stream.Seek(0, SeekOrigin.Begin);
                    await _appFileManager.FileCopyAsync(targetFilePath, stream);
                }
            }

            return MauiBlazorWebViewHandler.FilePathToUrlRelativePath(targetFilePath);
        }

        public override async Task<bool> ShareImageAsync(string title, string url)
        {
            var filePath = await GetImageFilePathAsync(url);
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            await _platformIntegration.ShareFileAsync(title, filePath);
            return true;
        }

        public override async Task<bool> SaveImageAsync(string url)
        {
            var filePath = await GetImageFilePathAsync(url);
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            return await _platformIntegration.SaveFileAsync(filePath);
        }

        private async Task<string> GetImageFilePathAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            string? filePath;
            if (IsUrlBasedOnBaseUrl(url))
            {
                filePath = MauiBlazorWebViewHandler.UrlRelativePathToFilePath(url);
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = await CopyPackageFileAndCreateTempFileAsync(url);
                }
            }
            else
            {
                filePath = await DownloadFileAndCreateTempFileAsync(url);
            }

            if (string.IsNullOrEmpty(filePath))
            {
                await _alertService.Error(_i18n.T("Image.Not exist"));
            }

            return filePath;
        }

        bool IsUrlBasedOnBaseUrl(string url)
        {
            string baseUrl = MauiBlazorWebViewHandler.BaseUri;
            Uri baseUri = new Uri(baseUrl);
            Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);
            return baseUri.IsBaseOf(uri);
        }

        async Task<string> DownloadFileAndCreateTempFileAsync(string url)
        {
            string filePath = string.Empty;
            try
            {
                using Stream stream = await _httpClient.GetStreamAsync(url);
                var fileName = Path.GetFileName(url);
                filePath = await _appFileManager.CreateTempFileAsync(fileName, stream);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DownloadFile fail");
            }

            return filePath;
        }

        async Task<string> CopyPackageFileAndCreateTempFileAsync(string url)
        {
            var exists = await FileSystem.AppPackageFileExistsAsync($"wwwroot/{url}");
            if (!exists)
            {
                return string.Empty;
            }

            using var stream = await FileSystem.OpenAppPackageFileAsync($"wwwroot/{url}");
            var fileName = Path.GetFileName(url);
            return await _appFileManager.CreateTempFileAsync(fileName, stream);
        }
    }
}