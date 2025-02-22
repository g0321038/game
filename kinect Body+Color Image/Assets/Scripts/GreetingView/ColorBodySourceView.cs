﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using UnityEngine.SceneManagement;

public class ColorBodySourceView : MonoBehaviour 
{
    public Material BoneMaterial;
    public GameObject BodySourceManager;
    public Camera ConvertCamera;
    public float nowtime;

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    private Kinect.CoordinateMapper _CoordinateMapper;
    private int _KinectWidth = 1920;
    private int _KinectHeight = 1080;

    private float counttime;
    private int countflag;

    public Dictionary<ulong, GameObject> GetBodies()
    {
        return _Bodies;
    }

    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    void Start()
    {
        nowtime = 0;
    }
    
    void Update () 
    {
        nowtime += Time.deltaTime;

        if (BodySourceManager == null)
        {
            return;
        }
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        if (_CoordinateMapper == null)
        {
            _CoordinateMapper = _BodyManager.Sensor.CoordinateMapper;
        }

        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
              }
                
            if(body.IsTracked)
            {
                trackedIds.Add (body.TrackingId);
            }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach (var body in data)
        {
            if(body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }

                RefreshBodyObject(body, _Bodies[body.TrackingId]);
                if (TouchArea(body, _Bodies[body.TrackingId], Kinect.JointType.HandRight))
                {
                    if(countflag == 0)
                    {
                        counttime = 0;
                        countflag = 1;
                    }
                    else
                    {
                        counttime += Time.deltaTime;
                    }
                    
                    if(counttime >= 3.0)
                    {
                        SceneManager.LoadScene("Result");
                    }

                    //Debug.Log(counttime);
                }
                else
                {
                    countflag = 0;
                }
            }
        }
    }
    
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);
        body.transform.parent = GameObject.Find("Bodies").transform;
        
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            //lr.SetVertexCount(2);
            lr.positionCount = 2;
            lr.material = BoneMaterial;
            //lr.SetWidth(0.05f, 0.05f);
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;

            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
        }
        
        return body;
    }
    
    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;
            
            if(_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }
            
            Transform jointObj = bodyObject.transform.Find(jt.ToString());

            jointObj.localPosition = GetVector3FromJoint(sourceJoint);
            //jointObj.transform.position = GetVector3FromJoint(sourceJoint);

            LineRenderer lr = jointObj.GetComponent<LineRenderer>();
            if(targetJoint.HasValue)
            {
                //Debug.Log(GetVector3FromJoint(sourceJoint));

                lr.SetPosition(0, jointObj.localPosition);
                lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
                //lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
                lr.startColor = GetColorForState(sourceJoint.TrackingState);
                lr.endColor = GetColorForState(targetJoint.Value.TrackingState);
            }
            else
            {
                lr.enabled = false;
            }
        }
    }
    
    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }

    private Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        var valid = joint.TrackingState != Kinect.TrackingState.NotTracked;
        if (ConvertCamera != null || valid)
        {
            // KinectのCamera座標系(3次元)をColor座標系(2次元)に変換する
            var point = _CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            var point2 = new Vector3(point.X, point.Y, 0);
            if ((0 <= point2.x) && (point2.x < _KinectWidth) &&
                 (0 <= point2.y) && (point2.y < _KinectHeight))
            {

                // スクリーンサイズで調整(Kinect->Unity)
                point2.x = point2.x * Screen.width / _KinectWidth;
                point2.y = point2.y * Screen.height / _KinectHeight;

                // Unityのワールド座標系(3次元)に変換
                var colorPoint3 = ConvertCamera.ScreenToWorldPoint(point2);

                // 座標の調整
                // Y座標は逆、Z座標は-1にする(Xもミラー状態によって逆にする必要あり)
                colorPoint3.y *= -1;
                colorPoint3.z = -1;

                return colorPoint3;
            }
        }

        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, -1);
    }

    private bool TouchArea(Kinect.Body body, GameObject bodyObject, Kinect.JointType jt)
    {
        Transform jointObj = bodyObject.transform.Find(jt.ToString());
        var position = new Vector3(0, 0, 0);
        position = jointObj.localPosition;
        //Debug.Log(position);

        if ((position.x >= 4 && position.x <= 6) && (position.y >= 3 && position.y <= 4))
        {

            return true;
        }
        else
        {
            return false;
        }
    }
}
