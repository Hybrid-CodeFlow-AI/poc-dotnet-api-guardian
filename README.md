# PocApiGuardian

API minimale .NET 8 agissant comme un gardien d'API pour OpenWeatherMap.

## Vue d'ensemble
- Nom du projet : `PocApiGuardian`
- Endpoint unique : `GET /api/weather/{city}`
- Valide la clé API au démarrage (depuis la configuration `WeatherApi:ApiKey`). Le programme lève une `InvalidOperationException` si la clé est manquante.
- Appelle l'API OpenWeatherMap et renvoie les réponses JSON brutes (type de contenu `application/json`).

## Comment exécuter
1. Ajoutez votre clé OpenWeatherMap dans `appsettings.json` :

```json
{
  "WeatherApi": {
    "ApiKey": "VOTRE_CLE_OPENWEATHERMAP_ICI"
  }
}
```

2. Démarrez l'application :

```powershell
cd PocApiGuardian
dotnet run
```

3. Exemple de requête :

```powershell
curl http://localhost:5000/api/weather/Casablanca
```

Remarque : les ports par défaut utilisés par le runtime peuvent s'afficher dans la sortie de la console.

