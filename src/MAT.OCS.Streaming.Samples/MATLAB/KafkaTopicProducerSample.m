
classdef KafkaTopicProducerSample < handle

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
        function ctor = KafkaTopicProducerSample(pathToBinFolder, brokerList, outputTopicName)
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

            outputFeed = output.DataOutput.BindDefaultFeed();
            output.SessionOutput.AutoHeartbeatInterval = TimeSpan.FromSeconds(1);
            
            output.SessionOutput.SessionState = StreamSessionState.Open;
            output.SessionOutput.SessionStart = start.Date;    
            output.SessionOutput.SendSession();
           
            delay = 1000 / frequency;
            data = outputFeed.MakeTelemetryData(1, epoch);
            param = data.Parameters(1);
            
            for step=1:10000
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
end