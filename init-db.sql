CREATE TABLE IF NOT EXISTS calcul_results (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre INT NOT NULL,
    pair BOOLEAN NOT NULL,
    premier BOOLEAN NOT NULL,
    parfait BOOLEAN NOT NULL,
    created_at DATETIME NOT NULL
);
