// -----------------------------------------------------------------------------
// donutchart.js
// JavaScript module used by the DonutChart component to attach click handlers
// to MudBlazor donut chart slices and invoke .NET callbacks.
// -----------------------------------------------------------------------------

/**
 * Registers click handlers for donut chart slices and forwards the index
 * of the clicked slice to the .NET component via JS interop.
 *
 * @param {string} chartId - The DOM element ID of the donut chart container.
 */
export function registerSliceClicks(chartId) {
    const root = document.getElementById(chartId);
    if (!root) return;

    // MudBlazor renders donut slices as <path class="mud-chart-donut-slice">
    const slices = root.querySelectorAll(".mud-chart-donut-slice");

    slices.forEach((slice, index) => {
        slice.addEventListener("click", () => {
            DotNet.invokeMethodAsync("DonutChartSolution", "SliceClicked", index);
        });
    });
}
