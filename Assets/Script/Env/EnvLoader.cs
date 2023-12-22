using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net;
using Init;
using Unity.Mathematics;

public class EnvLoader : MonoBehaviour
{
    
    public static GameObject env;

    void Start()
    {
        initServerScene();
    }
    public void init()
    {
        env=Resources.Load<GameObject>("Env");
    }
    public void loadEnv()
    {
        Instantiate(env, new Vector3(-13.80f, -27.57f, 5.12f), Quaternion.identity);
    }
    public void initServerScene()
    {
        InitData.setup();
        GameObject controllerGo=new GameObject("Controller");

        // EnvLoader env=controllerGo.AddComponent<EnvLoader>();
        init();loadEnv();

        ControllerServer controllerServer=ControllerServer.AddThisComponent(controllerGo);
        controllerServer.groundMask=LayerMask.GetMask("ground");
        NetBufferServer.playerCount=InitData.number;
        controllerServer.clientAttribute=InitData.att;
        for(int i=0;i<NetBufferServer.playerCount;i++)
        {
            GameObject temp=GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.name="car"+i;
			temp.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("New Material");
            temp.GetComponent<BoxCollider>().enabled=false;
            CapsuleCollider collider=temp.AddComponent<CapsuleCollider>();
            collider.radius=InitData.cAtt[i].radius;collider.height=InitData.cAtt[i].height;collider.direction=2;
            Rigidbody rb=temp.AddComponent<Rigidbody>();
            rb.mass=InitData.rbAtt[i].mass;rb.drag=InitData.rbAtt[i].drag;rb.angularDrag=InitData.rbAtt[i].angularDrag;
            ControllerServer.clientObjects[i]=rb;
            temp.transform.position=new float3(-2.253231f,0.7047228f,-31.24989f);
            temp.transform.localScale=new float3(1,0.5f,2);
            for(int j=0;j<4;j++)
            {
                GameObject temp2=new GameObject("tire"+j);
                temp2.transform.parent=temp.transform;
                float3 pos=new float3();
                if(j<2)
                    pos.z=0.4f;
                else
                    pos.z=-0.4f;
                if(j%2==0)
                    pos.x=0.4f;
                else
                    pos.x=-0.4f;
                temp2.transform.localPosition=pos;
                ControllerServer.tires[i,j]=temp2.transform;
            }
            CollisionHandler.AddThisComponent(temp);
        }
        controllerGo.AddComponent<ServerSocket>();
    }
}
