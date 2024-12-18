from flask import Flask, request, jsonify
from flask_cors import CORS
import requests

app = Flask(__name__)
CORS(app)  # Autorise toutes les origines pour simplifier le développement

@app.route('/calculate', methods=['POST'])
def calculate() :
    try:
        data = request.get_json()

        number = int(data['number'])

        # Pair / Impair
        pair = est_pair(number)

        # Premier
        premier = est_premier(number)

        # Parfait
        parfait = est_parfait(number)

        # Suite de Syracuse
        syracuse = suite_de_syracuse(number)

        # Envoi du résultat à l'API C#
        csharp_api_url = 'http://api-csharp:6000/api/save'
        payload = {
            'Nombre': number,
            'Pair': pair,
            'Premier': premier,
            'Parfait': parfait,
            'Syracuse': syracuse,
        }
        response = requests.post(csharp_api_url, json=payload)
        response.raise_for_status() 

        # Traitement de la réponse de l'API C#
        result = response.json()

        return jsonify({'result': result})
    except Exception as e:
        return jsonify({"error": e}), 500


def est_pair(nombre):
    """
    Détermine si un nombre est pair ou impair.
    Retourne True si le nombre est pair, False sinon.
    """
    return nombre % 2 == 0
    
def est_premier(nombre) :
    """
    Vérifie si un nombre est premier.
    Retourne True si le nombre est premier, False sinon.
    """
    if nombre <= 1:
        return False
    for i in range(2, int(nombre**0.5) + 1):
        if nombre % i == 0:
            return False
    return True

def est_parfait(nombre) :
    """
    Vérifie si un nombre est parfait.
    Un nombre parfait est un entier positif égal à la somme de ses diviseurs propres.
    Retourne True si le nombre est parfait, False sinon.
    """
    if nombre <= 0:
        return False
    somme_diviseurs = sum(i for i in range(1, nombre) if nombre % i == 0)
    return somme_diviseurs == nombre

def suite_de_syracuse(nombre) :
    """
    Calcule la suite de Syracuse pour un nombre donné.
    La suite de Syracuse est définie comme suit :
    - Si le nombre est pair, le prochain terme est la moitié du nombre.
    - Si le nombre est impair, le prochain terme est 3 fois le nombre plus 1.
    - La suite se termine lorsque le nombre atteint 1.
    """
    if nombre <= 0:
        raise ValueError("Le nombre doit être un entier strictement positif.")
    
    sequence = [nombre]
    while nombre != 1:
        if est_pair(nombre):
            nombre = nombre // 2
        else:
            nombre = 3 * nombre + 1
        sequence.append(nombre)
    return sequence

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0')
