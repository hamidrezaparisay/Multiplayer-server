using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Net;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using Init;

namespace Net{
public static class NetBufferServer
{
    static public int playerCount=1;

    public static int inputSendSize=5;
    public static int inputSaveSize=4*5;


    public static NativeArray<InputMessage> inputMessages;
    public static NativeArray<int> inputMessagesLength;
    public static int inputMessagesSum=0;
    public static NativeArray<State> snapShotData;
    public static NativeArray<SnapShot> sendSnapShots;
    public static NativeArray<int> lastFrameSeen;
    
    public static void start()
    {
        inputMessages=new NativeArray<InputMessage>(playerCount*inputSaveSize,Allocator.Persistent);
        inputMessagesLength=new NativeArray<int>(playerCount,Allocator.Persistent);
        clearInputMessages();
        snapShotData=new NativeArray<State>(playerCount,Allocator.Persistent);
        sendSnapShots=new NativeArray<SnapShot>(playerCount,Allocator.Persistent);
        lastFrameSeen=new NativeArray<int>(playerCount,Allocator.Persistent);
        for(int i=0;i<playerCount;i++)//use memset
        {
            lastFrameSeen[i]=-1;
        }
    }
    public static void exit()
    {
        if(snapShotData.IsCreated)snapShotData.Dispose();
        if(inputMessages.IsCreated)inputMessages.Dispose();
        if(inputMessagesLength.IsCreated)inputMessagesLength.Dispose();
        if(sendSnapShots.IsCreated)sendSnapShots.Dispose();
        if(lastFrameSeen.IsCreated)lastFrameSeen.Dispose();
    }

    public static void createSnapShots()
    {
        ServerSocket.ServerJobHandle.Complete();
        for(int i=0;i<playerCount;i++)
        {
            State temp=new State(ControllerServer.clientObjects[i].transform.position,ControllerServer.clientObjects[i].transform.rotation);
            snapShotData[i]=temp;

            Debug.Log("state:"+temp.position+" at frame "+lastFrameSeen[i]);

            SnapShot temp2=new SnapShot(new rigidBodyState
                        (ControllerServer.clientObjects[i].velocity,ControllerServer.clientObjects[i].angularVelocity));
            sendSnapShots[i]=temp2;
        }
    }
    public static void parseInput(NativeQueue<ServerInputMessage> data)
    {
        ServerInputMessage temp=new ServerInputMessage();
        while(data.TryDequeue(out temp))
        {
            addInput(temp.input,temp.clientId);
        }
    }
    public static void clearInputMessages()
    {
        for(int i=0;i<playerCount;i++)//use memset
        {
            inputMessagesLength[i]=0;
        }
    }
    private static unsafe void addAndShift(int index,InputMessage value, int clientNumber)
    {
        if(inputMessagesLength[clientNumber]+1>inputSaveSize)
            return;
        InputMessage* pointer=(InputMessage*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(inputMessages);
        UnsafeUtility.MemMove(pointer+index+1,pointer+index,(inputMessagesLength[clientNumber]-index) * UnsafeUtility.SizeOf<InputMessage>());
        
        inputMessages[index]=value;
        inputMessagesLength[clientNumber]++;
        inputMessagesSum++;
    }
    public static void addInput(InputMessage input,int clientId)
    {
        int bass=clientId*inputSaveSize;
        int j=bass+inputMessagesLength[clientId]-1;
        while(j>=bass)
        {
            if(input.frame==inputMessages[j].frame)
                return;
            if(input.frame>inputMessages[j].frame)
            {
                // Debug.Log("adding frame "+input.frame+" to position "+(j+1));
                addAndShift(j+1,input,clientId);
                return;
            }
            j--;
        }
        // Debug.Log("adding frame "+input.frame+" to position "+bass);
        addAndShift(bass,input,clientId);
    }
}
public enum ServerCondition{
    WaitForPlayersToConnect=0,
    GameLoop=1,
}
}
