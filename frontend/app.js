document.addEventListener('DOMContentLoaded', function() {

    // Capture la soumission du formulaire
    document.getElementById('calcForm').addEventListener('submit', function(event) {

        document.getElementById('result').classList.remove('active'); 
        document.getElementById('error').classList.remove('active'); 
        
        event.preventDefault(); // Empêche la soumission normale du formulaire
        const number = document.getElementById('number').value; // Récupère la valeur du champ 'number'

        // Envoie la requête POST avec le nombre en JSON
        fetch('http://localhost:5000/calculate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ number: number }),
        })
        .then(response => response.json()) // Parse la réponse en JSON
        .then(data => { 
            // Affichage des résultats dans le tableau
            document.getElementById('result').classList.add('active');
            document.getElementById('result-number').innerText = data.result.number;
            document.getElementById('result-even').innerText = data.result.isEven ? 'Oui' : 'Non';
            document.getElementById('result-prime').innerText = data.result.isPrime ? 'Oui' : 'Non';
            document.getElementById('result-perfect').innerText = data.result.isPerfect ? 'Oui' : 'Non';
            document.getElementById('result-syracuse').innerText = data.result.syracuse;
            
        })
        .catch((error) => { // En cas d'erreur
            document.getElementById('error').classList.add('error'); 
            document.getElementById('error').textContent = 'Erreur : ' + error;
        });
    });
});
