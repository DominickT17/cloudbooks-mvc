(() => {
    const lienzo = document.getElementById('grafica-pedidos');
    if (!lienzo || typeof Chart === 'undefined') return;
    new Chart(lienzo, {
        type: 'bar',
        data: { labels: ['Pendientes', 'Aprobados', 'Enviados', 'Cancelados'], datasets: [{ label: 'Pedidos', data: [lienzo.dataset.pendientes, lienzo.dataset.aprobados, lienzo.dataset.enviados, lienzo.dataset.cancelados].map(Number), backgroundColor: ['#80bdf3', '#2188ef', '#285c8d', '#aab9c8'] }] },
        options: { maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } }
    });
})();
