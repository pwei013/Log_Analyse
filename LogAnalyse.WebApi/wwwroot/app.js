const state = {
    summaryItems: [],
    currentFolderName: "",
    currentMethod: "",
    pageNumber: 1,
    pageSize: 10, // 默认改为10条，方便展示
    totalCount: 0
};

const elements = {
    logsRootPath: document.getElementById("logsRootPath"),
    thresholdMs: document.getElementById("thresholdMs"),
    startTime: document.getElementById("startTime"),
    endTime: document.getElementById("endTime"),
    btnQuerySummary: document.getElementById("btnQuerySummary"),
    summaryBody: document.getElementById("summaryBody"),
    detailBody: document.getElementById("detailBody"),
    summaryStats: document.getElementById("summaryStats"),
    detailStats: document.getElementById("detailStats"),
    btnPrevPage: document.getElementById("btnPrevPage"),
    btnNextPage: document.getElementById("btnNextPage"),
    pageInfo: document.getElementById("pageInfo"),
    
    // 新增元素
    statScannedFiles: document.getElementById("statScannedFiles"),
    statTotalLogs: document.getElementById("statTotalLogs"),
    statTimeoutLogs: document.getElementById("statTimeoutLogs"),
    statErrorLogs: document.getElementById("statErrorLogs"),
    statMaxTime: document.getElementById("statMaxTime"),
    chartSection: document.getElementById("chartSection"),
    detailSection: document.getElementById("detailSection"),
    summaryBadge: document.getElementById("summaryBadge"),
    detailContextBadge: document.getElementById("detailContextBadge"),
    summarySkeleton: document.getElementById("summarySkeleton")
};

let chartInstance = null;
let timeTrendChartInstance = null;

// 初始化 ECharts
function initChart() {
    if (!chartInstance) {
        const chartDom = document.getElementById('timeoutChart');
        if (chartDom) {
            chartInstance = echarts.init(chartDom);
            window.addEventListener('resize', () => chartInstance.resize());
        }
    }
    if (!timeTrendChartInstance) {
        const trendDom = document.getElementById('timeTrendChart');
        if (trendDom) {
            timeTrendChartInstance = echarts.init(trendDom);
            window.addEventListener('resize', () => timeTrendChartInstance.resize());
        }
    }
}

// 格式化 JSON
function formatJson(jsonStr) {
    if (!jsonStr) return "";
    try {
        const obj = JSON.parse(jsonStr);
        return JSON.stringify(obj, null, 2);
    } catch (e) {
        return jsonStr;
    }
}

