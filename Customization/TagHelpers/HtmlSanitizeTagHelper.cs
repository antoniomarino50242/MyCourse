using System;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Ganss.XSS;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MyCourse.Customization.TagHelpers
{
    [HtmlTargetElement(Attributes = "html-sanitize")]
    public class HtmlSanitizeTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            //otteniamo il contenuto del tag
            TagHelperContent tagHelperContent = await output.GetChildContentAsync(NullHtmlEncoder.Default);
            string content = tagHelperContent.GetContent(NullHtmlEncoder.Default);

            //Sanitizzazione
            var sanitizer = CreaSanitizer();
            content = sanitizer.Sanitize(content);

            //Reimpostiamo il contenuto del tag
            output.Content.SetHtmlContent(content);
        }

        private static HtmlSanitizer CreaSanitizer()
        {
            var sanitizer = new HtmlSanitizer();
            
            //Tag Consentiti
            sanitizer.AllowedTags.Clear();      //cancello tutti i tag creati di defaul
            sanitizer.AllowedTags.Add("b");     //aggiungo tag grassetti
            sanitizer.AllowedTags.Add("i");     //tag corsivo
            sanitizer.AllowedTags.Add("p");     //tag paragrafi
            sanitizer.AllowedTags.Add("br");     
            sanitizer.AllowedTags.Add("ul");        
            sanitizer.AllowedTags.Add("ol");     //tag liste puntate
            sanitizer.AllowedTags.Add("li");     //tag liste numerate
            sanitizer.AllowedTags.Add("iframe"); //tag video

            //attributi consentiti
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("src"); //solo attributo src
            sanitizer.AllowDataAttributes = false;  

            //stili consentiti
            sanitizer.AllowedCssProperties.Clear();

            sanitizer.FilterUrl += FilterUrl;
            sanitizer.PostProcessNode += ProcessIFrames;

            return sanitizer;
        }

        private static void ProcessIFrames(object sender, PostProcessNodeEventArgs postProcessNodeEventArgs)
        {
            var iFrame = postProcessNodeEventArgs.Node as IHtmlInlineFrameElement;
            if (iFrame == null)
            {
                return;
            }
            var container = postProcessNodeEventArgs.Document.CreateElement("span");
            container.ClassName = "video-container";
            container.AppendChild(iFrame.Clone(true));
            postProcessNodeEventArgs.ReplacementNodes.Add(container);
        }

        private static void FilterUrl(object sender, FilterUrlEventArgs filterUrlEventArgs)
        {
            if (!filterUrlEventArgs.OriginalUrl.StartsWith("//www.youtube.com/") && !filterUrlEventArgs.OriginalUrl.StartsWith("https://www.youtube.com/"))
            {
                filterUrlEventArgs.SanitizedUrl = null;
            }
        }
    }
}