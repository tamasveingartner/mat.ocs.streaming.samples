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

# Reading and Writing in Pipeline

Working example of reading data from kafka in pipeline and producing modified data back to kafka.

This sample combines [Reading data from Kafka](/docs/Matlab/ReadingDataFromKafka.md) and [Writing data to Kafka](/docs/Matlab/WritingDataToKafka.md). 

```matlab
classdef KafkaReadAndProduceInPipelineSample < handle

     properties(Access = private)
        dataFormatClient;

        dataFormatId;
        atlasConfId;

        outputTopic;
        outputFeed;
     end

    methods
        function ctor = KafkaReadAndProduceInPipelineSample(pathToBinFolder, brokerList, inputTopicName, outputTopicName)

            ctor.BrokerList = brokerList;
            ctor.InputTopicName = inputTopicName;
            ctor.OutputTopicName = outputTopicName;

            NET.addAssembly([pathToBinFolder, '\MAT.OCS.Streaming.dll']);
            NET.addAssembly([pathToBinFolder '\MAT.OCS.Streaming.Kafka.dll']);

            import MAT.OCS.Streaming.Matlab.*;
            import MAT.OCS.Streaming.*;
            import MAT.OCS.Streaming.FileDependencyClient.*;
            import MAT.OCS.Streaming.Model.DataFormat.*;
            import MAT.OCS.Streaming.Kafka.*;
            import MAT.OCS.Streaming.IO.*;
        end 
    end

    properties(SetAccess = private)

    % Topic name for data reading from Kafka. 
    InputTopicName;

    % Name of output topic that would be pushed back to Kafka
    OutputTopicName;

    % Comma-separated list of Kafka brokers, without embedded whitespace 
    % e.g. server1:9092,server2:9092,server3:9092
    BrokerList;

    end

    methods       
        function[] = run(self)

            import System.*;

            import MAT.OCS.Streaming.*;
            import MAT.OCS.Streaming.Model.AtlasConfiguration.*;
            import MAT.OCS.Streaming.Model.*;
            import MAT.OCS.Streaming.Model.DataFormat.*;
            import MAT.OCS.Streaming.Kafka.*;

            % dependencyClient = FileDependencyClient('C:/temp/dep');
            httpDependencyClient = HttpDependencyClient(Uri('http://localhost:8180/api/dependencies/'), 'dev', false);

            client = KafkaStreamClient(self.BrokerList);

            self.dataFormatClient = DataFormatClient(httpDependencyClient);

            % Creates data format for output topic.
            outputDataFormat = self.createOutputDataFormat();

            % Send this data format to dependency service with a key that is 
            % stored in session dependency section later.
            self.dataFormatId = self.dataFormatClient.PutAndIdentifyDataFormat(outputDataFormat);

            acClient = AtlasConfigurationClient(httpDependencyClient);

            % We need this to display parameters in Atlas parameter
            % browser.
            atlasConfiguration = self.createAtlasConfiguration();
            self.atlasConfId = acClient.PutAndIdentifyAtlasConfiguration(atlasConfiguration);

            self.outputTopic = client.OpenOutputTopic(self.OutputTopicName);

            enrichPipeline = client.StreamTopic(self.InputTopicName).PollInto(@self.createStreamPipeline);   

            while(enrichPipeline.Poll(100))             
            end

            enrichPipeline.Dispose();
            self.outputTopic.Dispose();
            client.Dispose();
        end
    end

    methods(Access = protected)
        function inputDataFormat = createInputDataFormat(~)
            import MAT.OCS.Streaming.Model.DataFormat.*;
            defaultFeed = DataFormat.DefineFeed('');
            inputDataFormat = defaultFeed.Parameter('vCar:Chassis').AtFrequency(100).BuildFormat();
        end

        function outputDataFormat = createOutputDataFormat(~)
            import MAT.OCS.Streaming.Model.DataFormat.*;
            defaultFeed = DataFormat.DefineFeed('');
            outputDataFormat = defaultFeed.Parameter('vCar2:Chassis').AtFrequency(100).BuildFormat();
        end

        % This string would identify consumer of data from Kafka.
        function consumerGroup = getConsumerGroup(self)
            consumerGroup = 'vCar2_modeler_matlab';
        end

        function output = processData(~, dataBuffer)

            data = dataBuffer.GetData();

            vCar = data.Parameters(1);

            vCar.AvgValues = double(vCar.AvgValues) .* 2;

            disp('.');

            output = data;
        end

        % TODO: Create builder for this!
        function atlasConfiguration = createAtlasConfiguration(~)

            import MAT.OCS.Streaming.Model.AtlasConfiguration.*;

            atlasConfiguration = AtlasConfiguration;

            appGroups = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.ApplicationGroup'});

            appGroup = ApplicationGroup;

            groups = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.ParameterGroup'});

            group = ParameterGroup;

            parameters = NET.createGeneric('System.Collections.Generic.Dictionary',{'System.String', 'MAT.OCS.Streaming.Model.AtlasConfiguration.Parameter'});

            parameter = Parameter;

            parameter.Name = 'vCar2';
            parameter.Units = 'kmh';
            parameter.Description = 'Double speed from MATLAB!';

            Add(parameters, 'vCar2:Chassis', parameter);


            group.Parameters = parameters;

            Add(groups, 'group', group);


            appGroup.Groups = groups;

            Add(appGroups, 'app', appGroup);

            atlasConfiguration.AppGroups = appGroups;
        end
   end

    methods(Access = private)

        % This method creates stream pipeline for each child stream.
        function polledStreamPipeline = createStreamPipeline(self, streamId)

             try

                import MAT.OCS.Streaming.IO.*;
                import MAT.OCS.Streaming.*;

                % These templates provide commonly-combined data, but you
                % can make your own.
                input =  SessionTelemetryDataInput(streamId, self.dataFormatClient);
                output = SessionTelemetryDataOutput(self.outputTopic, self.OutputTopicName, self.dataFormatClient);

                inputDataFormat = self.createInputDataFormat();

                % Data is split into named feeds; use default feed if you
                % don't need that.
                inputFeed = input.DataInput.BindDefaultFeed(inputDataFormat);
                self.outputFeed = output.DataOutput.BindDefaultFeed();

                % Add dependency references to output session that you
                % previously pushed to dependency service.
                output.SessionOutput.AddSessionDependency(DependencyTypes.AtlasConfiguration, self.atlasConfId);
                output.SessionOutput.AddSessionDependency(DependencyTypes.DataFormat, self.dataFormatId);

                % Automatically propagate session metadata and lifecycle.
                input.LinkToOutput(output.SessionOutput, @self.getIdentifier);

                % React to data.
                EventToCallbackConverter.RegisterEventHandler(inputFeed, 'DataBuffered', @self.dataBuffered);

                polledStreamPipeline = input;

                disp(streamId);
             catch ex
                 disp(ex);
             end
         end

         function result = getIdentifier(self, identifier)
             result = [char(identifier), '_', self.OutputTopicName]; 
         end

         function dataBuffered(self, ~, src)

            try
                output = self.processData(src.Buffer);

                self.outputFeed.EnqueueAndSendData(output);

            catch ex
                disp(ex);
            end
         end
    end
end
```