// Toast 提示
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    const bgColor = type === 'error' ? 'bg-rose-500' : (type === 'success' ? 'bg-emerald-500' : 'bg-slate-800');
    
    toast.className = `${bgColor} text-white px-4 py-3 rounded-lg shadow-lg text-sm flex items-center gap-2 toast-enter`;
    
    let icon = '';
    if (type === 'error') {
        icon = `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`;
    } else if (type === 'success') {
        icon = `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`;
    } else {
        icon = `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`;
    }

    toast.innerHTML = `
        ${icon}
        <span>${message}</span>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.remove('toast-enter');
        toast.classList.add('toast-exit');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// 页面初始化
document.addEventListener('DOMContentLoaded', async () => {
    // 页面加载完成后的初始化逻辑（如需要）
});

elements.btnQuerySummary.addEventListener("click", async () => {
    state.pageNumber = 1;
    await querySummary();
});

elements.btnPrevPage.addEventListener("click", async () => {
    if (state.pageNumber <= 1) {
        return;
    }

    state.pageNumber -= 1;
    await queryDetails();
});

elements.btnNextPage.addEventListener("click", async () => {
    const totalPages = Math.ceil(state.totalCount / state.pageSize);
    if (state.pageNumber >= totalPages) {
        return;
    }

    state.pageNumber += 1;
    await queryDetails();
});

async function querySummary() {
    toggleSummaryLoading(true);
    elements.detailSection.classList.add('hidden');
    
    const body = buildBaseRequest();
    const response = await postJson("/LogAnalysis/GetSummary", body);
    
    if (!response) {
        toggleSummaryLoading(false);
        return;
    }

    if (response.resultCode !== "200") {
        showToast(response.resultMsg || "查询失败", 'error');
        toggleSummaryLoading(false);
        return;
    }

    state.summaryItems = response.items || [];
    const timeDistribution = response.timeDistribution || [];
    
    // 更新概览卡片
    elements.statScannedFiles.textContent = response.scannedFileCount;
    elements.statTotalLogs.textContent = response.totalLogCount;
    elements.statTimeoutLogs.textContent = response.timeoutLogCount;
    elements.statErrorLogs.textContent = response.errorLogCount;
    elements.summaryBadge.textContent = `共 ${state.summaryItems.length} 个接口`;
    
    // 计算最高耗时
    const maxTime = state.summaryItems.length > 0 
        ? Math.max(...state.summaryItems.map(x => x.maxUsedTimeMs)) 
        : 0;
    elements.statMaxTime.textContent = maxTime > 0 ? toNumber(maxTime) : "-";
    
    // 渲染图表和列表
    renderChart(state.summaryItems, timeDistribution);
    renderSummary(state.summaryItems);
    
    showToast(`扫描完成，发现 ${response.timeoutLogCount} 条超时日志`, 'success');
    toggleSummaryLoading(false);
}

function renderChart(items, timeDistribution) {
    if ((!items || items.length === 0) && (!timeDistribution || timeDistribution.length === 0)) {
        elements.chartSection.classList.add('hidden');
        return;
    }

    elements.chartSection.classList.remove('hidden');
    initChart();

    // 1. 渲染 Top 10 接口分布图
    if (items && items.length > 0) {
        const topItems = items.slice(0, 10); // 取前 10 个
        
        const option = {
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'shadow' }
            },
            grid: {
                left: '3%',
                right: '4%',
                bottom: '3%',
                top: '15%',
                containLabel: true
            },
            xAxis: {
                type: 'value',
                name: '超时次数',
                minInterval: 1
            },
            yAxis: {
                type: 'category',
                data: topItems.map(item => item.method.length > 15 ? item.method.substring(0, 15) + '...' : item.method).reverse(),
                axisLabel: {
                    interval: 0
                }
            },
            series: [
                {
                    name: '超时次数',
                    type: 'bar',
                    data: topItems.map(item => item.timeoutCount).reverse(),
                    itemStyle: {
                        color: new echarts.graphic.LinearGradient(1, 0, 0, 0, [
                            { offset: 0, color: '#3b82f6' },
                            { offset: 1, color: '#93c5fd' }
                        ]),
                        borderRadius: [0, 4, 4, 0]
                    },
                    label: {
                        show: true,
                        position: 'right'
                    }
                }
            ]
        };

        chartInstance.setOption(option);
    }

    // 2. 渲染时间趋势图 (面积图)
    if (timeDistribution && timeDistribution.length > 0) {
        const trendOption = {
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'cross' }
            },
            grid: {
                left: '3%',
                right: '4%',
                bottom: '3%',
                top: '15%',
                containLabel: true
            },
            xAxis: {
                type: 'category',
                boundaryGap: false,
                data: timeDistribution.map(t => {
                    // 仅显示时分，让 x 轴更简洁
                    const parts = t.time.split(' ');
                    return parts.length > 1 ? parts[1] : t.time;
                })
            },
            yAxis: {
                type: 'value',
                name: '慢接口数',
                minInterval: 1
            },
            series: [
                {
                    name: '出现次数',
                    type: 'line',
                    smooth: true,
                    areaStyle: {
                        color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
                            { offset: 0, color: 'rgba(59, 130, 246, 0.5)' },
                            { offset: 1, color: 'rgba(59, 130, 246, 0.0)' }
                        ])
                    },
                    itemStyle: {
                        color: '#3b82f6'
                    },
                    data: timeDistribution.map(t => t.count)
                }
            ]
        };
        timeTrendChartInstance.setOption(trendOption);
    }
}

async function queryDetails() {
    if (!state.currentFolderName || !state.currentMethod) {
        return;
    }

    toggleDetailLoading(true);
    const body = {
        ...buildBaseRequest(),
        folderName: state.currentFolderName,
        method: state.currentMethod,
        pageNumber: state.pageNumber,
        pageSize: state.pageSize
    };

    const response = await postJson("/LogAnalysis/GetDetails", body);
    if (!response) {
        toggleDetailLoading(false);
        return;
    }

    if (response.resultCode !== "200") {
        showToast(response.resultMsg || "获取明细失败", 'error');
        toggleDetailLoading(false);
        return;
    }

    state.totalCount = response.totalCount || 0;
    
    elements.detailSection.classList.remove('hidden');
    elements.detailContextBadge.textContent = `${state.currentFolderName} / ${state.currentMethod}`;
    
    renderDetail(response.items || []);
    
    const totalPages = Math.max(1, Math.ceil(state.totalCount / state.pageSize));
    elements.pageInfo.textContent = `第 ${state.pageNumber} / ${totalPages} 页`;
    elements.detailStats.textContent = `共 ${state.totalCount} 条记录`;
    
    elements.btnPrevPage.disabled = state.pageNumber <= 1;
    elements.btnNextPage.disabled = state.pageNumber >= totalPages;
    
    // 滚动到详情区
    elements.detailSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    
    toggleDetailLoading(false);
}

function renderSummary(items) {
    elements.summaryBody.innerHTML = "";
    
    if (items.length === 0) {
        elements.summaryBody.innerHTML = `
            <tr>
                <td colspan="6" class="px-6 py-8 text-center text-slate-500 bg-slate-50/50">
                    <div class="flex flex-col items-center justify-center">
                        <svg class="w-12 h-12 text-slate-300 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                        <p>太棒了！没有发现超时日志</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        const tr = document.createElement("tr");
        tr.className = "hover:bg-slate-50 transition-colors fade-in";
        tr.style.animationDelay = `${i * 0.05}s`;
        
        tr.innerHTML = `
            <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-slate-100 text-slate-800">
                    ${escapeHtml(item.folderName)}
                </span>
            </td>
            <td class="px-6 py-4">
                <span class="font-mono text-xs text-slate-700 bg-slate-100 px-2 py-1 rounded">${escapeHtml(item.method)}</span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
                <button class="count-link group flex items-center gap-1 text-blue-600 font-semibold hover:text-blue-800 transition-colors bg-blue-50 px-3 py-1 rounded-full border border-blue-100 hover:border-blue-300">
                    <span>${item.timeoutCount}</span>
                    <svg class="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path></svg>
                </button>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${item.errorCount > 0 ? 'bg-rose-100 text-rose-800' : 'bg-emerald-100 text-emerald-800'}">
                    ${item.errorCount}
                </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-slate-600">${toNumber(item.averageUsedTimeMs)}</td>
            <td class="px-6 py-4 whitespace-nowrap font-medium text-rose-600">${toNumber(item.maxUsedTimeMs)}</td>
            <td class="px-6 py-4 whitespace-nowrap text-slate-500 text-xs">${toDateTime(item.lastCallTime)}</td>
        `;
        
        tr.querySelector(".count-link").addEventListener("click", async () => {
            state.currentFolderName = item.folderName;
            state.currentMethod = item.method;
            state.pageNumber = 1;
            await queryDetails();
        });
        
        elements.summaryBody.appendChild(tr);
    }
}

