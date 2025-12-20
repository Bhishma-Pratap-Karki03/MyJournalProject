let moodChart = null;
let wordCountChart = null;

export function createMoodChart(data) {
    const ctx = document.getElementById('moodChart');

    if (!ctx) return;

    if (moodChart) {
        moodChart.destroy();
    }

    const chartData = {
        labels: ['Positive', 'Neutral', 'Negative'],
        datasets: [{
            data: [data.positive, data.neutral, data.negative],
            backgroundColor: [
                '#22c55e', // Green for Positive
                '#94a3b8', // Grey for Neutral
                '#ef4444'  // Red for Negative
            ],
            borderWidth: 2,
            borderColor: '#ffffff',
            hoverOffset: 15
        }]
    };

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                position: 'right',
                labels: {
                    padding: 20,
                    usePointStyle: true,
                    pointStyle: 'circle',
                    pointRadius: 6,
                    font: {
                        size: 13,
                        family: "'Inter', sans-serif",
                        weight: '500'
                    },
                    color: '#334a5f',
                    generateLabels: function (chart) {
                        const data = chart.data;
                        if (data.labels.length && data.datasets.length) {
                            return data.labels.map((label, i) => {
                                const value = data.datasets[0].data[i];
                                return {
                                    text: `${label}: ${value}%`,
                                    fillStyle: data.datasets[0].backgroundColor[i],
                                    strokeStyle: '#ffffff',
                                    lineWidth: 2,
                                    hidden: false,
                                    index: i
                                };
                            });
                        }
                        return [];
                    }
                }
            },
            tooltip: {
                callbacks: {
                    label: function (context) {
                        return `${context.label}: ${context.raw}%`;
                    }
                },
                backgroundColor: '#ffffff',
                titleColor: '#334a5f',
                bodyColor: '#334a5f',
                borderColor: '#c9d5e0',
                borderWidth: 1
            }
        },
        layout: {
            padding: {
                top: 10,
                right: 20,
                bottom: 10,
                left: 10
            }
        }
    };

    moodChart = new Chart(ctx, {
        type: 'pie',
        data: chartData,
        options: options
    });
}

export function createWordCountChart(data) {
    const ctx = document.getElementById('wordCountChart');

    if (!ctx) return;

    if (wordCountChart) {
        wordCountChart.destroy();
    }

    const chartData = {
        labels: data.labels,
        datasets: [{
            label: 'Word Count',
            data: data.data,
            backgroundColor: 'rgba(0, 127, 232, 0.1)',
            borderColor: '#007fe8',
            borderWidth: 3,
            pointBackgroundColor: '#007fe8',
            pointBorderColor: '#ffffff',
            pointBorderWidth: 2,
            pointRadius: 6,
            pointHoverRadius: 8,
            tension: 0.3,
            fill: true
        }]
    };

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: false
            },
            tooltip: {
                mode: 'index',
                intersect: false,
                callbacks: {
                    label: function (context) {
                        return `Words: ${context.raw}`;
                    }
                },
                backgroundColor: '#ffffff',
                titleColor: '#334a5f',
                bodyColor: '#334a5f',
                borderColor: '#c9d5e0',
                borderWidth: 1
            }
        },
        scales: {
            x: {
                grid: {
                    display: false,
                    drawBorder: false
                },
                ticks: {
                    font: {
                        size: 12,
                        family: "'Inter', sans-serif",
                        weight: '500'
                    },
                    color: '#334a5f',
                    padding: 10
                }
            },
            y: {
                beginAtZero: true,
                grid: {
                    color: '#e2e8f0',
                    drawBorder: false
                },
                ticks: {
                    font: {
                        size: 11,
                        family: "'Inter', sans-serif"
                    },
                    color: '#64748b',
                    padding: 10,
                    callback: function (value) {
                        return value;
                    }
                }
            }
        },
        layout: {
            padding: {
                top: 10,
                right: 10,
                bottom: 10,
                left: 10
            }
        }
    };

    wordCountChart = new Chart(ctx, {
        type: 'line',
        data: chartData,
        options: options
    });
}

export function updateMoodChart(data) {
    if (moodChart) {
        moodChart.data.datasets[0].data = [data.positive, data.neutral, data.negative];
        moodChart.update();
    }
}

export function updateWordCountChart(data) {
    if (wordCountChart) {
        wordCountChart.data.labels = data.labels;
        wordCountChart.data.datasets[0].data = data.data;
        wordCountChart.update();
    }
}