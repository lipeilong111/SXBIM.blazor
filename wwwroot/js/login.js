

window.checkLoginStatus = function () {
    const isLoggedIn = sessionStorage.getItem("isLoggedIn");
    if (isLoggedIn !== "true") {
        window.location.href = "/login"; // 未登录强制跳转
    }
};






async function downloadMultipleFiles() {
    const ghVersion = await fetch('/api/version/rhino-gh').then(r => r.json());
    const rhVersion = await fetch('/api/version/rhino-rh').then(r => r.json());

    const files = [
        `/Protected/SX_Snail_RH_V${rhVersion.version}.rhp`,
        `/Protected/SX_Snail_GH_V${ghVersion.version}.gha`
    ];

    downloadFiles(files);
}

async function downloadCAD() {
    const cadVersion = await fetch('/api/version/cad').then(r => r.json());
    const files = [`/Protected/SX_CAD_V${cadVersion.version}.dll`];

    downloadFiles(files);
}

async function downloadRevit() {
    const revitVersion = await fetch('/api/version/revit').then(r => r.json());
    const files = [`/Protected/SX_Revit_V${revitVersion.version}.7z`];

    downloadFiles(files);
}

function downloadFiles(files) {
    for (let i = 0; i < files.length; i++) {
        setTimeout(() => {
            const a = document.createElement('a');
            a.href = files[i];
            a.download = '';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        }, i * 500);
    }
}

window.getPluginVersion = async function (plugin) {
    const map = {
        cad: "/api/version/cad",
        rhino: "/api/version/rhino-gh",
        revit: "/api/version/revit"
    };
    try {
        const res = await fetch(map[plugin]);
        const data = await res.json();
        return data.version;
    } catch (err) {
        console.error("❌ 获取版本失败:", plugin);
        return "未知";
    }
}