function renderDetail(items) {
    elements.detailBody.innerHTML = "";
    
    if (items.length === 0) {
        elements.detailBody.innerHTML = `
            <tr>
                <td colspan="5" class="px-6 py-8 text-center text-slate-500">没有找到明细数据</td>
            </tr>`;
        return;
    }

    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        const tr = document.createElement("tr");
        tr.className = "hover:bg-slate-50 transition-colors fade-in";
        tr.style.animationDelay = `${i * 0.05}s`;
        
        const formattedReq = formatJson(item.requestPayload);
        const formattedRes = formatJson(item.responsePayload);
        
        tr.innerHTML = `
            <td class="px-6 py-4 whitespace-nowrap text-xs text-slate-600">
                <div class="flex items-center gap-1">
                    <svg class="w-3 h-3 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                    ${toDateTime(item.callTime)}
                </div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap font-medium text-rose-600">
                ${toNumber(item.usedTimeMs)}
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${item.resultCode && item.resultCode !== '200' ? 'bg-rose-100 text-rose-800' : 'bg-emerald-100 text-emerald-800'}">
                    ${escapeHtml(item.resultCode || '-')}
                </span>
            </td>
            <td class="px-6 py-4 text-xs text-slate-500 break-all">
                <div class="flex flex-col gap-1">
                    <span class="bg-slate-100 px-1.5 py-0.5 rounded inline-block w-fit">${escapeHtml(item.filePath.split('\\').pop() || item.filePath.split('/').pop())}</span>
                    <span class="text-slate-400">Line: ${item.lineNumber}</span>
                </div>
            </td>
            <td class="px-6 py-4 json-cell">
                <pre><code class="language-json">${escapeHtml(formattedReq)}</code></pre>
            </td>
            <td class="px-6 py-4 json-cell">
                <pre><code class="language-json">${escapeHtml(formattedRes)}</code></pre>
            </td>
        `;
        elements.detailBody.appendChild(tr);
    }
    
    // 触发代码高亮
    document.querySelectorAll('pre code').forEach((block) => {
        hljs.highlightElement(block);
    });
}

