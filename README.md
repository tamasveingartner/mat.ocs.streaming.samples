# ATLAS Advanced Streaming Samples

![Build Status](https://mat-ocs.visualstudio.com/Telemetry%20Analytics%20Platform/_apis/build/status/MAT.OCS.Streaming/Streaming%20Samples?branchName=develop)

Table of Contents
=================
<!--ts-->
* [Introduction - MAT.OCS.Streaming library](/README.md)
* C# samples
    * [Writing data](/docs/CSharp/WritingData.md)
    * [Reading data](/docs/CSharp/ReadingData.md)
<!--te-->

# Introduction - MAT.OCS.Streaming Library

This API provides infrastructure for streaming data around the ATLAS technology platform. 

Using this API, you can: 
* Subscribe to streams of engineering values - no ATLAS recorder required 
* Inject parameters and aggregates back into the ATLAS ecosystem 
* Build up complex processing pipelines that automatically process new sessions as they are generated 

With support for Apache Kafka, the streaming API also offers: 
* Late-join capability - clients can stream from the start of a live session 
* Replay of historic streams 
* Fault-tolerant, scalable broker architecture 

## API Documentation
See [v1.8 documentation](https://mclarenappliedtechnologies.github.io/mat.atlas.advancedstreams-docs/1.8/)

## Knowledgebase
Be sure to look at our support knowledgebase on Zendesk: https://mclarenappliedtechnologies.zendesk.com/

## Scope
This pre-release version of the API demonstrates the event-based messaging approach, for sessions and simple telemetry data. 

Future versions will model all ATLAS entities, and likely offer better support for aggregates and predictive models. 
