# Utilise une image de base Python
FROM python:3.10

# Définir le répertoire de travail
WORKDIR /app

# Copier les fichiers nécessaires
COPY requirements.txt ./
RUN pip install -r requirements.txt

COPY . .

# Expose le port de Flask (par défaut 5000)
EXPOSE 5000

# Commande pour démarrer l'application Flask
CMD ["python", "app.py"]
