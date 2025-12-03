const content = document.getElementById("content-inner");
const env = document.querySelector('meta[name="environment"]')?.content || "DEVELOPMENT";
const envLabel = document.getElementById("env-label");
const envStrip = document.getElementById("env-strip");

if (env.toLowerCase() !== "production") {
    envLabel.textContent = env.toUpperCase();
    envStrip.style.background = env.toLowerCase() === "dev" || env.toLowerCase() === "development" ? "black" : "orange";
} else {
    envLabel.textContent = "";
    envStrip.style.display = "none";
}

// Light/dark toggle
document.getElementById("themeToggle").addEventListener("click", () => {
    const current = document.documentElement.getAttribute("data-theme");
    document.documentElement.setAttribute("data-theme", current === "dark" ? "light" : "dark");
});

// Sidebar resize
(function () {
    const sidebar = document.getElementById("sidebar");
    const bar = document.getElementById("resize-bar");
    let dragging = false;
    bar.addEventListener("mousedown", () => dragging = true);
    window.addEventListener("mouseup", () => dragging = false);
    window.addEventListener("mousemove", (e) => {
        if (!dragging) return;
        sidebar.style.width = e.clientX + "px";
    });
})();

// Load OpenAPI
let openapi = null;
let rawSpec = "";
async function loadSpec() {
    const res = await fetch("./openapi.json");
    rawSpec = await res.text();
    try { openapi = JSON.parse(rawSpec); } catch { openapi = null; }
    if (openapi) buildControllerMenu(openapi);
}
loadSpec();

// Build left menu accordions
function buildControllerMenu(spec) {
    const container = document.getElementById("controllers-container");
    container.innerHTML = "";
    const groups = {};
    for (const [path, ops] of Object.entries(spec.paths)) {
        for (const [verb, op] of Object.entries(ops)) {
            const tag = op.tags ? op.tags[0] : "Default";
            groups[tag] ??= [];
            groups[tag].push({ path, verb: verb.toUpperCase(), op });
        }
    }
    for (const [ctrl, endpoints] of Object.entries(groups)) {
        const ctrlDiv = document.createElement("div");
        ctrlDiv.className = "controller";
        const header = document.createElement("div");
        header.className = "controller-header";
        header.textContent = ctrl;
        const contentDiv = document.createElement("div");
        contentDiv.className = "controller-content";

        endpoints.forEach(e => {
            const row = document.createElement("div");
            row.className = "method";
            row.innerHTML = `<span>${e.path}</span><span class="method-verb ${e.verb}">${e.verb}</span>`;
            row.addEventListener("click", () => showEndpoint(e));
            contentDiv.appendChild(row);
        });

        header.addEventListener("click", () => contentDiv.style.display = contentDiv.style.display === "block" ? "none" : "block");
        ctrlDiv.appendChild(header);
        ctrlDiv.appendChild(contentDiv);
        container.appendChild(ctrlDiv);
    }
}

// Show endpoint content in Scalar style
function showEndpoint(e) {
    const op = e.op;
    content.innerHTML = `
      <div class="endpoint-detail">
        <div class="endpoint-header">
          <span class="method-verb ${e.verb}">${e.verb}</span>
          <span class="endpoint-path">${e.path}</span>
        </div>
        ${op.summary ? `<h3 class="endpoint-summary">${op.summary}</h3>` : ""}
        ${op.description ? `<p class="endpoint-desc">${op.description}</p>` : ""}
        <div class="section"><h4>Parameters</h4>${renderParameters(op.parameters || [])}</div>
        <div class="section"><h4>Request Body</h4>${renderRequestBody(op.requestBody)}</div>
        <div class="section"><h4>Responses</h4>${renderResponses(op.responses)}</div>
      </div>
      <div class="raw-spec"><h3>Raw Spec</h3><pre>${JSON.stringify(op, null, 2)}</pre></div>
    `;
}

// Parameter / body / responses renderers
function renderParameters(params) {
    if (!params.length) return '<div class="muted">No parameters</div>';
    return '<table><thead><tr><th>Name</th><th>In</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>' +
        params.map(p => `<tr><td>${p.name}</td><td>${p.in}</td><td>${p.schema?.type || ''}</td><td>${p.required ? "yes" : "no"}</td><td>${p.description || ''}</td></tr>`).join('') +
        '</tbody></table>';
}

function renderRequestBody(body) {
    if (!body) return '<div class="muted">No body</div>';
    const content = body.content || {};
    const mts = Object.keys(content);
    if (!mts.length) return '<div class="muted">No body</div>';
    return mts.map(mt => `<div style="margin-bottom:8px"><div class="muted">${mt}</div>${JSON.stringify(content[mt].schema, null, 2)}</div>`).join('');
}

function renderResponses(res) {
    if (!res) return '<div class="muted">No responses</div>';
    return '<table><thead><tr><th>Code</th><th>Description</th><th>Schema</th></tr></thead><tbody>' +
        Object.keys(res).map(code => `<tr><td>${code}</td><td>${res[code].description || ''}</td><td>${JSON.stringify(res[code]?.content?.['application/json']?.schema || {}, null, 2)}</td></tr>`).join('') +
        '</tbody></table>';
}

// Left menu options
document.querySelectorAll(".menu-item").forEach(btn => {
    btn.addEventListener("click", () => {
        const view = btn.dataset.view;
        if (view === "formatted" && openapi) content.innerHTML = `<h2>Formatted Spec</h2><pre class="json">${JSON.stringify(openapi, null, 2)}</pre>`;
        if (view === "file") content.innerHTML = `<h2>openapi.json File Contents</h2><pre class="json">${rawSpec}</pre>`;
    });
});