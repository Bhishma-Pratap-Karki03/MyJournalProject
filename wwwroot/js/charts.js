// wwwroot/js/charts.js
let moodChart = null;
let wordCountChart = null;
let analyticsMoodChart = null;
let moodTrendChart = null;
let wordTrendChart = null;
let entriesVolumeChart = null;
let tagsBarChart = null;

// Analytics Mood Chart - for analytics page
export function createAnalyticsMoodChart(data) {
    const ctx = document.getElementById('moodDistributionChart');
    if (!ctx) return;

    // Destroy existing chart if it exists
    const existingChart = Chart.getChart(ctx);
    if (existingChart) {
        existingChart.destroy();
    }

    // Convert percentages to actual counts if needed
    let positive = data.positive || 0;
    let neutral = data.neutral || 0;
    let negative = data.negative || 0;

    // If data appears to be percentages, convert to counts for better display
    const total = positive + neutral + negative;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Positive', 'Neutral', 'Negative'],
            datasets: [{
                data: [positive, neutral, negative],
                backgroundColor: [
                    '#22c55e', // Green for Positive
                    '#94a3b8', // Grey for Neutral
                    '#ef4444'  // Red for Negative
                ],
                borderWidth: 2,
                borderColor: '#ffffff',
                hoverOffset: 15
            }]
        },
        options: {
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
                        color: '#334a5f'
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = total > 0 ? Math.round((context.raw / total) * 100) : 0;
                            return `${context.label}: ${context.raw} (${percentage}%)`;
                        }
                    }
                }
            },
            cutout: '60%'
        }
    });
}

// Dashboard Mood Chart - FIXED VERSION
export function createMoodChart(data) {
    const ctx = document.getElementById('moodChart');
    if (!ctx) return;

    if (moodChart) {
        moodChart.destroy();
    }

    // Store the original counts for use in labels
    const originalCounts = {
        positive: data.positive || 0,
        neutral: data.neutral || 0,
        negative: data.negative || 0
    };

    // Calculate percentages
    const total = originalCounts.positive + originalCounts.neutral + originalCounts.negative;
    const positivePercent = total > 0 ? Math.round((originalCounts.positive / total) * 100) : 0;
    const neutralPercent = total > 0 ? Math.round((originalCounts.neutral / total) * 100) : 0;
    const negativePercent = total > 0 ? Math.round((originalCounts.negative / total) * 100) : 0;

    const chartData = {
        labels: ['Positive', 'Neutral', 'Negative'],
        datasets: [{
            data: [positivePercent, neutralPercent, negativePercent],
            backgroundColor: [
                '#22c55e', // Green for Positive
                '#94a3b8', // Grey for Neutral
                '#ef4444'  // Red for Negative
            ],
            borderWidth: 2,
            borderColor: '#ffffff',
            hoverOffset: 15
        }],
        // Store original counts in the chart data for access in callbacks
        counts: [originalCounts.positive, originalCounts.neutral, originalCounts.negative]
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
                                const percentage = data.datasets[0].data[i];
                                const count = data.counts ? data.counts[i] : 0;
                                return {
                                    text: `${label}: ${percentage}% (${count})`,
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
                        const counts = context.chart.data.counts || [0, 0, 0];
                        const count = counts[context.dataIndex];
                        return `${context.label}: ${context.raw}% (${count} entries)`;
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

// Create custom line/bar chart
export function createCustomChart(canvasId, labels, data, label, color) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart if it exists
    const existingChart = Chart.getChart(ctx);
    if (existingChart) {
        existingChart.destroy();
    }

    const isLineChart = canvasId === 'moodTrendChart' || canvasId === 'wordTrendChart';

    new Chart(ctx, {
        type: isLineChart ? 'line' : 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: label,
                data: data,
                backgroundColor: isLineChart ? 'rgba(0, 127, 232, 0.1)' : color + '80',
                borderColor: color,
                borderWidth: isLineChart ? 3 : 1,
                pointBackgroundColor: color,
                pointBorderColor: '#ffffff',
                pointBorderWidth: 2,
                pointRadius: 6,
                pointHoverRadius: 8,
                tension: isLineChart ? 0.3 : 0,
                fill: isLineChart
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
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
                        padding: 10
                    }
                }
            }
        }
    });
}

// Clean up all charts
export function cleanupAllCharts() {
    if (moodChart) moodChart.destroy();
    if (wordCountChart) wordCountChart.destroy();
    if (analyticsMoodChart) analyticsMoodChart.destroy();
    if (moodTrendChart) moodTrendChart.destroy();
    if (wordTrendChart) wordTrendChart.destroy();
    if (entriesVolumeChart) entriesVolumeChart.destroy();
    if (tagsBarChart) tagsBarChart.destroy();
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

// Clean up function for analytics page
export function cleanupAnalyticsCharts() {
    const moodChart = Chart.getChart('moodDistributionChart');
    if (moodChart) moodChart.destroy();

    const moodTrendChart = Chart.getChart('moodTrendChart');
    if (moodTrendChart) moodTrendChart.destroy();

    const wordTrendChart = Chart.getChart('wordTrendChart');
    if (wordTrendChart) wordTrendChart.destroy();

    const entriesVolumeChart = Chart.getChart('entriesVolumeChart');
    if (entriesVolumeChart) entriesVolumeChart.destroy();

    const tagsBarChart = Chart.getChart('tagsBarChart');
    if (tagsBarChart) tagsBarChart.destroy();
}

// Fixed JavaScript function for file download
window.downloadFile = function (base64Data, fileName, contentType) {
    try {
        // Create a blob from the base64 data
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);

        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });

        // Create download link
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = url;
        a.download = fileName;

        document.body.appendChild(a);
        a.click();

        // Cleanup
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};