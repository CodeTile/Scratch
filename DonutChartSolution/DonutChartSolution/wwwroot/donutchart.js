window.donutChart = {
    registerSliceClicks: function (containerId) {

        const container = document.getElementById(containerId);
        if (!container) return;

        const svg = container.querySelector("svg");
        if (!svg) return;

        const paths = svg.querySelectorAll("path");

        paths.forEach((p, index) => {
            p.addEventListener("click", () => {
                DotNet.invokeMethodAsync("DonutChartSolution", "SliceClicked", index);
            });
        });
    }
};
