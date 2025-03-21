using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SwashbucklerDiary.Rcl.Components
{
    public class MarkdownJSModule : CustomJSModule
    {
        public MarkdownJSModule(IJSRuntime js) : base(js, "js/markdown-helper.js")
        {
        }

        public async Task After(DotNetObjectReference<object> dotNetObjectReference, ElementReference element)
        {
            await base.InvokeVoidAsync("after", [dotNetObjectReference, element]);
        }

        public async Task Focus(ElementReference element)
        {
            await base.InvokeVoidAsync("focus", element);
        }

        public async Task FocusToEnd(ElementReference element)
        {
            await base.InvokeVoidAsync("focusToEnd", element);
        }

        public async Task Upload(ElementReference element, ElementReference? inputFileElement)
        {
            await base.InvokeVoidAsync("upload", element, inputFileElement);
        }
    }
}
