# Material Clustering Benchmark

## Table of Contents

* [Folders](#folders)
* [Screenshots](#screenshots)
* [Documentation](#documentation)

## Folders

The folder [Unity](./Unity) contains a Unity project, which runs clustering repeatedly on a set of video files and generates reports in JSON format. The reports contain the clustering algorithm parameters and per-frame variance (in quality benchmark mode) or frame times (in speed benchmark mode). The reports are placed in "Reports" folder alongside the executable.

The folder [node-notebook](./node-notebook) contains a [Node.js REPL](https://marketplace.visualstudio.com/items?itemName=donjayamanne.typescript-notebook) notebook, which produces plots from the JSON reports.

## Screenshots

|||
|----|----|
|![image](https://user-images.githubusercontent.com/36504423/202903483-30bd083e-47a2-4807-b110-6ff55ac4fd54.png)|![image](https://user-images.githubusercontent.com/36504423/202903522-867759d3-dee4-497d-9b4b-b85498e26813.png)|

## Documentation

Doxygen documentation is available [here](https://kosrud.github.io/Material-Clustering-Benchmark/html).
