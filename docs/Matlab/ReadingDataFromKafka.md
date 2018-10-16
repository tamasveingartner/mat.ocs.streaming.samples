# ATLAS Advanced Streaming Samples

![Build Status](https://mat-ocs.visualstudio.com/Telemetry%20Analytics%20Platform/_apis/build/status/MAT.OCS.Streaming/Streaming%20Samples?branchName=develop)

Table of Contents
=================
<!--ts-->
* [Introduction](/README.md)
* C# Samples
    * [Writing Data](/docs/CSharp/WritingData.md)
    * [Reading Data](/docs/CSharp/ReadingData.md)
* MATLAB Samples
    * [Introduction to .NET MATLAB integration](/docs/Matlab/IntroToNetMatlabIntegration.md)
    * [Reading data from Kafka](/docs/Matlab/ReadingDataFromKafka.md)
    * [Writing data to Kafka](/docs/Matlab/WritingDataToKafka.md)
        * [ATLAS 10 Configuration](/docs/Matlab/Atlas10Configuration.md)
    * [Reading and writing in pipeline](/docs/Matlab/ReadingAndWritingInPipeline.md)
<!--te-->

# Reading data from Kafka

This sample demonstrates reading data from a Kafka topic. It includes establishing a connection to Kafka, handling streams and listening to data. 

Use this sample code together with data produced by the Gateway Service, or by sample [Writing data to Kafka](/docs/Matlab/WritingDataToKafka.md).

You can complete listing see [here](/src/MAT.OCS.Streaming.Samples/MATLAB/KafkaTopicReaderSample.m).

## Initialization

Load the streaming assemblies: 

```matlab
NET.addAssembly([pathToBinFolder, '\MAT.OCS.Streaming.dll']);
NET.addAssembly([pathToBinFolder '\MAT.OCS.Streaming.Kafka.dll']);
```

Import the namespaces used in this sample:

```matlab
import MAT.OCS.Streaming.*;
import MAT.OCS.Streaming.Model.*;
import MAT.OCS.Streaming.Model.DataFormat.*;
import MAT.OCS.Streaming.Kafka.*;
```

## Connecting to services
Open a connection to Kafka using `KafkaStreamClient`. 

```matlab
% connection string is a list of servers, without any whitespace
client = KafkaStreamClient('server1:9092,server2:9092,server3:9092');
```

You'll also need to use a `DependencyClient` to resolve metadata not stored in Kafka: 
```matlab
dependencyClient = HttpDependencyClient(Uri('http://server:8180/api/dependencies/'), 'dev', false);
dataFormatClient = DataFormatClient(dependencyClient);
```

## Streaming topic and reading data
To subscribe to a topic, hook a callback to handle the incoming data, and then poll the pipeline to process network traffic. 

```matlab
pipeline = client.StreamTopic(self.InputTopicName).PollInto(@self.onNewStream);
while(pipeline.Poll(100))
end
```

The callback is fired for each stream - typically a Session. There can be multiple concurrent streams, so the callback effectively virtualises the data processing for each stream. 

Data is split into named feeds (the default feed is unnamed) to carry sets of parameters at different frequencies. These parameters are described by a `DataFormat`, downloaded by the `DataFormatClient`. 

In the callback, bind a "view" - a list of parameters you expect to see, and register an event handler to handle buffered data: 

```matlab
import MAT.OCS.Streaming.IO.*;
import MAT.OCS.Streaming.*;
import MAT.OCS.Streaming.Model.DataFormat.*;

input =  SessionTelemetryDataInput(streamId, dataFormatClient);
inputFeed = input.DataInput.BindDefaultFeed('vCar:Chassis);

EventToCallbackConverter.RegisterEventHandler(inputFeed, 'DataBuffered', @self.dataBuffered);
```

**NOTE:** `EventToCallbackConverter` is used in this example to setup a callback. 

## Working with data
```matlab
data = src.Buffer.GetData();
vCar = data.Parameters(1);
for i=1:vCar.AvgValues.Length
disp(vCar.AvgValues(i));
end
```
