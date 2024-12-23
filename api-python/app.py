from flask import Flask, request, jsonify
from flask_cors import CORS
import requests

# Voir pour CORS(app) -> autoriser seulement depuis frontend ?

app = Flask(__name__)
CORS(app)  # Autorise toutes les origines pour simplifier le développement

API_CSHARP_URL = "http://api-csharp:6000/api"

@app.route('/calculate', methods=['POST'])
def calculate() :
    try:
        data = request.get_json()

        number = int(data['number'])

        # Vérifier si les informations sont déjà stockées
        result = verify_if_already_saved(number)

        # Si oui, renvoyer
        if result['found'] :
            return jsonify({'result': result['dto']})
        
        # Effectuer les calculs
        save_payload = calculate_data(number)

        # Stocker les informations
        result = save_data(save_payload)

        return jsonify({'result': result['dto']})
    except Exception as e:
        return jsonify({"error": e}), 500

def verify_if_already_saved(number):
    """
    Appelle l'API C# pour vérifier si le nombre est déjà stocké.
    Retourne la réponse de l'API sous forme de dictionnaire.
    """
    verif_url = f"{API_CSHARP_URL}/verif"
    response = requests.post(verif_url, json=number)
    response.raise_for_status()
    return response.json()

def save_data(payload):
    """
    Appelle l'API C# pour sauvegarder les informations calculées.
    Retourne la réponse de l'API sous forme de dictionnaire.
    """
    save_url = f"{API_CSHARP_URL}/save"
    response = requests.post(save_url, json=payload)
    response.raise_for_status()
    return response.json()

def calculate_data(number):
    """
    Effectue les calculs requis pour le nombre donné.
    Retourne un dictionnaire contenant les résultats.
    """
    is_even = is_even_number(number)
    is_prime = is_prime_number(number)
    is_perfect = is_perfect_number(number)
    syracuse_sequence = syracuse_sequence_calculator(number)

    return {
        'Number': number,
        'IsEven': is_even,
        'IsPrime': is_prime,
        'IsPerfect': is_perfect,
        'Syracuse': syracuse_sequence,
    }

def is_even_number(number):
    """
    Détermine si un nombre est pair ou impair.
    Retourne True si le nombre est pair, False sinon.
    """
    return number % 2 == 0
    
def is_prime_number(number) :
    """
    Vérifie si un nombre est premier.
    Retourne True si le nombre est premier, False sinon.
    """
    if number <= 1:
        return False
    for i in range(2, int(number**0.5) + 1):
        if number % i == 0:
            return False
    return True

def is_perfect_number(number) :
    """
    Vérifie si un nombre est parfait.
    Un nombre parfait est un entier positif égal à la somme de ses diviseurs propres.
    Retourne True si le nombre est parfait, False sinon.
    """
    if number <= 0:
        return False
    divisor_sum = sum(i for i in range(1, number) if number % i == 0)
    return divisor_sum == number

def syracuse_sequence_calculator(number) :
    """
    Calcule la suite de Syracuse pour un nombre donné.
    La suite de Syracuse est définie comme suit :
    - Si le nombre est pair, le prochain terme est la moitié du nombre.
    - Si le nombre est impair, le prochain terme est 3 fois le nombre plus 1.
    - La suite se termine lorsque le nombre atteint 1.
    """
    if number <= 0:
        raise ValueError("Le nombre doit être un entier strictement positif.")
    
    sequence = [number]
    while number != 1:
        if is_even_number(number):
            number = number // 2
        else:
            number = 3 * number + 1
        sequence.append(number)
    return sequence

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0')
