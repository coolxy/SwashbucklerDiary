﻿using Microsoft.AspNetCore.Components;
using SwashbucklerDiary.Rcl.Essentials;
using SwashbucklerDiary.Rcl.Services;

namespace SwashbucklerDiary.Rcl.Components
{
    public partial class MarkdownPreview
    {
        private Dictionary<string, object>? _options;

        [Inject]
        protected II18nService I18n { get; set; } = default!;

        [CascadingParameter(Name = "Culture")]
        public string? Culture { get; set; }

        [CascadingParameter(Name = "IsDark")]
        public bool Dark { get; set; }

        [Parameter]
        public string? Value { get; set; }

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public string? Style { get; set; }

        private bool Show => !string.IsNullOrEmpty(Value) && _options is not null;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            SetOptions();
        }

        private void SetOptions()
        {
            string lang = I18n.Culture.Name.Replace("-", "_");
            string mode = Dark ? "dark" : "light";
            lang = lang.Replace("-", "_");
            var theme = new Dictionary<string, object?>()
            {
                { "current", mode },
                { "path", $"_content/{StaticWebAssets.RclAssemblyName}/npm/vditor/3.9.6/dist/css/content-theme" }
            };

            _options = new()
            {
                { "mode", mode },
                { "cdn", $"_content/{StaticWebAssets.RclAssemblyName}/npm/vditor/3.9.6" },
                { "lang", lang },
                { "theme", theme },
                { "icon", "material" },
            };
        }
    }
}
