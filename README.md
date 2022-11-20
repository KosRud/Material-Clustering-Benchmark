# Material Clustering Benchmark

## Table of Contents

* [Documentation](#documentation)
* [Folders](#folders)
* [Screenshots](#screenshots)
* [ToDo](#todo)

## Documentation

Documentation is available [here](https://kosrud.github.io/Material-Clustering-Benchmark/html).\
See revised [texture data layout](https://kosrud.github.io/Material-Clustering-Benchmark/html/md__assets__documentation__data__layout.html).

## Folders

The folder [Unity](./Unity) contains a Unity project, which runs clustering repeatedly on a set of video files and generates reports in JSON format. The reports contain the clustering algorithm parameters and per-frame variance (in quality benchmark mode) or frame times (in speed benchmark mode). The reports are placed in `Reports` folder alongside the executable.

The folder [node-notebook](./node-notebook) contains a [Node.js REPL](https://marketplace.visualstudio.com/items?itemName=donjayamanne.typescript-notebook) notebook, which produces plots from the JSON reports.

## Screenshots

<div style="display:flex">
<img src="https://user-images.githubusercontent.com/36504423/202903483-30bd083e-47a2-4807-b110-6ff55ac4fd54.png" width="400">
<img src="https://user-images.githubusercontent.com/36504423/202916137-e31150f7-1dda-4a9a-8ccf-d9f7b2fab270.png" width="400">
<img src="https://user-images.githubusercontent.com/36504423/202916821-1257fccf-c312-4e39-9fd6-b3a1ec2ee728.png" width="400">
<div>

## ToDo

### Report Format

JSON files are getting too large. Use SQLite, or separate JSON files for every dispatch.
