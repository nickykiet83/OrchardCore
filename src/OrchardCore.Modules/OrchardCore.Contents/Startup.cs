using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.AdminMenu.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentManagement.Routing;
using OrchardCore.Contents.AdminNodes;
using OrchardCore.Contents.Deployment;
using OrchardCore.Contents.Drivers;
using OrchardCore.Contents.Feeds.Builders;
using OrchardCore.Contents.Handlers;
using OrchardCore.Contents.Indexing;
using OrchardCore.Contents.Liquid;
using OrchardCore.Contents.Models;
using OrchardCore.Contents.Recipes;
using OrchardCore.Contents.Security;
using OrchardCore.Contents.Services;
using OrchardCore.Contents.TagHelpers;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Entities;
using OrchardCore.Feeds;
using OrchardCore.Indexing;
using OrchardCore.Liquid;
using OrchardCore.Lists.Settings;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Contents
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddContentManagement();
            services.AddContentManagementDisplay();
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<IPermissionProvider, ContentTypePermissions>();
            services.AddScoped<IAuthorizationHandler, ContentTypeAuthorizationHandler>();
            services.AddScoped<IShapeTableProvider, Shapes>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddScoped<IContentDisplayDriver, ContentsDriver>();
            services.AddScoped<IContentHandler, ContentsHandler>();
            services.AddRecipeExecutionStep<ContentStep>();

            services.AddScoped<IContentItemIndexHandler, AspectsContentIndexHandler>();
            services.AddScoped<IContentItemIndexHandler, DefaultContentIndexHandler>();
            services.AddScoped<IContentAliasProvider, ContentItemIdAliasProvider>();
            services.AddScoped<IContentItemIndexHandler, ContentItemIndexCoordinator>();

            services.AddIdGeneration();
            services.AddScoped<IDataMigration, Migrations>();

            // Common Part
            services.AddSingleton<ContentPart, CommonPart>();
            services.AddScoped<IContentTypePartDefinitionDisplayDriver, CommonPartSettingsDisplayDriver>();
            services.AddScoped<IContentPartDisplayDriver, DateEditorDriver>();
            services.AddScoped<IContentPartDisplayDriver, OwnerEditorDriver>();

            // Feeds
            // TODO: Move to feature
            services.AddScoped<IFeedItemBuilder, CommonFeedItemBuilder>();

            services.AddTagHelpers<ContentLinkTagHelper>();

            services.Configure<AutorouteOptions>(options =>
            {
                if (options.GlobalRouteValues.Count == 0)
                {
                    options.GlobalRouteValues = new RouteValueDictionary
                    {
                        {"Area", "OrchardCore.Contents"},
                        {"Controller", "Item"},
                        {"Action", "Display"}
                    };

                    options.ContentItemIdKey = "contentItemId";
                }
            });
        }

        public override void Configure(IApplicationBuilder builder, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaRoute(
                name: "DisplayContentItem",
                areaName: "OrchardCore.Contents",
                template: "Contents/ContentItems/{contentItemId}",
                defaults: new { controller = "Item", action = "Display" }
            );

            routes.MapAreaRoute(
                name: "PreviewContentItem",
                areaName: "OrchardCore.Contents",
                template: "Contents/ContentItems/{contentItemId}/Preview",
                defaults: new { controller = "Item", action = "Preview" }
            );

            routes.MapAreaRoute(
                name: "PreviewContentItemVersion",
                areaName: "OrchardCore.Contents",
                template: "Contents/ContentItems/{contentItemId}/Version/{version}/Preview",
                defaults: new { controller = "Item", action = "Preview" }
            );

            // Admin
            routes.MapAreaRoute(
                name: "EditContentItem",
                areaName: "OrchardCore.Contents",
                template: "Admin/Contents/ContentItems/{contentItemId}/Edit",
                defaults: new { controller = "Admin", action = "Edit" }
            );

            routes.MapAreaRoute(
                name: "CreateContentItem",
                areaName: "OrchardCore.Contents",
                template: "Admin/Contents/ContentTypes/{id}/Create",
                defaults: new { controller = "Admin", action = "Create" }
            );

            routes.MapAreaRoute(
                name: "AdminContentItem",
                areaName: "OrchardCore.Contents",
                template: "Admin/Contents/ContentItems/{contentItemId}/Display",
                defaults: new { controller = "Admin", action = "Display" }
            );

            routes.MapAreaRoute(
                name: "ListContentItems",
                areaName: "OrchardCore.Contents",
                template: "Admin/Contents/ContentItems/{contentTypeId?}",
                defaults: new { controller = "Admin", action = "List" }
            );
        }
    }

    [RequireFeatures("OrchardCore.Liquid")]
    public class LiquidStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ILiquidTemplateEventHandler, ContentLiquidTemplateEventHandler>();

            services.AddLiquidFilter<BuildDisplayFilter>("shape_build_display");
            services.AddLiquidFilter<ContentItemFilter>("content_item_id");
        }
    }

    [RequireFeatures("OrchardCore.Deployment")]
    public class DeploymentStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDeploymentSource, AllContentDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<AllContentDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, AllContentDeploymentStepDriver>();

            services.AddTransient<IDeploymentSource, ContentDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<ContentDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, ContentDeploymentStepDriver>();
        }
    }


    [RequireFeatures("OrchardCore.AdminMenu")]
    public class AdminMenuStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<ContentTypesAdminNode>());
            services.AddScoped<IAdminNodeNavigationBuilder, ContentTypesAdminNodeNavigationBuilder>();
            services.AddScoped<IDisplayDriver<MenuItem>, ContentTypesAdminNodeDriver>();
        }
    }

    [Feature("OrchardCore.Contents.FileContentDefinition")]
    public class FileContentDefinitionStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddFileContentDefinitionStore();
        }
    }
}
