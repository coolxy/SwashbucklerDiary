﻿using BlazorComponent;
using Microsoft.AspNetCore.Components;
using SwashbucklerDiary.Components;
using SwashbucklerDiary.IServices;
using SwashbucklerDiary.Models;

namespace SwashbucklerDiary.Pages
{
    public partial class IndexPage : PageComponentBase
    {
        private StringNumber tabs = 0;
        private List<DiaryModel> Diaries = new();
        private List<TagModel> Tags = new();
        private readonly List<string> Types = new() { "All", "Tags" };

        [Inject]
        public IDiaryService DiaryService { get; set; } = default!;
        [Inject]
        public ITagService TagService { get; set; } = default!;

        [Parameter]
        [SupplyParameterFromQuery]
        public string? Type { get; set; }

        protected override async Task OnInitializedAsync()
        {
            InitTab();
            await UpdateTags();
            await UpdateDiaries();
            await FirstLaunch();
            await base.OnInitializedAsync();
        }

        private bool ShowAddTag { get; set; }

        private async Task UpdateDiaries()
        {
            Diaries = await DiaryService.QueryTakeAsync(50, it => !it.Private);
        }

        private async Task UpdateTags()
        {
            Tags = await TagService.QueryAsync();
        }

        private void InitTab()
        {
            if (string.IsNullOrEmpty(Type))
            {
                Type = Types[0];
            }
            tabs = Types.IndexOf(Type!);
        }

        private async Task RefreshData(StringNumber value)
        {
            if (value == 0)
            {
                await UpdateDiaries();
            }
            else if (value == 1)
            {
                await UpdateTags();
            }
            var url = Navigation.GetUriWithQueryParameter("Type", Types[tabs.ToInt32()]);
            Navigation.NavigateTo(url);
        }

        private async Task SaveAddTag(string tagName)
        {
            ShowAddTag = false;
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return;
            }

            if (Tags.Any(it => it.Name == tagName))
            {
                await PopupService.ToastAsync(it =>
                {
                    it.Type = AlertTypes.Warning;
                    it.Title = I18n.T("Tag.Repeat.Title");
                    it.Content = I18n.T("Tag.Repeat.Content");
                });
                return;
            }

            TagModel tagModel = new()
            {
                Name = tagName
            };
            var flag = await TagService.AddAsync(tagModel);
            if (!flag)
            {
                await PopupService.ToastAsync(it =>
                {
                    it.Type = AlertTypes.Error;
                    it.Title = I18n.T("Share.AddFail");
                });
                return;
            }

            await PopupService.ToastAsync(it =>
            {
                it.Type = AlertTypes.Success;
                it.Title = I18n.T("Share.AddSuccess");
            });
            Tags.Add(tagModel);
            this.StateHasChanged();
        }

        private string GetWelcomeText()
        {
            int hour = Convert.ToInt16(DateTime.Now.ToString("HH"));
            if (hour >= 6 && hour < 11)
            {
                return I18n.T("Index.Welcome.Morning")!;
            }
            else if (hour >= 11 && hour < 13)
            {
                return I18n.T("Index.Welcome.Noon")!;
            }
            else if (hour >= 13 && hour < 18)
            {
                return I18n.T("Index.Welcome.Afternoon")!;
            }
            else if (hour >= 18 && hour < 23)
            {
                return I18n.T("Index.Welcome.Night")!;
            }
            else if (hour >= 23 || hour < 6)
            {
                return I18n.T("Index.Welcome.BeforeDawn")!;
            }
            return "Hello World";
        }

        private async Task FirstLaunch()
        {
            if (Diaries.Count > 0)
            {
                return;
            }
            bool flag = SystemService.IsFirstLaunch();
            if (flag)
            {
                var uri = I18n.T("FilePath.FunctionalDescription")!;
                string content = await SystemService.ReadMarkdown(uri);
                var diary = new DiaryModel()
                {
                    Title = I18n.T("FunctionalDescription"),
                    Content = content,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                };
                await DiaryService.AddAsync(diary);
                await UpdateDiaries();
            }
        }
    }
}
