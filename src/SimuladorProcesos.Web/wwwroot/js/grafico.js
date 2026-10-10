// Módulo ES para el gráfico Proceso vs TurnaRound; usa el global Chart del UMD local.
const graficos = new Map();

// Dibuja (o redibuja) la línea Proceso vs TAT en el canvas indicado.
export function dibujar(canvasId, etiquetas, valores) {
    const lienzo = document.getElementById(canvasId);
    if (!lienzo) {
        return;
    }
    destruir(canvasId);
    const grafico = new Chart(lienzo, {
        type: "line",
        data: {
            labels: etiquetas,
            datasets: [
                {
                    label: "TurnaRound (ms)",
                    data: valores,
                    borderColor: "#2563eb",
                    backgroundColor: "#2563eb",
                    pointRadius: 4,
                    tension: 0,
                    fill: false,
                },
            ],
        },
        options: {
            responsive: true,
            plugins: {
                title: { display: true, text: "Proceso vs TurnaRound" },
                legend: { display: true },
            },
            scales: {
                x: { title: { display: true, text: "Proceso" }, grid: { display: false } },
                y: { title: { display: true, text: "TAT (ms)" }, beginAtZero: true, grid: { display: true } },
            },
        },
    });
    graficos.set(canvasId, grafico);
}

// Destruye el gráfico del canvas para liberar memoria antes de redibujar o salir.
export function destruir(canvasId) {
    const grafico = graficos.get(canvasId);
    if (grafico) {
        grafico.destroy();
        graficos.delete(canvasId);
    }
}
