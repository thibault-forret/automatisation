# Étape de construction : Utilisation de l'image SDK .NET 6 pour construire l'application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copier les fichiers .csproj et restaurer les dépendances du projet
COPY *.csproj ./
RUN dotnet restore

# Ajouter la dépendance MySql.Data avant de copier les fichiers source
RUN dotnet add package MySql.Data

# Copier tous les fichiers du projet
COPY . ./

# Publier l'application en mode Release dans le dossier 'out'
RUN dotnet publish -c Release -o out

# Étape d'exécution : Utilisation de l'image d'exécution ASP.NET Core pour .NET 6
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# Copier les fichiers publiés depuis l'étape de construction
COPY --from=build-env /app/out ./

# Exposer le port 6000
EXPOSE 6000

# Commande pour démarrer l'application ASP.NET Core sur le port 6000
ENTRYPOINT ["dotnet", "MonProjet.dll", "--urls", "http://0.0.0.0:6000"]
