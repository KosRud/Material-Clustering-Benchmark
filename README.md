# Material Clustering Benchmark

## Table of Contents

* [Documentation](#documentation)
* [Folders](#folders)
* [Screenshots](#screenshots)
* [Charts](#charts)
  * [Comparison of Algorithms](#comparison-of-algorithms)
    * [Stopping Condition](#stopping-condition)
    * [Fixed Number of Iterations per Frame](#fixed-number-of-iterations-per-frame)
   * [KHM: parameter p](#khm-parameter-p)
   * [Random Swap: Number of KM Iterations per Swap](#random-swap-number-of-km-iterations-per-swap)
   * [Subsampling](#subsampling)

## Documentation

Documentation is available [here](https://kosrud.github.io/Material-Clustering-Benchmark/html).\
See revised [texture data layout](https://kosrud.github.io/Material-Clustering-Benchmark/html/md__assets__documentation__data__layout.html).

## Folders

The folder [Unity](./Unity) contains a Unity project, which runs clustering repeatedly on a set of video files and generates reports in JSON format. The reports contain the clustering algorithm parameters and per-frame variance (in quality benchmark mode) or frame times (in speed benchmark mode). The reports are placed in `Reports` folder alongside the executable.

The folder [node-notebook](./node-notebook) contains a [Node.js REPL](https://marketplace.visualstudio.com/items?itemName=donjayamanne.typescript-notebook) notebook, which produces plots from the JSON reports.

## Screenshots

<div style="display:flex">
<img src="https://user-images.githubusercontent.com/36504423/202903483-30bd083e-47a2-4807-b110-6ff55ac4fd54.png" width="450">
<img src="https://user-images.githubusercontent.com/36504423/202916137-e31150f7-1dda-4a9a-8ccf-d9f7b2fab270.png" width="450">
<img src="https://user-images.githubusercontent.com/36504423/202916821-1257fccf-c312-4e39-9fd6-b3a1ec2ee728.png" width="450">
<img src="https://user-images.githubusercontent.com/36504423/202917046-b26856c2-d456-4c04-93f4-0e0df29b0dcd.png" width="450">

<div>

## Charts
 
### Comparison of Algorithms

#### Stopping Condition
 
<br><br>
 
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Stop-condition_video-1.png" width="450">

<br><br>

<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Stop-condition_video-2.png" width="450">

<br><br>
 
#### Fixed Number of Iterations per Frame
 
<br><br>
 
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Algorithm-convergence_video-1.png" width="450">
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Algorithm-convergence_video-2.png" width="450"> 
 
<br><br><br><br>
 
<img src="https://github.com/KosRud/Material-Clustering-Benchmark/blob/master/charts/Algorithm-peak_video-1.png" width="450">
<img src="https://github.com/KosRud/Material-Clustering-Benchmark/blob/master/charts/Algorithm-peak_video-2.png" width="450"> 
 
<br><br>
 
### KHM: Parameter p

<br><br>
 
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/KHMp_video-1.png" width="450">
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/KHMp_video-2.png" width="450">

<br><br>

We used the value `p=2.5` for other benchmarks involving KHM

### Random Swap: Number of KM Iterations per Swap

<br><br>
 
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/RS_KM-per-swap_video-1.png" width="450">
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/RS_KM-per-swap_video-2.png" width="450">

<br><br>
 
 ### Subsampling
 
 <br><br>
 
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Subsampling_video-1.png" width="450">
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Subsampling_video-2.png" width="450">
 
<br><br>
Zoomed in sections:
<br><br>
 
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Subsampling_video-1_zoom-1.png" width="450">
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Subsampling_video-1_zoom-2.png" width="450">

<br><br>

<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Subsampling_video-2_zoom-1.png" width="450">
<img src="https://raw.githubusercontent.com/KosRud/Material-Clustering-Benchmark/master/charts/Subsampling_video-2_zoom-2.png" width="450">

