# ATLAS Advanced Streaming Samples

![Build Status](https://mat-ocs.visualstudio.com/Telemetry%20Analytics%20Platform/_apis/build/status/MAT.OCS.Streaming/Streaming%20Samples?branchName=develop)

Table of Contents
=================
<!--ts-->
* [Introduction - MAT.OCS.Streaming library](/README.md)
* C# samples
    * [Writing data](/docs/CSharp/WritingData.md)
    * [Reading data](/docs/CSharp/ReadingData.md)
* MATLAB samples
    * [Introduction to .NET MATLAB integration](/docs/Matlab/IntroToNetMatlabIntegration.md)
    * [Reading data from Kafka](/docs/Matlab/ReadingDataFromKafka.md)
    * [Writing data to Kafka](/docs/Matlab/WritingDataToKafka.md)
        * [ATLAS10 configuration](/docs/Matlab/Atlas10Configuration.md)
    * [Reading and writing in pipeline](/docs/Matlab/ReadingAndWritingInPipeline.md)
<!--te-->

# Introduction to .NET MATLAB Integration

## Loading and Importing .NET Assemblies

Load .NET assemblies using NET.addAssembly. 

```matlab
NET.addAssembly([pathToBinFolder, '\MAT.OCS.Streaming.dll']);
NET.addAssembly([pathToBinFolder, '\MAT.OCS.Streaming.Kafka.dll']);
```

**NOTE:** Assemblies cannot be unloaded until MATLAB is shut down.

Classes can be referenced by their full name, or the namespace can be imported: 

```matlab
import MAT.OCS.Streaming.*;
```

**NOTE:** The import scope is limited to the surrounding function. [More information](https://uk.mathworks.com/help/matlab/matlab_external/use-import-in-matlab-functions.html).
 

## Collections
Every non-primitive object you pass or receive from .NET code is represented by the original .NET class. For example, to get an item from a .NET list: 

```matlab
item = list.Item(0);
```
**NOTE:** .NET collections are indexed from 0. 
 

Generic collections are created as follows:

```matlab
dictionary = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'System.Double'});
```

## Arrays
.NET arrays are created as follows: 

```matlab
params = NET.createArray('System.String', 2);
params(1) = 'vCar';
params(2) = 'NGear';
```
or
```matlab
params = NET.createArray('System.String', ['vCar', 'NGear']);
```

## Registering Event Handlers
In general, use the addListener function to register .NET event handlers. If this does not work in specific instances, the following approach can be used: 

```matlab
EventToCallbackConverter.RegisterEventHandler(inputFeed, 'DataBuffered', @self.dataBuffered);
```