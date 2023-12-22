using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Mathematics;


namespace Net
{
    public struct Header
    {
        public byte OpCode;//max=4      0=Auth,1=Init,2=Loop
        public int frame;//max=1024
        public Header(int frame,byte OpCode)
        {
            this.frame=frame;
            this.OpCode=OpCode;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteRawBits((uint)OpCode,2);
            writer.WriteRawBits((uint)frame,10);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            this.OpCode=(byte)reader.ReadRawBits(2);
            this.frame=(int)reader.ReadRawBits(10);
        }
    }
    
    //client sends&serever recives
    public struct InputMessage
    {
        public int frame;
        public float2 input;
        public InputMessage(int frame,float2 input)
        {
            this.frame=frame;
            this.input=input;
        }
        public void Deserialize(ref DataStreamReader reader)//reader have been readed header
        {
            input.x=reader.ReadFloat();
            input.y=reader.ReadFloat();
        }
    }

    //server sends&client receives
    public struct State
    {
        public float3 position;
        public quaternion rotation;
        public State(float3 position, quaternion rotation)
        {
            this.position=position;
            this.rotation=rotation;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteFloat(position.x);
            writer.WriteFloat(position.y);
            writer.WriteFloat(position.z);

            writer.WriteFloat(rotation.value.x);
            writer.WriteFloat(rotation.value.y);
            writer.WriteFloat(rotation.value.z);
            writer.WriteFloat(rotation.value.w);
        }
    }
    public struct rigidBodyState
    {
        public float3 vol;
        public float3 angularVol;
        public rigidBodyState(float3 vol, float3 angularVol)
        {
            this.vol=vol;
            this.angularVol=angularVol;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteFloat(vol.x);
            writer.WriteFloat(vol.y);
            writer.WriteFloat(vol.z);

            writer.WriteFloat(angularVol.x);
            writer.WriteFloat(angularVol.y);
            writer.WriteFloat(angularVol.z);
        }
    }
    public struct SnapShot
    {
        public rigidBodyState rbState;
        public SnapShot(rigidBodyState rbState )
        {
            this.rbState=rbState;
        }
        public void Serialize(ref DataStreamWriter writer, NativeArray<State> data)
        {
            rbState.Serialize(ref writer);
            for(int i=0;i<data.Length;i++)
            {
                data[i].Serialize(ref writer);
            }
        }
    }

    public struct ServerInputMessage
    {
        public InputMessage input;
        public int clientId;
        public ServerInputMessage(InputMessage input,int clientId)
        {
            this.input=input;
            this.clientId=clientId;
        }
    }
}

