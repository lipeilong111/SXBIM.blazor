window.renderBarChart = (function () {
    const charts = {};
    return function (elemId, data, titleText) {
        const el = document.getElementById(elemId);
        if (!el) return;

        const labels = (data || []).map(d => d.Date || d.date);
        const values = (data || []).map(d => (d.Count ?? d.count ?? 0));

        if (charts[elemId]) charts[elemId].destroy();

        charts[elemId] = new Chart(el.getContext('2d'), {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: titleText || '',
                    data: values,
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: { y: { beginAtZero: true } },
                plugins: {
                    legend: { position: 'top' },
                    datalabels: {
                        anchor: 'end',       // 标签位置
                        align: 'top',        // 文字对齐柱顶
                        color: '#111',       // 文字颜色
                        font: { weight: 'bold' },
                        formatter: function (value) {
                            return value;      // 显示数据值
                        }
                    }
                }
            },
            plugins: [ChartDataLabels]  // 开启插件
        });
    };
})();
