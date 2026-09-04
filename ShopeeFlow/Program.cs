using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using ShopeeFlow.Configurations;
using ShopeeFlow.Data;
using ShopeeFlow.Integrations.Ai;
using ShopeeFlow.Integrations.Shopee;
using ShopeeFlow.Integrations.WhatsApp;
using ShopeeFlow.Interfaces.Data;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Jobs;
using ShopeeFlow.Middleware;
using ShopeeFlow.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ShopeeFlow API",
        Version = "v1",
        Description = "Personal Shopee Affiliate automation API. Use header X-Api-Key with ApiSecurity:AccessToken."
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API token from local appsettings (ApiSecurity:AccessToken).",
        Name = ApiSecuritySettings.HeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    options.SchemaFilter<EnumSchemaFilter>();
});

builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddOptions<ShopeeAffiliateSettings>()
    .Bind(builder.Configuration.GetSection(ShopeeAffiliateSettings.SectionName))
    .Validate(settings => settings.HasRequiredValues(), "ShopeeAffiliate: BaseUrl, AppId and Secret are required.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ApiSecuritySettings>()
    .Bind(builder.Configuration.GetSection(ApiSecuritySettings.SectionName))
    .Validate(settings => settings.HasRequiredValues(), "ApiSecurity: AccessToken is required.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ScoringSettings>()
    .Bind(builder.Configuration.GetSection(ScoringSettings.SectionName));

builder.Services
    .AddOptions<PersistenceSettings>()
    .Bind(builder.Configuration.GetSection(PersistenceSettings.SectionName));

builder.Services
    .AddOptions<AiSettings>()
    .Bind(builder.Configuration.GetSection(AiSettings.SectionName));

builder.Services
    .AddOptions<PostingSettings>()
    .Bind(builder.Configuration.GetSection(PostingSettings.SectionName));

builder.Services
    .AddOptions<GreenApiSettings>()
    .Bind(builder.Configuration.GetSection(GreenApiSettings.SectionName));

builder.Services.AddSingleton<IShopeeSignatureService, ShopeeSignatureService>();
builder.Services.AddSingleton<IPublishedProductDAO, PublishedProductDAO>();
builder.Services.AddScoped<IProductScoreService, ProductScoreService>();
builder.Services.AddScoped<IProductOfferService, ProductOfferService>();
builder.Services.AddScoped<IProductPostMessageBuilder, ProductPostMessageBuilder>();
builder.Services.AddScoped<IProductPostingService, ProductPostingService>();
builder.Services.AddSingleton<LoggingWhatsAppSender>();
builder.Services.AddHttpClient<GreenApiWhatsAppSender>();
builder.Services.AddSingleton<IWhatsAppSender>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<GreenApiSettings>>().Value;
    if (settings.IsConfigured)
        return serviceProvider.GetRequiredService<GreenApiWhatsAppSender>();

    return serviceProvider.GetRequiredService<LoggingWhatsAppSender>();
});
builder.Services.AddHostedService<ProductPostingBackgroundService>();

builder.Services.AddHttpClient<IGeminiHeadlineClient, GeminiHeadlineClient>();
builder.Services.AddHttpClient<IShopeeGraphQlClient, ShopeeGraphQlClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<ShopeeAffiliateSettings>>().Value;
    client.Timeout = TimeSpan.FromSeconds(settings.GetTimeoutSecondsOrDefault());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiTokenMiddleware>();
app.MapControllers();

app.Run();
