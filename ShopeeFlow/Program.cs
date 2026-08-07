using Microsoft.Extensions.Options;
using ShopeeFlow.Configurations;
using ShopeeFlow.Integrations.Shopee;
using ShopeeFlow.Interfaces.Integrations;
using ShopeeFlow.Interfaces.Services;
using ShopeeFlow.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ShopeeFlow API",
        Version = "v1",
        Description = "Personal Shopee Affiliate automation API."
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

builder.Services.AddSingleton<IShopeeSignatureService, ShopeeSignatureService>();
builder.Services.AddScoped<IProductOfferService, ProductOfferService>();

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
app.MapControllers();

app.Run();
