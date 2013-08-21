using UnityEngine;
using System.Collections.Generic;

//================================================================================
// class SysInput
//================================================================================
public class SysInput : Singleton<SysInput>
{
    //------------------------------------------------------------
    // 初期化
    //------------------------------------------------------------
    protected override void Initialize()
    {
    }
    //------------------------------------------------------------
    // Start
    //------------------------------------------------------------
    void Start()
    {
        m_State = 0;
    }
    //------------------------------------------------------------
    // Update
    //------------------------------------------------------------
    void Update()
    {
        m_Old = m_On;

        int on = 0;

#if UNITY_EDITOR
        switch( m_State )
        {
        case 0:
            if( Input.GetMouseButtonDown(0) )
            {
                m_State = 1;

                on |= 1;
                m_TouchPos = Input.mousePosition;
                m_TouchPosOld = m_TouchPos;
                // calc delta
                m_TouchDeltaPos = m_TouchPos - m_TouchPosOld;
                m_DeltaX = m_TouchDeltaPos.x / Screen.width;
                m_DeltaY = m_TouchDeltaPos.y / Screen.height;
            }
            break;
        case 1:
            if( Input.GetMouseButton(0) ){
                on |= 1;
            }else{
                m_State = 0;
            }
            m_TouchPosOld = m_TouchPos;
            m_TouchPos = Input.mousePosition;
            // calc delta
            m_TouchDeltaPos = m_TouchPos - m_TouchPosOld;
            m_DeltaX = m_TouchDeltaPos.x / Screen.width;
            m_DeltaY = m_TouchDeltaPos.y / Screen.height;
            break;
        }
#else
        switch( m_State )
        {
        case 0:
            if( Input.touchCount > 0 )
            {
                m_State = 1;

                on |= 1;
                m_TouchPos = Input.mousePosition;
                m_TouchPosOld = m_TouchPos;
                // calc delta
                m_TouchDeltaPos = m_TouchPos - m_TouchPosOld;
                m_DeltaX = m_TouchDeltaPos.x / Screen.width;
                m_DeltaY = m_TouchDeltaPos.y / Screen.height;
            }
            break;
        case 1:
            if( Input.touchCount == 1 )
            {
                if( Input.GetTouch(0).phase == TouchPhase.Began ||
                    Input.GetTouch(0).phase == TouchPhase.Stationary ||
                    Input.GetTouch(0).phase == TouchPhase.Moved
                ){
                    on |= 1;
                }
            }
            else{
                m_State = 0;
            }

            m_TouchPosOld = m_TouchPos;
            m_TouchPos = Input.mousePosition;
            // calc delta
            m_TouchDeltaPos = m_TouchPos - m_TouchPosOld;
            m_DeltaX = m_TouchDeltaPos.x / Screen.width;
            m_DeltaY = m_TouchDeltaPos.y / Screen.height;
            break;
        }
#endif
        // set
        m_On = on;
        // make trigger
        m_Trg = m_On & ~m_Old;
        // make release
        m_Rel = m_Old & ~m_On;
    }
#if false
    //------------------------------------------------------------
    // OnGUI
    //------------------------------------------------------------
    void OnGUI()
    {
        // DeltaX Check
        int base_y = 16;
        GUI.Label( new Rect( 200,base_y+32,500,40 ), "DeltaX:" + m_DeltaX, m_LabelStyle );
        GUI.Label( new Rect( 200,base_y+48,500,40 ), "DeltaY:" + m_DeltaY, m_LabelStyle );

        // Reset Button
        if( GUI.RepeatButton(m_ResetRect,"Reset") )
        {
            Application.LoadLevel(Application.loadedLevelName);
        }
    }
#endif

    //------------------------------------------------------------
    // トリガー（押された瞬間）取得
    //------------------------------------------------------------
    public int  GetTrg()
    {
        return m_Trg;
    }
    //------------------------------------------------------------
    // リリース（離した瞬間）取得
    //------------------------------------------------------------
    public int  GetRel()
    {
        return m_Rel;
    }
    //------------------------------------------------------------
    // ホールド（押しっぱ）取得
    //------------------------------------------------------------
    public int  GetOn()
    {
        return m_On;
    }
    //------------------------------------------------------------
    // タッチ位置取得
    //------------------------------------------------------------
    public Vector2  GetTouchPos()
    {
        return m_TouchPos;
    }
    //------------------------------------------------------------
    // 前のフレームとのタッチ差分ベクトル取得
    //------------------------------------------------------------
    public Vector2  GetTouchDeltaPos()
    {
        return m_TouchDeltaPos;
    }
    //------------------------------------------------------------
    // タッチ差分ベクトルのＸ成分（スクリーン比率）取得
    //------------------------------------------------------------
    public float    GetDeltaX()
    {
        return m_DeltaX;
    }
    //------------------------------------------------------------
    // タッチ差分ベクトルのＹ成分（スクリーン比率）取得
    //------------------------------------------------------------
    public float    GetDeltaY()
    {
        return m_DeltaY;
    }

    //------------------------------------------------------------
    // member
    //------------------------------------------------------------
    private int         m_State;
    // Single Union Switch
    private int         m_On;
    private int         m_Old;
    private int         m_Trg;
    private int         m_Rel;

    private Vector2     m_TouchPos;
    private Vector2     m_TouchPosOld;
    private Vector2     m_TouchDeltaPos;

    private float       m_DeltaX;
    private float       m_DeltaY;

    // for Debug
    public GUIStyle     m_LabelStyle;
    public Rect         m_ResetRect = new Rect(10, 10, 100, 60);
}
//================================================================================
// End of File
//================================================================================
