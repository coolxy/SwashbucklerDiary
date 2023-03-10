﻿using BlazorComponent;
using SwashbucklerDiary.Components;
using SwashbucklerDiary.Config;

namespace SwashbucklerDiary.Pages
{
    public partial class BackupsPage : PageComponentBase
    {
        private StringNumber tabs = 0;
        private bool ShowBackups;
        private bool ShowRestore;
        private string? BackupsFolderPath;
        private string? RestoreFilePath;

        protected override async Task OnInitializedAsync()
        {
            await SetBackupsFolderPath();
            await base.OnInitializedAsync();
        }

        private bool ShowEditButton => string.IsNullOrEmpty(BackupsFolderPath);

        private async Task SetBackupsFolderPath()
        {
            BackupsFolderPath = await SettingsService.Get("BackupsPath", string.Empty);
            if (string.IsNullOrEmpty(BackupsFolderPath))
            {
                return;
            }

            if (!Directory.Exists(BackupsFolderPath))
            {
                BackupsFolderPath = string.Empty;
                await SettingsService.Save("BackupsPath", BackupsFolderPath);
            }
        }

        private async Task Backups()
        {
            ShowBackups = false;
            var readPermission = await SystemService.CheckStorageReadPermission();
            if (!readPermission)
            {
                await AlertService.Success(I18n.T("Permission.OpenStorageRead"));
                return;
            }

            var writePermission = await SystemService.CheckStorageWritePermission();
            if (!writePermission)
            {
                await AlertService.Error(I18n.T("Permission.OpenStorageWrite"));
                return;
            }

            var folderPath = await SettingsService.Get("BackupsPath", string.Empty);
            if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
            {
                BackupsFolderPath = string.Empty;
                await SettingsService.Save("BackupsPath", string.Empty);
                await AlertService.Error(I18n.T("Backups.Invalid backup folder"));
                return;
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                var flag = await PickBackupsFolderPath();
                if (!flag)
                {
                    return;
                }
            }

            var sourceFile = SQLiteConstants.DatabasePath;
            if (!File.Exists(sourceFile))
            {
                return;
            }

            var destFileName = "SwashbucklerDiaryBackups" +
                DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") +
                SystemService.GetAppVersion() + ".db3";
            var destFile = Path.Combine(BackupsFolderPath!, destFileName);
            File.Copy(sourceFile, destFile, true);
            await AlertService.Success(I18n.T("Backups.BackupsSuccess"));
        }

        private string? GetLocalPointer()
        {
            if (string.IsNullOrEmpty(BackupsFolderPath))
            {
                return I18n.T("Backups.SelectFolder");
            }
            else
            {
                return I18n.T("Backups.LocalPointer") + BackupsFolderPath;
            }
        }

        private Task EditBackupsFolderPath()
        {
            return PickBackupsFolderPath();
        }

        private async Task<bool> PickBackupsFolderPath()
        {
            var folderPath = await SystemService.PickFolderAsync();
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            if (!Directory.Exists(folderPath))
            {
                return false;
            }

            folderPath = Path.Combine(folderPath, "SwashbucklerDiaryBackups");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            BackupsFolderPath = folderPath;
            await SettingsService.Save("BackupsPath", folderPath);
            return true;
        }

        private async Task Restore()
        {
            RestoreFilePath = string.Empty;
            RestoreFilePath = await SystemService.PickDBFileAsync();
            if (string.IsNullOrEmpty(RestoreFilePath))
            {
                return;
            }
            ShowRestore = true;
        }

        private async Task ConfirmRestore()
        {
            ShowRestore = false;
            if (string.IsNullOrEmpty(RestoreFilePath))
            {
                await AlertService.Success(I18n.T("Backups.RestoreFail"));
                return;
            }
            
            File.Copy(RestoreFilePath, SQLiteConstants.DatabasePath, true);
            await AlertService.Success(I18n.T("Backups.RestoreSuccess"));
        }
    }
}
