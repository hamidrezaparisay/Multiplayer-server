using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Net;
using Init;
public class ControllerServer : MonoBehaviour
{
    public static float timer;

    public LayerMask groundMask;
    

    float accelInput;
    float rotate;
    public InputMessage[] inputData;
    public static Rigidbody[] clientObjects;
    public static Transform[,] tires;
    public Attribute[] clientAttribute;


    RaycastHit hitData;
    

    public static ControllerServer AddThisComponent(GameObject myObject)
    {
        ControllerServer result=myObject.AddComponent<ControllerServer>();
        result.Awake();
        return result;
    }
    void Awake()
    {
        clientObjects=new Rigidbody[NetBufferServer.playerCount];
        clientAttribute=new Attribute[NetBufferServer.playerCount];
        inputData=new InputMessage[NetBufferServer.playerCount];
        tires=new Transform[NetBufferServer.playerCount,4];
        timer=0;
    }


    void Update()
    {
        if(ServerSocket.cond!=ServerCondition.GameLoop)
            return;
        timer += Time.deltaTime;
        while (timer >= Time.fixedDeltaTime)
        {
            timer -= Time.fixedDeltaTime;

            //flush all inputs and simulate the world
            int j=0;
            while(NetBufferServer.inputMessagesSum>0)
            {
                for(int i=0;i<NetBufferServer.playerCount;i++)
                {
                    if(NetBufferServer.inputMessagesLength[i]>0)
                    {
                        InputMessage temp=NetBufferServer.inputMessages[i*NetBufferServer.inputSaveSize+j];
                        NetBufferServer.lastFrameSeen[i]=temp.frame;
                        AddForces(temp,i);
                        NetBufferServer.inputMessagesSum--;
                    }
                }
                Physics.Simulate(Time.fixedDeltaTime);
                j++;
            }
            NetBufferServer.clearInputMessages();
            
            NetBufferServer.createSnapShots();
            ServerSocket.sendSnapshots();
        }
    }
    void AddForces(InputMessage mInput,int clientIndex)
    {
        float2 input=mInput.input;
        Attribute att=clientAttribute[clientIndex];
        accelInput=input.y * att.maxSpeed ;
        rotate=input.x * att.maxAngle;
        tires[clientIndex,0].localRotation=Quaternion.Euler(0,rotate,0);
        tires[clientIndex,1].localRotation=Quaternion.Euler(0,rotate,0);

        for(int i=0;i<4;i++)
        {
            if(Physics.Raycast(tires[clientIndex,i].position,-tires[clientIndex,i].up,out hitData, 1.5f,groundMask))
            {
                float3 springDir=tires[clientIndex,i].up;
                float3 tireWorldVel=clientObjects[clientIndex].GetPointVelocity(tires[clientIndex,i].position);
                float offset=att.susRest-hitData.distance;
                float vel=math.dot(springDir,tireWorldVel);
                float force=(offset*att.springStr) - (vel * att.springDamp);
                clientObjects[clientIndex].AddForceAtPosition(springDir*force,tires[clientIndex,i].position);
                
                float3 accelDir=tires[clientIndex,i].forward;
                if(Mathf.Abs(accelInput)>0.0f)
                {
                    float carSpeed=math.dot(transform.forward,clientObjects[clientIndex].velocity);
                    float torque=Mathf.Clamp01(carSpeed/att.maxSpeed);
                    torque=att.powerCurve.Evaluate(torque)*accelInput;
                    clientObjects[clientIndex].AddForceAtPosition(accelDir*torque,tires[clientIndex,i].position);
                }
                else{
                    clientObjects[clientIndex].AddForceAtPosition(-clientObjects[clientIndex].velocity/10,tires[clientIndex,i].position);
                }

                float3 steerDir=tires[clientIndex,i].right;
                tireWorldVel=clientObjects[clientIndex].GetPointVelocity(tires[clientIndex,i].position);
                float steerVel=math.dot(steerDir,tireWorldVel);
                float desiredVelChange=-steerVel * att.tireGripFactor;
                float desiredAccel=desiredVelChange/Time.fixedDeltaTime;
                clientObjects[clientIndex].AddForceAtPosition(steerDir*desiredAccel,tires[clientIndex,i].position);
            }
        }
    }
    
}
