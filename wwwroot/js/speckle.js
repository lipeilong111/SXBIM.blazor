window.initSpeckleMarquee = function () {
    const marquee = document.getElementById('logoMarquee');
    if (!marquee) return;

    const logos = [
        'ARUP', 'CUNDALL', 'Fast+Epp', 'FDB', 'ACPV ARCHITECTS',
        'Ramboll', 'Sasaki', 'SOM', 'Multiconsult', 'Perkins&Will'
    ];

    const content = [...logos, ...logos]; // 复制一份实现无缝滚动
    content.forEach(name => {
        const span = document.createElement('span');
        span.textContent = name;
        span.style.marginRight = '3rem';
        marquee.appendChild(span);
    });
};


window.scrollFadeInit = () => {
    const fadeItems = document.querySelectorAll(".plugin-section .title, .plugin-section .plugin-subtitle, .plugin-section .plugin-card");

    fadeItems.forEach(item => {
        item.style.opacity = "0";
        if (item.classList.contains("plugin-card")) {
            item.style.setProperty("--translateY", "0px"); // 卡片初始下移
        } else {
            item.style.transform = "translateY(40px)"; // 标题、副标题
        }
        item.style.transition = "opacity 0.8s ease, transform 0.8s ease";
    });

    function showFadeItems() {
        const windowHeight = window.innerHeight;
        let delayCounter = 0;

        fadeItems.forEach(item => {
            const rect = item.getBoundingClientRect();
            if (rect.top < windowHeight - 50 && item.style.opacity === "0") {
                item.style.transitionDelay = `${delayCounter * 0.2}s`;
                delayCounter++;

                item.style.opacity = "1";

                if (item.classList.contains("plugin-card")) {
                    // 使用CSS变量控制translate，不破坏scale
                    item.classList.add("visible");
                } else {
                    item.style.transform = "translateY(0)";
                }
            }
        });
    }

    window.addEventListener("scroll", showFadeItems);
    showFadeItems();
};
window.scrollFadeInit1 = () => {
    const fadeItems = document.querySelectorAll(
        ".bim-data-section h2, .bim-data-section h1, .bim-data-section .circle-flow-container"
    );

    // 初始状态：透明+下移
    fadeItems.forEach(item => {
        item.style.opacity = "0";
        item.style.transform = "translateY(40px)";
        item.style.transition = "opacity 1s ease, transform 1s ease";
    });

    function showFadeItems() {
        const windowHeight = window.innerHeight;
        let delayCounter = 0;

        fadeItems.forEach(item => {
            const rect = item.getBoundingClientRect();
            if (rect.top < windowHeight -300 && item.style.opacity === "0") {
                item.style.transitionDelay = `${delayCounter * 0.2}s`;
                delayCounter++;
                item.style.opacity = "1";
                item.style.transform = "translateY(0)";
            }
        });
    }

    window.addEventListener("scroll", showFadeItems);
    showFadeItems(); // 页面加载时立即执行一次
};
