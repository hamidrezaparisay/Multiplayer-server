using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net;
using Unity.Mathematics;

[ExecuteInEditMode]
public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        NetBufferServer.start();
        NetBufferServer.addInput(new InputMessage(0,new float2()),0);
        NetBufferServer.addInput(new InputMessage(1,new float2()),0);
        NetBufferServer.addInput(new InputMessage(2,new float2()),0);
        NetBufferServer.addInput(new InputMessage(3,new float2()),0);
        NetBufferServer.addInput(new InputMessage(4,new float2()),0);
        NetBufferServer.clearInputMessages();
        NetBufferServer.addInput(new InputMessage(0,new float2()),0);
        NetBufferServer.addInput(new InputMessage(1,new float2()),0);
        NetBufferServer.addInput(new InputMessage(2,new float2()),0);
        NetBufferServer.addInput(new InputMessage(3,new float2()),0);
        NetBufferServer.addInput(new InputMessage(4,new float2()),0);
    }
    void OnDisable()
    {
        NetBufferServer.exit();
    }
}
