# PoC 1 : Le "Gardien" d'API Hybride (.NET + N8N)

Bienvenue sur **Hybrid CodeFlow AI** ! Ceci est notre premier PoC stratégique.

Il démontre comment résoudre un problème critique de **sécurité** et de **maintenance** dans les outils No-Code en utilisant un pont hybride .NET.

### 1. Le Problème : Le Cauchemar des Clés API en No-Code

Les outils No-Code comme N8N sont incroyables pour l'orchestration. Cependant, un problème majeur survient lorsque nous devons nous connecter à des services tiers sensibles (Stripe, OpenAI, Google Maps...) : **Où stocker la clé API ?**

La mettre en clair dans un nœud N8N est :
* **Un Risque de Sécurité :** Toute personne ayant accès au workflow voit la clé.
* **Un Problème de Maintenance :** Si la clé change (rotation, révocation), vous devez la mettre à jour dans *tous* les workflows qui l'utilisent.

### 2. Notre Solution Hybride : .NET (Le Code) + N8N (Le Flow)

Nous utilisons notre **Angle Hybride** : N8N orchestre, .NET sécurise.

1.  **.NET (Le "Gardien") :** Nous construisons une API .NET Minimal qui stocke la clé API de manière sécurisée (via `user-secrets`) et expose un endpoint simple.
2.  **N8N (Le "Flow") :** N8N appelle *notre* API .NET. Il ne connaît pas la clé secrète. Il récupère les données propres et applique la logique métier (ex: "Si la température est basse, envoyer une alerte").

Voici le workflow N8N final, qui illustre parfaitement cette séparation des rôles :

<img width="1190" height="438" alt="image" src="https://github.com/user-attachments/assets/076feaf5-fbef-4752-aaa5-ac3ec821a45c" />


### 3. La Stack Technique (Gratuite)

* **Orchestrateur :** N8N (v1.119.2+)
* **Gardien API :** .NET 8 (Minimal API)
* **Service Tiers (Simulation) :** [OpenWeatherMap](https://openweathermap.org/api) (API météo gratuite)

---

### 4. Les Actifs (Code Source)

#### Partie A : Le "Gardien" (.NET Minimal API)

Créez une API .NET (avec `dotnet new webapi -minimal`).

**1. Sécuriser la Clé (dans le Terminal) :**
*Initialisez les secrets (une seule fois)*
`dotnet user-secrets init`
*Stockez votre clé (remplacez par votre clé)*
`dotnet user-secrets set "WeatherApi:ApiKey" "VOTRE_VRAIE_CLE_API_ICI"`

**2. Le Code (`Program.cs`) :**
Le code lit la clé depuis les `user-secrets`, appelle l'API externe, et retourne le JSON propre à N8N.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Récupérer la clé API depuis la configuration (liée aux User Secrets)
var weatherApiKey = builder.Configuration["WeatherApi:ApiKey"];

if (string.IsNullOrEmpty(weatherApiKey))
{
    throw new InvalidOperationException("La clé API Météo (WeatherApi:ApiKey) n'est pas configurée dans les User Secrets.");
}

// 2. Ajouter le HttpClientFactory (bonne pratique)
builder.Services.AddHttpClient();

var app = builder.Build();

// 3. Notre Endpoint "Gardien" que N8N va appeler
app.MapGet("/api/weather/{city}", async (string city, IHttpClientFactory clientFactory) =>
{
    var httpClient = clientFactory.CreateClient();
    
    try
    {
        // 4. L'appel SÉCURISÉ : .NET appelle le service tiers avec la clé
        var apiUrl = $"[https://api.openweathermap.org/data/2.5/weather?q=](https://api.openweathermap.org/data/2.5/weather?q=){city}&appid={weatherApiKey}&units=metric&lang=fr";
        
        var response = await httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem("Erreur lors de l'appel au service météo.", statusCode: (int)response.StatusCode);
        }

        var weatherData = await response.Content.ReadAsStringAsync();
        
        // 5. On retourne les données brutes à N8N
        return Results.Content(weatherData, "application/json");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erreur interne du Gardien : {ex.Message}", statusCode: 500);
    }
});

app.Run();
```

#### Partie B : L'Orchestrateur (Workflow N8N)

Voici la configuration de chaque nœud visible dans la capture d'écran.

**1. Nœud : `Appel Api .NET pour la Météo` (`HTTP Request`)**
* **Method :** `GET`
* **URL :** `http://host.docker.internal:5123/api/weather/Paris`
    *(Utilisez `host.docker.internal` si N8N est sur Docker, ou `localhost` sinon. Adaptez le port.)*
* **Options (onglet) > Response :**
    * **Response Format :** `JSON`

**2. Nœud : `Transformation` (`Set`)**
* **Mode :** `JSON`
* **Contenu de l'éditeur JSON :**
    *(Nous créons un objet propre. La clé est dynamique et nous ne gardons que le "body" de la réponse.)*
    ```json
    {
      "Meteo{{ $json.body }}": {{ $json }}
    }
    ```
    *(Note : L'image d'aperçu du workflow montre "raw", mais c'est bien ce nœud `Set` qui effectue la transformation.)*

**3. Nœud : `Si la Température < 10°` (`IF`)**
* **Conditions > Add Condition > Number :**
* **Value 1 (Expression) :**
    * Cliquez sur l'icône "Expression" et entrez :
    * `{{ $json["MeteoParis"].main.temp }}`
    * *(Note : Si vous utilisez le `Set` dynamique, l'expression sera plus complexe. Pour un PoC, il est plus simple de nommer la clé "Meteo" en dur dans le nœud `Set`.)*
* **Operation :** `Smaller`
* **Value 2 :** `10`

### 5. Conclusion : Le ROI Stratégique

Ce PoC démontre une architecture mature :
* **Sécurité (ROI) :** Nos clés sensibles sont protégées dans .NET.
* **Maintenance (ROI) :** Si la clé API change, nous la mettons à jour à **UN SEUL ENDROIT** (`user-secrets` de notre API .NET). Aucun workflow N8N n'est impacté.
* **Notre Angle :** N8N est parfait pour le **Flow** (logique métier visuelle). .NET est essentiel pour le **Code** (sécurité, performance, abstraction).

---

> **Rejoignez la discussion !**
>
> Ce PoC vous a aidé ? Vous avez des questions ?
>
> Rejoignez notre communauté d'experts **"Hybrid CodeFlow AI"** sur Discord [Ajoutez votre lien d'invitation Discord permanent ici].
