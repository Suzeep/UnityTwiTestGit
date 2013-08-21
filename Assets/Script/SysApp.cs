//#define MONO_VERSION_CHECK

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

public class SysApp : Singleton<SysApp>
{
    //------------------------------------------------------------
    // 初期化
    //------------------------------------------------------------
    protected override void Initialize()
    {
#if MONO_VERSION_CHECK
        // Monoのバージョン表示
        var type = Type.GetType("Mono.Runtime");
        if( type != null ){
            var dispalayName = type.GetMethod(
                                "GetDisplayName",
                                BindingFlags.NonPublic | BindingFlags.Static
            );
            if( dispalayName != null ){
                Debug.Log(dispalayName.Invoke(null, null));
            }
        }
#endif
    }
    //------------------------------------------------------------
    // Start
    //------------------------------------------------------------
    void Start()
    {
#if false    // ６０フレームに固定
        Application.targetFrameRate = 60;
        m_Time = GetTime();
#endif
    }
    //------------------------------------------------------------
    // Update
    //------------------------------------------------------------
    void Update()
    {
        // FPS計測
        CalcFPS();
    }
    //------------------------------------------------------------
    // OnGUI
    //------------------------------------------------------------
    void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label( m_FPS );
    }

    //------------------------------------------------------------
    // FPS計測
    //------------------------------------------------------------
    private void    CalcFPS()
    {
        double time = GetTime();

        m_FPS = string.Format("FPS:{0}", Math.Round( 1.0/(time - m_Time), 3));
        m_Time = time;  // 保持
    }
    //------------------------------------------------------------
    // 現在時間をチックから取得
    //------------------------------------------------------------
    public double   GetTime()
    {
        return (double)(System.DateTime.Now.Ticks) / (1000.0f * 1000.0f * 10.0f);
    }

    //-----------------------------------------------------
    // member
    //-----------------------------------------------------
    private string      m_FPS;
    private double      m_Time;
}
//================================================================================
// End of File
//================================================================================
