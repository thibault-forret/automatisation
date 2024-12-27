document.addEventListener('DOMContentLoaded', function () {
    // Fonction pour gérer l'affichage ou la suppression des classes 'active'
    function toggleClass(element, className, shouldAdd) {
        element.classList[shouldAdd ? 'add' : 'remove'](className);
    }

    // Récupération des éléments DOM importants
    const calcForm = document.getElementById('calcForm');
    const numberElement = document.getElementById('number');
    const resultElement = document.getElementById('result');
    const errorElement = document.getElementById('error');
    const waitResultElement = document.getElementById('wait-result');
    const resultNumber = document.getElementById('result-number');
    const resultEven = document.getElementById('result-even');
    const resultPrime = document.getElementById('result-prime');
    const resultPerfect = document.getElementById('result-perfect');
    const resultSyracuse = document.getElementById('result-syracuse');

    // Gestion de la soumission du formulaire
    calcForm.addEventListener('submit', function (event) {
        event.preventDefault(); // Empêche la soumission par défaut

        // Réinitialisation des messages d'erreur et des résultats
        toggleClass(resultElement, 'active', false);
        toggleClass(errorElement, 'active', false);

        // Vérification de la validité du champ 'number'
        if (!numberElement || !numberElement.value.trim()) {
            toggleClass(errorElement, 'active', true);
            errorElement.textContent = "Error: Le champ 'number' est requis.";
            return;
        }

        // Affichage d'un message d'attente
        toggleClass(waitResultElement, 'active', true);
        waitResultElement.textContent = "Calcul en cours...";

        const number = numberElement.value.trim();

        // Envoi de la requête POST pour le calcul
        fetch('http://localhost:5000/calculate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ number: number }),
        })
            .then((response) => {
                // Gestion des erreurs côté serveur
                if (!response.ok) {
                    return response.json().then((err) => {
                        throw new Error(err.error);
                    });
                }
                return response.json(); // Parse la réponse JSON
            })
            .then((data) => {
                // Affichage des résultats
                toggleClass(resultElement, 'active', true);
                resultNumber.innerText = data.result.number;
                resultEven.innerText = data.result.isEven ? 'Oui' : 'Non';
                resultPrime.innerText = data.result.isPrime ? 'Oui' : 'Non';
                resultPerfect.innerText = data.result.isPerfect ? 'Oui' : 'Non';
                resultSyracuse.innerText = data.result.syracuse;

                // Suppression du message d'attente
                toggleClass(waitResultElement, 'active', false);
            })
            .catch((error) => {
                // Gestion des erreurs de requête
                toggleClass(errorElement, 'active', true);
                errorElement.textContent = `Erreur: ${error.message}`;
                toggleClass(waitResultElement, 'active', false);
            });
    });
});
