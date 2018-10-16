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

# Writing Data to Kafka

This sample demonstrates how to write some random data to Kafka topic. 

It establishes a connection to Kafka, describes the DataFormat, and sends data to Kafka. 

This sample does not include atlas 10 dependencies handling, therefore cannot be consumed in Atlas10. To do that, try [ATLAS 10 Configuration](/docs/Matlab/Atlas10Configuration.md). 

For a complete listing see [here](/src/MAT.OCS.Streaming.Samples/MATLAB/KafkaTopicProducerSample.m).

## Initialization
Load the streaming assemblies: 

```matlab
NET.addAssembly([pathToBinFolder, '\MAT.OCS.Streaming.dll']);
NET.addAssembly([pathToBinFolder '\MAT.OCS.Streaming.Kafka.dll']);
```

Initialize a `DependencyClient`, and publish a DataFormat to describe the data: 

```matlab
import MAT.OCS.Streaming.*;
import MAT.OCS.Streaming.Model.DataFormat.*;

dependencyClient = HttpDependencyClient(Uri('http://server:8180/api/dependencies/'), 'dev', false);
ctor.dataFormatClient = DataFormatClient(dependencyClient);

% describe the parameters in the default feed
ctor.outputDataFormat = DataFormat.DefineFeed('').Parameter('vCar:Chassis').AtFrequency(100).BuildFormat();

% publish and generate an Id for later use
ctor.dataFormatId = ctor.dataFormatClient.PutAndIdentifyDataFormat(ctor.outputDataFormat);
```

## Connecting to Kafka
Open a connection to Kafka using KafkaStreamClient. 

```matlab
% connection string is a list of servers, without any whitespace
client = KafkaStreamClient('server1:9092,server2:9092,server3:9092');
```

## Opening an output topic
Open an `IOutputTopic` to write data into: 

```matlab
outputTopic = self.client.OpenOutputTopic(OutputTopicName);
```

## Creating a session
```matlab
start = DateTime.UtcNow;
epoch = Time.ToTelemetryTime(start);

output = SessionTelemetryDataOutput(outputTopic, self.dataFormatId, self.dataFormatClient);
output.SessionOutput.AddSessionDependency(DependencyTypes.DataFormat, self.dataFormatId);
output.SessionOutput.SessionState = StreamSessionState.Open;
output.SessionOutput.SessionStart = start.Date;
output.SessionOutput.SendSession();

outputFeed = output.DataOutput.BindDefaultFeed();
```