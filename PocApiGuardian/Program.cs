using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Enregistre IHttpClientFactory pour permettre la création d'instances HttpClient via DI
builder.Services.AddHttpClient();

// Récupère la clé API au démarrage depuis la configuration (WeatherApi:ApiKey)
// Si la clé est absente ou vide, on lève une InvalidOperationException dès l'initialisation
var startupApiKey = builder.Configuration["WeatherApi:ApiKey"];
if (string.IsNullOrWhiteSpace(startupApiKey))
{
    throw new InvalidOperationException("La valeur de configuration 'WeatherApi:ApiKey' doit être fournie.");
}

var app = builder.Build();

// Crée un endpoint HTTP GET unique: /api/weather/{city}
// Cet endpoint agit comme un "gardien" (proxy sécurisé) vers OpenWeatherMap
app.MapGet("/api/weather/{city}", async (string city, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    // Récupère la clé API depuis la configuration (au moment de la requête)
    var apiKey = config["WeatherApi:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        // Si la clé a disparu en runtime, on retourne un problème HTTP
        return Results.Problem("La clé API est absente de la configuration.");
    }

    // Construit l'URL pour l'appel OpenWeatherMap avec les paramètres nécessaires
    var baseUrl = "https://api.openweathermap.org/data/2.5/weather";
    var url = $"{baseUrl}?q={Uri.EscapeDataString(city)}&appid={Uri.EscapeDataString(apiKey)}&units=metric&lang=fr";

    // Crée un HttpClient via IHttpClientFactory et appelle l'API distante
    var client = clientFactory.CreateClient();
    HttpResponseMessage response;
    try
    {
        response = await client.GetAsync(url);
    }
    catch (Exception ex)
    {
        // En cas d'erreur réseau ou d'échec d'appel, retourne un Problem 503
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Erreur lors de l'appel à l'API météo distante");
    }

    // Si le service distant renvoie une erreur HTTP, on retourne un Problem qui reflète le code HTTP reçu
    if (!response.IsSuccessStatusCode)
    {
        var detail = $"L'API distante a renvoyé {(int)response.StatusCode} {response.ReasonPhrase}";
        return Results.Problem(detail: detail, statusCode: (int)response.StatusCode, title: "Erreur du fournisseur météo externe");
    }

    // Lit le JSON brut et le renvoie tel quel avec le type MIME application/json
    var jsonContent = await response.Content.ReadAsStringAsync();
    return Results.Content(jsonContent, "application/json");
});

app.Run();
