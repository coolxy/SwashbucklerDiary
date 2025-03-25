using SwashbucklerDiary.Rcl.Essentials;
using SwashbucklerDiary.Shared;
using System.Net.Http.Json;

namespace SwashbucklerDiary.Rcl.Services
{
    public abstract class VersionUpdataManager : IVersionUpdataManager
    {
        public event Action? AfterFirstEnter;

        public event Action? AfterVersionUpdate;

        public event Action? AfterCheckFirstLaunch;

        protected int updateCount;

        protected const string LatestVersionApiUrl = "https://api.github.com/repos/Yu-Core/SwashbucklerDiary/releases/latest";

        protected const string zh_CNLatestVersionApiUrl = "https://gitee.com/api/v5/repos/Yu-core/SwashbucklerDiary/releases/latest";

        protected readonly IDiaryService _diaryService;

        protected readonly IResourceService _resourceService;

        protected readonly ISettingService _settingService;

        protected readonly IMediaResourceManager _mediaResourceManager;

        protected readonly II18nService _i18n;

        protected readonly IVersionTracking _versionTracking;

        protected readonly IDiaryFileManager _diaryFileManager;

        protected readonly IStaticWebAssets _staticWebAssets;

        private Lazy<HttpClient> _httpClient;

        private readonly SortedDictionary<Version, Func<Task>> _versionHandlers = [];

        private class Release
        {
            public string? Tag_Name { get; set; }
        }

        public VersionUpdataManager(IDiaryService diaryService,
            IResourceService resourceService,
            ISettingService settingService,
            IMediaResourceManager mediaResourceManager,
            II18nService i18n,
            IVersionTracking versionTracking,
            IDiaryFileManager diaryFileManager,
            IStaticWebAssets staticWebAssets)
        {
            _diaryService = diaryService;
            _resourceService = resourceService;
            _settingService = settingService;
            _mediaResourceManager = mediaResourceManager;
            _i18n = i18n;
            _versionTracking = versionTracking;
            _diaryFileManager = diaryFileManager;
            _staticWebAssets = staticWebAssets;
            _httpClient = new(() =>
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; Trident/6.0)");
                return httpClient;
            });

            InitializeVersionHandlers();
        }

        public void NotifyAfterFirstEnter()
        {
            AfterFirstEnter?.Invoke();
        }

        public async Task HandleVersionUpdate()
        {
            foreach (var handler in _versionHandlers)
            {
                await HandleVersionUpdate(handler.Key.ToString(), handler.Value);
            }

            var version = await _staticWebAssets.ReadJsonAsync<string>("docs/update-instruction/version.json");
            await HandleVersionUpdate(version, HandleUpdateInstruction);
            if (updateCount > 0)
            {
                AfterVersionUpdate?.Invoke();
            }
        }

        public async Task<bool> CheckForUpdates()
        {
            var url = _i18n.Culture.Name == "zh-CN" 
                ? zh_CNLatestVersionApiUrl
                : LatestVersionApiUrl;
            var release = await HttpClient.GetFromJsonAsync<Release>(url);
            if (release is null || release.Tag_Name is null)
            {
                throw new Exception("Tag format error");
            }

            var latestVersion = new Version(release.Tag_Name.TrimStart('v'));
            var currentVersion = new Version(_versionTracking.CurrentVersion);
            if (latestVersion.CompareTo(currentVersion) > 0)
            {
                return true;
            }

            return false;
        }

        public abstract Task ToUpdate();

        protected HttpClient HttpClient => _httpClient.Value;

        protected virtual void InitializeVersionHandlers()
        {
            AddVersionHandler("0.69.7", HandleVersionUpdate697);
            AddVersionHandler("0.80.9", HandleVersionUpdate809);
            AddVersionHandler("0.86.0", HandleVersionUpdate860);
            AddVersionHandler("1.01.5", HandleVersionUpdate1015);
            AddVersionHandler("1.03.9", HandleVersionUpdate1039);
            AddVersionHandler("1.12.9", HandleVersionUpdate1129);
        }

        protected void AddVersionHandler(string versionString, Func<Task> handler)
        {
            var version = Version.Parse(versionString);
            _versionHandlers[version] = handler;
        }

        protected async Task HandleVersionUpdate(string version, Func<Task> func)
        {
            string? strPreviousVersion = _versionTracking.PreviousVersion;
            if (string.IsNullOrEmpty(strPreviousVersion))
            {
                return;
            }

            bool first = _versionTracking.IsFirstLaunchForCurrentVersion;
            if (!first)
            {
                return;
            }

            var previousVersion = new Version(strPreviousVersion);
            var targetVersion = new Version(version);
            int result = previousVersion.CompareTo(targetVersion);

            // The previous run version is greater than the target version
            if (result > 0)
            {
                return;
            }

            updateCount++;
            await func.Invoke();
        }

        protected virtual Task HandleVersionUpdate697()
        {
            return Task.CompletedTask;
        }

        public void NotifyAfterCheckFirstLaunch()
        {
            AfterCheckFirstLaunch?.Invoke();
        }

        private async Task HandleVersionUpdate809()
        {
            var uri = $"docs/vditor-tutorial/{_i18n.Culture}.md";
            var content = await _staticWebAssets.ReadContentAsync(uri);
            var diary = new DiaryModel()
            {
                Content = content,
            };
            await _diaryService.AddAsync(diary);
        }

        private async Task HandleUpdateInstruction()
        {
            var uri = $"docs/update-instruction/{_i18n.Culture}.md";
            var content = await _staticWebAssets.ReadContentAsync(uri);
            var diary = new DiaryModel()
            {
                Content = content,
            };
            await _diaryService.AddAsync(diary);
        }

        private async Task HandleVersionUpdate860()
        {
            string[] keys = ["PrivacyMode", "PrivatePassword"];
            await _settingService.RemoveAsync(keys);
        }

        private async Task HandleVersionUpdate1015()
        {
            var oldKey = "DiaryCardDateFormat";
            var oldValue = await _settingService.GetAsync(oldKey, string.Empty);
            if (!string.IsNullOrEmpty(oldValue))
            {
                await _settingService.RemoveAsync(oldKey);
                await _settingService.SetAsync("DiaryCardTimeFormat", oldValue);
            }
        }

        private async Task HandleVersionUpdate1039()
        {
            await _diaryService.MovePrivacyDiariesAsync();
        }

        private async Task HandleVersionUpdate1129()
        {
            var oldKey = "UserName";
            var oldValue = await _settingService.GetAsync(oldKey, string.Empty);
            if (!string.IsNullOrEmpty(oldValue))
            {
                await _settingService.RemoveAsync(oldKey);
                await _settingService.SetAsync("NickName", oldValue);
            }
        }
    }
}
