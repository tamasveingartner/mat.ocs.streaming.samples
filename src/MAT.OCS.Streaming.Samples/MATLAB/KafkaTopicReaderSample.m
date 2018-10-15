
classdef KafkaTopicReaderSample < handle
    
     properties(Access = private)      
        outputTopic;
        outputFeed;
        dataFormatClient;
     end

    methods
        function ctor = KafkaTopicReaderSample(pathToBinFolder, brokerList, inputTopicName)
            ctor.BrokerList = brokerList;
            ctor.InputTopicName = inputTopicName;

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
    
    % Comma-separated list of Kafka brokers, without embedded whitespace 
    % e.g. server1:9092,server2:9092,server3:9092
    BrokerList;
    
    end
    
    methods       
        function[] = run(self)

            import System.*;
            
            import MAT.OCS.Streaming.*;
            import MAT.OCS.Streaming.Model.*;
            import MAT.OCS.Streaming.Model.DataFormat.*;
            import MAT.OCS.Streaming.Kafka.*;

            client = KafkaStreamClient(self.BrokerList);
            
            % dependencyClient = FileDependencyClient('C:/temp/dep');
            dependencyClient = HttpDependencyClient(Uri('http://localhost:8180/api/dependencies/'), 'dev', false);
            
            self.dataFormatClient = DataFormatClient(dependencyClient);

            pipeline = client.StreamTopic(self.InputTopicName).PollInto(@self.onNewStream);
            
            while(pipeline.Poll(100))             
            end
            
            pipeline.Dispose();
            client.Dispose();
        end
    end


    methods(Access = private)
      
        % This method creates stream pipeline for each child stream.
        function polledStreamPipeline = onNewStream(self, streamId)
             try
                import MAT.OCS.Streaming.IO.*;
                import MAT.OCS.Streaming.*;
                import MAT.OCS.Streaming.Model.DataFormat.*;
                
                input =  SessionTelemetryDataInput(streamId, self.dataFormatClient);             

                defaultFeed = DataFormat.DefineFeed('');
                inputDataFormat = defaultFeed.Parameter('vCar:Chassis').AtFrequency(10).BuildFormat();
                
                % Data is split into named feeds; use default feed if you
                % don't need that.
                inputFeed = input.DataInput.BindDefaultFeed(inputDataFormat);                              
                  
                EventToCallbackConverter.RegisterEventHandler(inputFeed, ...
                    'DataBuffered', @self.dataBuffered);
                
                polledStreamPipeline = input;
                
                disp(streamId);
             catch ex
                 disp(ex);
             end
         end
        
         function dataBuffered(~, ~, src)

            try
                
                data = src.Buffer.GetData();
                vCar = data.Parameters(1);

                for i=1:vCar.AvgValues.Length
                    disp(vCar.AvgValues(i));
                end
            
            catch ex
                disp(ex);
            end
         end 
    end
end
