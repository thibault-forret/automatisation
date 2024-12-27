from flask import Flask, request, jsonify
from flask_cors import CORS
import requests
import threading

app = Flask(__name__)

# Autoriser uniquement les requêtes provenant de localhost sur le port 80
CORS(app, resources={r"/*": {"origins": "http://localhost"}})

API_CSHARP_URL = "http://api-csharp:6000/api"
TIMEOUT = 30  # Timeout pour les calculs en secondes

@app.route('/calculate', methods=['POST'])
def calculate() :
    try:
        data = request.get_json()

        # Valider les données
        is_valid, result = validate_number_request(data, 1, 9223372036854775807)
        if not is_valid:
            return jsonify({"error": result}), 400
        
        number = result  # Récupérer le nombre validé

        # Vérifier si les informations sont déjà stockées
        result = verify_if_already_saved(number)

        # Si oui, renvoyer
        if result['found'] :
            return jsonify({'result': result['dto']})
        
        # Dictionnaire pour stocker le résultat ou l'erreur
        result_container = {}

        # Lancer le calcul dans un thread séparé
        thread = threading.Thread(target=calculate_data, args=(number, result_container))
        thread.start()

        # Attendre le résultat du calcul (avec timeout)
        thread.join(TIMEOUT)

        # Vérifier si le thread a terminé ou si un timeout est survenu
        if thread.is_alive():
            return jsonify({"error": "Le calcul a pris trop de temps."}), 500
        
        # Stocker les résultats
        save_payload = result_container['value'] 
        result = save_data(save_payload)

        return jsonify({'result': result['dto']})
    except Exception as e:
        print(e)
        return jsonify({"error": str(e)}), 500


def validate_number_request(data, min_value=None, max_value=None):
    """
    Valide les données JSON envoyées pour le champ 'number'.
    Retourne un tuple (booléen, message ou valeur).
    """
    # Vérifie si les données JSON sont valides (non nulles)
    if data is None:
        return False, "Aucune donnée JSON valide dans la requête."
    
    # Vérifie si la clé 'number' est présente dans les données
    if 'number' not in data:
        return False, "Le champ 'number' est requis."
    
    # Récupère la valeur associée à 'number'
    number = data['number']
    
    # Vérifie si la valeur de 'number' n'est pas vide
    if not number:
        return False, "Le champ 'number' ne peut pas être vide."
    
    # Tente de convertir la valeur en entier
    try:
        number = int(number)
    except ValueError:
        return False, "Le champ 'number' doit être un entier valide."
    
    # Vérification des bornes (si spécifiées)
    if min_value is not None and number < min_value:
        return False, f"Le champ 'number' doit être supérieur ou égal à {min_value}."
    if max_value is not None and number > max_value:
        return False, f"Le champ 'number' doit être inférieur ou égal à {max_value}."
    
    return True, number

def verify_if_already_saved(number):
    """
    Appelle l'API C# pour vérifier si le nombre est déjà stocké.
    Retourne la réponse de l'API sous forme de dictionnaire.
    """
    try :
        verif_url = f"{API_CSHARP_URL}/verif"
        response = requests.post(verif_url, json=number, timeout=TIMEOUT)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.Timeout:
        raise Exception("La vérification du calcul à pris trop de temps.")

def save_data(payload):
    """
    Appelle l'API C# pour sauvegarder les informations calculées.
    Retourne la réponse de l'API sous forme de dictionnaire.
    """
    try :
        save_url = f"{API_CSHARP_URL}/save"
        response = requests.post(save_url, json=payload, timeout=TIMEOUT)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.Timeout:
        raise Exception("L'enregistrement du calcul à pris trop de temps.")

def calculate_data(number, result_container):
    """
    Effectue les calculs requis pour le nombre donné et met les 
    résultats dans le dictionnaire result_container.
    """
    try:
        is_even = is_even_number(number)
        is_prime = is_prime_number(number)
        is_perfect = is_perfect_number(number)
        syracuse_sequence = syracuse_sequence_calculator(number)

        result_container['value'] = {
            'Number': number,
            'IsEven': is_even,
            'IsPrime': is_prime,
            'IsPerfect': is_perfect,
            'Syracuse': list(map(str, syracuse_sequence)),
        }
    except Exception as e:
        raise Exception(f"Une erreur est survenue lors des calculs: {str(e)}")

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
