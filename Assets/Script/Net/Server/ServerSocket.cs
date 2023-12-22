using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace Net{
    // [BurstCompile]
    struct ServerSendSnapshotJob : IJobParallelFor
    {
        [ReadOnly]public NativeArray<SnapShot> SnapShot;
        public NativeArray<State> Data;
        [ReadOnly]public NativeArray<int> LastFrameSeen;
        public NetworkDriver.Concurrent driver;
        public NativeArray<NetworkConnection> connection;
        
        public void Execute(int index)
        {
            if(!connection[index].IsCreated || LastFrameSeen[index]<0)
                return;
            Header header=new Header(LastFrameSeen[index],2);
            driver.BeginSend(connection[index],out DataStreamWriter writer);
            header.Serialize(ref writer);
            SnapShot[index].Serialize(ref writer,Data);
            driver.EndSend(writer);
        }
    }
    // [BurstCompile]
    struct ServerUpdateJob : IJobParallelFor
    {
        public int InputSendSize;
        public NativeArray<int> LastFrameSeen;
        public NetworkDriver.Concurrent driver;
        public NativeArray<NetworkConnection> connections;

        public NativeQueue<ServerInputMessage>.ParallelWriter recivedInputs;

        public void Execute(int index)
        {
            if(!connections[index].IsCreated)
                return;
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[index], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    Header header=new Header();
                    header.Deserialize(ref stream);
                    #region Get GameLoop Input
                    if(header.OpCode==2)
                    {
                        int inputTick=header.frame;
                        if(LastFrameSeen[index]<inputTick || LastFrameSeen[index]-inputTick>100)
                        {
                            InputMessage message=new InputMessage();
                            for(int j=0;j<InputSendSize;j++)
                            {
                                if(LastFrameSeen[index]<inputTick || LastFrameSeen[index]-inputTick>100)
                                {
                                    message.Deserialize(ref stream);
                                    message.frame=inputTick;
                                    recivedInputs.Enqueue(new ServerInputMessage(message,index));
                                }
                                inputTick--;
                            }
                        }
                    }
                    #endregion Get GameLoop Input
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    connections[index] = default(NetworkConnection);
                }
            }
        }
    }
    public class ServerSocket : MonoBehaviour
    {
        public static NetworkDriver m_Driver;
        private static NativeArray<NetworkConnection> m_Connections;
        public static JobHandle ServerJobHandle;

        public static NativeQueue<ServerInputMessage> recivedMessages;

        public static ServerCondition cond;


        public static void sendSnapshots()
        {
            var job = new ServerSendSnapshotJob{
                SnapShot=NetBufferServer.sendSnapShots,
                Data=NetBufferServer.snapShotData,
                driver=m_Driver.ToConcurrent(),
                connection=m_Connections,
                LastFrameSeen=NetBufferServer.lastFrameSeen,
            };
            ServerJobHandle = job.Schedule(NetBufferServer.playerCount,1,ServerJobHandle);
        }
        // Start is called before the first frame update
        void Start ()
        {
            recivedMessages=new NativeQueue<ServerInputMessage>(Allocator.Persistent);
            
            NetBufferServer.start();
            cond=ServerCondition.WaitForPlayersToConnect;
            m_Connections = new NativeArray<NetworkConnection>(NetBufferServer.playerCount, Allocator.Persistent);
            // var settings = new NetworkSettings(); 
            // settings.WithNetworkConfigParameters();
            // settings.WithSecureServerParameters()

            // m_Driver = NetworkDriver.Create(new SimulatorUtility.Parameters {MaxPacketSize = NetworkParameterConstants.MTU, MaxPacketCount = 30, PacketDelayMs = 25, PacketDropPercentage = 10});
            // m_Pipeline = m_Driver.CreatePipeline(typeof(SimulatorPipelineStage));
            
            m_Driver = NetworkDriver.Create();

            var endpoint = NetworkEndPoint.AnyIpv4;
            endpoint.Port = 9000;//this should be set by command line argumants
            if (m_Driver.Bind(endpoint) != 0)
                Debug.Log("Failed to bind to port 9000");
            else
                m_Driver.Listen();

        }
        public void OnDestroy()
        {
            ServerJobHandle.Complete();
            NetBufferServer.exit();
            // Make sure we run our jobs to completion before exiting.
            if (m_Driver.IsCreated)
            {
                m_Connections.Dispose();
                m_Driver.Dispose();
                recivedMessages.Dispose();
            }
        }
        void setupForGameLoop()
        {
        }
        // Update is called once per frame
        void Update()
        {
            ServerJobHandle.Complete();
            ServerUpdateJob job = new ServerUpdateJob
            {
                driver = m_Driver.ToConcurrent(),
                connections = m_Connections,
                recivedInputs=recivedMessages.AsParallelWriter(),
                InputSendSize=NetBufferServer.inputSendSize,
                LastFrameSeen=NetBufferServer.lastFrameSeen,
            };
            ServerJobHandle = m_Driver.ScheduleUpdate(ServerJobHandle);
            ServerJobHandle = job.Schedule(m_Connections.Length,1,ServerJobHandle);
            ServerJobHandle.Complete();
            if(cond==ServerCondition.WaitForPlayersToConnect)
            {
                NetworkConnection c;
                while ((c = m_Driver.Accept()) != default(NetworkConnection))
                {
                    
                    if(!m_Connections[c.InternalId].IsCreated)
                    {
                        m_Connections[c.InternalId]=c;
                    }
                }
                
                //Auth
                // for(int i=0;i<m_Connections.Length;i++)
                // {
                //     Auth(m_Connections[i]){
                //         if authList
                //             send(1 authOpcode)
                //             m_Connections[realSlot]=
                //         else
                //             send(0 authOpcode)
                //  }

                //change this part
                for(int i=0;i<m_Connections.Length;i++)
                {
                    if(!m_Connections[i].IsCreated)
                        return;
                }
                //change this
                setupForGameLoop();
                cond=ServerCondition.GameLoop;
                Debug.Log("server goes to game loop");
            }
            else if(cond==ServerCondition.GameLoop)
            {
                
            }
            NetBufferServer.parseInput(recivedMessages);
        }     
    }
}
