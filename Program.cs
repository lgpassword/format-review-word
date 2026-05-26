using WordFormatAnalyzer.Options;
using WordFormatAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.Configure<AppStorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<AppStorage>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<AnalysisSessionRepository>();
builder.Services.AddSingleton<WordAnalysisService>();
builder.Services.AddSingleton<WordComparisonService>();
builder.Services.AddSingleton<WordAnnotationService>();
builder.Services.AddSingleton<ReportRenderService>();
builder.Services.AddSingleton<WordConversionService>();
builder.Services.AddSingleton<IssueTextService>();
builder.Services.AddSingleton<PunctuationIssueService>();
builder.Services.AddSingleton<NormalDocumentService>();
builder.Services.AddSingleton<WordTerminologyService>();
builder.Services.AddSingleton<WordStyleResolver>();
builder.Services.AddSingleton<TemplateRuleConflictService>();

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
