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

# ATLAS 10 configuration

To make data visible and readable in Atlas10, code has to handle Atlas 10 configuration. 

## Creating Atlas 10 configuration
```matlab
function atlasConfiguration = createAtlasConfiguration(~)

import MAT.OCS.Streaming.Model.AtlasConfiguration.*;

atlasConfiguration = AtlasConfiguration;

appGroups = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.ApplicationGroup'});

appGroup = ApplicationGroup;

groups = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.ParameterGroup'});

group = ParameterGroup;

parameters = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.Parameter'});

parameter = Parameter;

parameter.Name = 'vCar';
parameter.Units = 'kmh';
parameter.Description = 'Speed from MATLAB!';

Add(parameters, 'vCar:Chassis', parameter);

group.Parameters = parameters;

Add(groups, 'group', group);

appGroup.Groups = groups;

Add(appGroups, 'app', appGroup);

atlasConfiguration.AppGroups = appGroups;
end
```

## Sending atlas configuration to dependency service
```matlab
fileDependencyClient = FileDependencyClient('C:/temp/dep');

acClient = AtlasConfigurationClient(fileDependencyClient);

% We need this to display parameters in Atlas parameter
% browser.
atlasConfiguration = ctor.createAtlasConfiguration();
acClient.PutAtlasConfiguration('AtlasCf_1', atlasConfiguration);
```

## Session dependencies
Adding Atlas 10 configuration ID to session dependencies.

```matlab
output.SessionOutput.AddSessionDependency(DependencyTypes.AtlasConfiguration, AtlasCf_1');
```

## Working sample
Working sample including Atlas 10 configuration handling.

```matlab
classdef KafkaTopicProducerSampleWithAtlas10Dep < handle

    properties(SetAccess = private)
        % Comma-separated list of Kafka brokers, without embedded whitespace 
        % e.g. server1:9092,server2:9092,server3:9092
        BrokerList;

        % Name of output topic that would be pushed back to Kafka
        OutputTopicName;
    end

    properties(Access = private)
        dataFormatId;
        outputDataFormat;
        dataFormatClient;
    end

    methods
        function ctor = KafkaTopicProducerSampleWithAtlas10Dep(pathToBinFolder, brokerList, outputTopicName)
            ctor.BrokerList = brokerList;
            ctor.OutputTopicName = outputTopicName;

            import System.*;

            import MAT.OCS.Streaming.*;
            import MAT.OCS.Streaming.Model.DataFormat.*;

            NET.addAssembly([pathToBinFolder, '\MAT.OCS.Streaming.dll']);
            NET.addAssembly([pathToBinFolder '\MAT.OCS.Streaming.Kafka.dll']);

            % dependencyClient = FileDependencyClient('C:/temp/dep');
            dependencyClient = HttpDependencyClient(Uri('http://localhost:8180/api/dependencies/'), 'dev', false);
            ctor.dataFormatClient = DataFormatClient(dependencyClient);

            ctor.dataFormatId = [ctor.OutputTopicName, '_1'];

            % Creates data format for output topic.
            defaultFeed = DataFormat.DefineFeed('');

            ctor.outputDataFormat = defaultFeed.Parameter('vCar:Chassis').AtFrequency(100).BuildFormat();

            % Send this data format to dependency service with a key that is 
            % stored in session dependency section later.
            ctor.dataFormatClient.PutDataFormat(ctor.dataFormatId, ctor.outputDataFormat);

            acClient = AtlasConfigurationClient(dependencyClient);

            % We need this to display parameters in Atlas parameter
            % browser.
            atlasConfiguration = ctor.createAtlasConfiguration();
            acClient.PutAtlasConfiguration('AtlasCf_1', atlasConfiguration);
        end 

        function produceData(self)

        import System.*;

        import MAT.OCS.Streaming.*;
        import MAT.OCS.Streaming.IO.*;
        import MAT.OCS.Streaming.Model.*;
        import MAT.OCS.Streaming.Model.DataFormat.*;
        import MAT.OCS.Streaming.Kafka.*;

        client = KafkaStreamClient(self.BrokerList);
        genTopic = client.OpenOutputTopic(self.OutputTopicName);

        frequency = 100; % Hz

        start = DateTime.UtcNow;
        epoch = Time.ToTelemetryTime(start);

        output = SessionTelemetryDataOutput(genTopic, self.dataFormatId, self.dataFormatClient);
        output.SessionOutput.AddSessionDependency(DependencyTypes.DataFormat, self.dataFormatId);
        output.SessionOutput.AddSessionDependency(DependencyTypes.AtlasConfiguration, 'AtlasCf_1');

        outputFeed = output.DataOutput.BindDefaultFeed();
        output.SessionOutput.AutoHeartbeatInterval = TimeSpan.FromSeconds(1);

        output.SessionOutput.SessionIdentifier = 'vCar_generated';

        output.SessionOutput.SessionState = StreamSessionState.Open;
        output.SessionOutput.SessionStart = start.Date;    
        output.SessionOutput.SendSession();

        delay = 1000 / frequency;
        data = outputFeed.MakeTelemetryData(1, epoch);
        param = data.Parameters(1);

        for step=1:5000
            pause(delay/1000);

            elapsedNs = step * delay * 1000000;
            data.TimestampsNanos(1) = elapsedNs;
            param.Statuses(1) = DataStatus.Sample;
            param.AvgValues(1) = step; % simple count from 1 to n

            output.SessionOutput.SessionDurationNanos = elapsedNs;
            outputFeed.EnqueueAndSendData(data);

            disp('>');
        end

        output.SessionOutput.SessionState = StreamSessionState.Closed;
        output.SessionOutput.SendSession();
        end
    end

    methods(Access=private)
        function atlasConfiguration = createAtlasConfiguration(~)

            import MAT.OCS.Streaming.Model.AtlasConfiguration.*;

            atlasConfiguration = AtlasConfiguration;

            appGroups = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.ApplicationGroup'});

            appGroup = ApplicationGroup;

            groups = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.ParameterGroup'});

            group = ParameterGroup;

            parameters = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.Parameter'});

            parameter = Parameter;

            parameter.Name = 'vCar';
            parameter.Units = 'kmh';
            parameter.Description = 'Speed from MATLAB!';

            Add(parameters, 'vCar:Chassis', parameter);

            group.Parameters = parameters;

            Add(groups, 'group', group);

            appGroup.Groups = groups;

            Add(appGroups, 'app', appGroup);

            atlasConfiguration.AppGroups = appGroups;
        end
    end
end
```