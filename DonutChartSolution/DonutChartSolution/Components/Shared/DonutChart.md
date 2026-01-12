# 🍩 DonutChart Component for Blazor  
A clean, lightweight, SVG‑driven Pie/Donut chart component for Blazor applications.
Designed for clarity, testability, and ease of integration — with built‑in legends, tooltips, filtering, and click interactions.

## ✨ Features
* Pie or Donut display modes
* Automatic slice generation from key/value data
* Zero‑value filtering (ignored automatically)
* Optional legend with color‑matched items
* Hover tooltips for label/value display
* Slice click and center click events
* Deterministic HSL color generation per label

Responsive SVG layout

Fully unit‑tested with bUnit + MSTest

📦 Installation  

Add the component files to your project:
```
/Components/Shared/DonutChart.razor
/Components/Shared/DonutChart.razor.cs
/Components/Shared/DonutSlice.cs
```
Then include the namespace in your _Imports.razor:
```
@using DonutChartSolution.Components.Shared
```
🚀 Basic Usage  
```razor``` 
```<DonutChart
    Data="new Dictionary<string, int>
    {
        ["North"] = 100,
        ["South"] = 50,
        ["East"] = 25
    }"
    IncludeLabels="new[] { "North", "South", "East" }"
/> 
``` 

🍩 Donut Mode
``` 
<DonutChart
    Data="Sales"
    IncludeLabels="Sales.Keys"
    IsDonut="true"
    Thickness="40"
/>
``` 
Thickness controls the donut hole size

InnerRadius = 100 - Thickness  

📊 Pie Mode
``` 
<DonutChart
    Data="Sales"
    IncludeLabels="Sales.Keys"
    IsDonut="false"
/>
```
Pie mode has no center circle and no center click event.

🏷️ Legend
Enable the legend:  
```
<DonutChart
    Data="Sales"
    IncludeLabels="Sales.Keys"
    ShowLegend="true"
/>
```
Legend items:

* Match slice colors  
* Only show non‑zero values  
* Respect Include Labels  

🖱️ Events  
Slice Click
``` <DonutChart
    Data="Sales"
    IncludeLabels="Sales.Keys"
    OnSliceClick="label => Console.WriteLine($"Clicked {label}")"
/>
```
Slice Click  
```
<DonutChart
    Data="Sales"
    IncludeLabels="Sales.Keys"
    IsDonut="true"
    OnCenterClick="@(() => Console.WriteLine("Center clicked"))"
/>
```
🎨 Styling  

CSS classes used by the component:  

| Element            | Class             |  
|--------------------|-------------------| 
|Slice path	         |donut-slice        |
|Tooltip	         |donut-tooltip      |
|Legend container	 |donut-legend       |
|Legend item	     |donut-legend-item  |
|Donut center group	 |donut-center       |
|Donut center circle |donut-center-circle|

Override them in your site stylesheet as needed.

🧮 Data Rules
* Zero‑value entries are ignored
* ```IncludeLabels``` filters the dataset
* Colors are generated deterministically from label text
* Slice angles always sum to 360°

🧪 Unit Tests  
A full MSTest + bUnit suite is included:

* Slice math
* Geometry
* Legend behavior
* Tooltip behavior
* Donut vs Pie
* Click events
* Zero‑value filtering
* IncludeLabels filtering

Tests live in:
``` 
DonutChartSolution.Tests/Components/Shared/
```
🛠️ Parameters
|Parameter	   |Type	                 |Description                          |
|--------------|-------------------------|-------------------------------------|
|Data	       | IDictionary<string,int> | *Required*. Source values.          | 
|IncludeLabels | IEnumerable	         | Filters which labels appear.        |
|IsDonut	   | bool	                 | Enables donut mode.                 |
|Thickness	   | int	                 | Donut hole size.                    |
|ShowLegend	   | bool	                 | Shows legend below chart.           |
|OnSliceClick  | Action                  | Fired when a slice is clicked.      |
|OnCenterClick | Action	                 | Fired when donut center is clicked. |

📐 SVG Layout
``` 
    viewBox="0 0 200 220"
```
📄 License  
MIT License