function toggleSummaryLoading(loading) {
    const btn = elements.btnQuerySummary;
    btn.disabled = loading;
    
    if (loading) {
        btn.innerHTML = `
            <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            查询中...
        `;
        elements.summarySkeleton.classList.remove('hidden');
        elements.summaryBody.innerHTML = '';
    } else {
        btn.innerHTML = `
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
            <span>查询统计</span>
        `;
        elements.summarySkeleton.classList.add('hidden');
    }
}

function toggleDetailLoading(loading) {
    elements.btnPrevPage.disabled = loading || state.pageNumber <= 1;
    // Next button state is handled after data load
    
    if (loading) {
        elements.detailBody.innerHTML = `
            <tr>
                <td colspan="5" class="px-6 py-12 text-center">
                    <div class="flex flex-col items-center justify-center text-slate-400">
                        <svg class="animate-spin h-8 w-8 text-blue-500 mb-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        <span>加载明细中...</span>
                    </div>
                </td>
            </tr>`;
    }
}

function buildBaseRequest() {
    const startTime = elements.startTime.value ? new Date(elements.startTime.value).toISOString() : null;
    const endTime = elements.endTime.value ? new Date(elements.endTime.value).toISOString() : null;
    return {
        logsRootPath: elements.logsRootPath.value.trim() || null,
        thresholdMs: Number(elements.thresholdMs.value || 0),
        startTime,
        endTime
    };
}

async function postJson(url, body) {
    try {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(body)
        });
        return await response.json();
    } catch (error) {
        console.error(error);
        showToast(`网络请求失败`, 'error');
        return null;
    }
}

function toNumber(value) {
    const number = Number(value || 0);
    return Number.isFinite(number) ? number.toFixed(2) : "0.00";
}

function toDateTime(value) {
    if (!value) {
        return "-";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    // 格式化为 YYYY-MM-DD HH:mm:ss
    const pad = (n) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

function escapeHtml(value) {
    if (!value) return "";
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}
