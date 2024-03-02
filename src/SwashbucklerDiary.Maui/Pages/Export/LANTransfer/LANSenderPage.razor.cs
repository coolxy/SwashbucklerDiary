﻿using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SwashbucklerDiary.Maui.Services;
using SwashbucklerDiary.Rcl.Components;
using SwashbucklerDiary.Rcl.Services;
using SwashbucklerDiary.Shared;

namespace SwashbucklerDiary.Maui.Pages
{
    public partial class LANSenderPage : ImportantComponentBase
    {
        private int ps;

        private long totalBytes;

        private long bytes;

        private string? filePath;

        private readonly string multicastAddress = "239.0.0.1";

        private readonly int millisecondsOutTime = 20000;

        private int multicastPort;

        private int tcpPort;

        private bool showTransferDialog;

        private readonly List<LANDeviceInfoListItem> lanDeviceInfoListItems = [];

        [Inject]
        private ILANSenderService LANSenderService { get; set; } = default!;

        [Inject]
        private IDiaryService DiaryService { get; set; } = default!;

        [Inject]
        private IDiaryFileManager DiaryFileManager { get; set; } = default!;

        [Inject]
        private IIconService IconService { get; set; } = default!;

        [Inject]
        private ILogger<LANSenderPage> Logger { get; set; } = default!;

        private class LANDeviceInfoListItem : LANDeviceInfo
        {
            public LANDeviceInfoListItem(LANDeviceInfo deviceInfo)
            {
                DeviceName = deviceInfo.DeviceName;
                IPAddress = deviceInfo.IPAddress;
                DevicePlatform = deviceInfo.DevicePlatform;
            }

            public string? DeviceIcon { get; set; }

            public Func<Task> OnClick { get; set; } = () => Task.CompletedTask;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LANSenderService.LANDeviceFound += LANDeviceFound;
            LANSenderService.SearchEnded += SearchEnded;
            LANSenderService.SendProgressChanged += SendProgressChanged;
            LANSenderService.SendCompleted += SendingCompleted;
            LANSenderService.SendAborted += SendAborted;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await LoadSettings();
                await InitializeLANSenderService();
                StateHasChanged();
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            showTransferDialog = false;
            LANSenderService.Dispose();
            LANSenderService.LANDeviceFound -= LANDeviceFound;
            LANSenderService.SearchEnded -= SearchEnded;
            LANSenderService.SendProgressChanged -= SendProgressChanged;
            LANSenderService.SendCompleted -= SendingCompleted;
            LANSenderService.SendAborted -= SendAborted;
        }

        private async Task LoadSettings()
        {
            multicastPort = await SettingService.Get<int>(Setting.LANScanPort);
            tcpPort = await SettingService.Get<int>(Setting.LANTransmissionPort);
        }

        private async Task InitializeLANSenderService()
        {
            try
            {
                LANSenderService.Initialize(multicastAddress, multicastPort, tcpPort, millisecondsOutTime);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "LANSenderService initialize error");
                await AlertService.Error(I18n.T("lanSender.No network connection"));
                await Task.Delay(1000);
                await NavigateToBack();
            }
        }

        private async Task Send(string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return;
            }

            if (LANSenderService.IsSending)
            {
                return;
            }

            ResetSendProgress();
            showTransferDialog = true;
            StateHasChanged();

            if (filePath == null)
            {
                var diaries = await DiaryService.QueryAsync();
                if (diaries.Count == 0)
                {
                    await AlertService.Info(I18n.T("Diary.NoDiary"));
                    return;
                }

                filePath = await DiaryFileManager.ExportJsonAsync(diaries);
            }

            LANSenderService.Send(ipAddress, filePath);
        }

        private void LANDeviceFound(LANDeviceInfo deviceInfo)
        {
            if (!lanDeviceInfoListItems.Any(it => it.IPAddress == deviceInfo.IPAddress))
            {
                LANDeviceInfoListItem lanDeviceInfoListItem = new(deviceInfo)
                {
                    DeviceIcon = IconService.GetDeviceSystemIcon(deviceInfo.DevicePlatform),
                    OnClick = () => Send(deviceInfo.IPAddress)
                };

                lanDeviceInfoListItems.Add(lanDeviceInfoListItem);
                InvokeAsync(StateHasChanged);
            }
        }

        private void SendProgressChanged(long readLength, long allLength)
        {
            //只有每次传输大于512k且达到1%以上才会刷新进度
            var percentage = (int)(((double)readLength / allLength) * 100);
            bool refresh = (percentage - ps > 1 && readLength / 1024 - bytes > 512) || percentage == 100;

            if (refresh)
            {
                ps = percentage; //传输完成百分比
                bytes = readLength / 1024; //当前已经传输的Kb
                totalBytes = allLength / 1024; //文件总大小Kb
                InvokeAsync(StateHasChanged);
            }
        }

        private void SendingCompleted()
        {
            InvokeAsync(async () =>
            {
                await AlertService.Success(I18n.T("lanSender.Send successfully"));
            });
        }

        private void SendAborted()
        {
            InvokeAsync(async () =>
            {
                if (showTransferDialog)
                {
                    await AlertService.Error(I18n.T("lanSender.Send failed"));
                }
                else
                {
                    await AlertService.Error(I18n.T("lanSender.Send canceled"));
                }

                if (IsCurrentPage)
                {
                    await NavigateToBack();
                }
            });
        }

        private async Task CancelSend()
        {
            showTransferDialog = false;
            if (LANSenderService.IsSending)
            {
                LANSenderService.CancelSend();
            }
            else
            {
                await NavigateToBack();
            }
        }

        private void SearchEnded()
        {
            InvokeAsync(StateHasChanged);
        }

        private void ResetSendProgress()
        {
            ps = 0;
            bytes = 0;
            totalBytes = 0;
        }
    }
}
