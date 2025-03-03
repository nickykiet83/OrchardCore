using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.Admin;
using OrchardCore.ContentManagement.Records;
using YesSql;
using YesSql.Services;
using OrchardCore.Modules;
using OrchardCore.ContentFields.Services;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentLocalization;

namespace OrchardCore.ContentFields.Controllers
{

    [RequireFeatures("OrchardCore.ContentLocalization")]
    [Admin]
    public class LocalizationSetContentPickerAdminController : Controller
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentLocalizationManager _contentLocalizationManager;
        private readonly IContentManager _contentManager;
        private readonly ISession _session;

        public LocalizationSetContentPickerAdminController(
            IContentDefinitionManager contentDefinitionManager,
            IContentLocalizationManager contentLocalizationManager,
            IContentManager contentManager,
            ISession session
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentLocalizationManager = contentLocalizationManager;
            _contentManager = contentManager;
            _session = session;
        }

        [HttpGet]
        public async Task<IActionResult> SearchLocalizationSets(string part, string field, string query)
        {
            if (string.IsNullOrWhiteSpace(part) || string.IsNullOrWhiteSpace(field))
            {
                return BadRequest("Part and field are required parameters");
            }

            var partFieldDefinition = _contentDefinitionManager.GetPartDefinition(part)?.Fields
                .FirstOrDefault(f => f.Name == field);

            var fieldSettings = partFieldDefinition?.Settings.ToObject<LocalizationSetContentPickerFieldSettings>();
            if (fieldSettings == null)
            {
                return BadRequest("Unable to find field definition");
            }

            var dbQuery = _session.Query<ContentItem, ContentItemIndex>()
              .With<ContentItemIndex>(x => x.ContentType.IsIn(fieldSettings.DisplayedContentTypes) && x.Latest);

            if (!string.IsNullOrEmpty(query))
            {
                dbQuery.With<ContentItemIndex>(x => x.DisplayText.Contains(query) || x.ContentType.Contains(query));
            }

            var contentItems = await dbQuery.Take(40).ListAsync();

            // if 2 search results have the same set, select one based on the current culture
            var cleanedContentItems = await _contentLocalizationManager.DeduplicateContentItemsAsync(contentItems);

            var results = new List<VueMultiselectItemViewModel>();

            foreach (var contentItem in cleanedContentItems)
            {
                results.Add(new VueMultiselectItemViewModel
                {
                    Id = contentItem.Key, //localization set
                    DisplayText = contentItem.Value.ToString(),
                    HasPublished = await _contentManager.HasPublishedVersionAsync(contentItem.Value)
                });
            }

            return new ObjectResult(results);
        }
    }
}
