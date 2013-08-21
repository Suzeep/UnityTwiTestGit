using UnityEngine;
using System.Collections;

public class SceneDemo : MonoBehaviour
{
    //------------------------------------------------------------
    // Awake
    //------------------------------------------------------------
    void Awake()
    {
        m_LoginRect = new Rect(
            Screen.width * 0.10f,
            Screen.height * 0.25f,
            Screen.width * 0.80f,
            Screen.height * 0.10f
        );
        m_PinInputRect = new Rect(
            Screen.width * 0.10f,
            Screen.height * 0.37f,
            Screen.width * 0.6f,
            Screen.height * 0.05f
        );
        m_PinEnterRect = new Rect(
            Screen.width * 0.72f,
            Screen.height * 0.37f,
            Screen.width * 0.18f,
            Screen.height * 0.05f
        );
        m_TweetInputRect = new Rect(
            Screen.width * 0.10f,
            Screen.height * 0.50f,
            Screen.width * 0.60f,
            Screen.height * 0.05f
        );
        m_TweetPostRect = new Rect(
            Screen.width * 0.72f,
            Screen.height * 0.50f,
            Screen.width * 0.18f,
            Screen.height * 0.05f
        );
    }
    //------------------------------------------------------------
    // Start
    //------------------------------------------------------------
    void Start()
    {
    }
    //------------------------------------------------------------
    // Update
    //------------------------------------------------------------
    void Update()
    {
    }
    //------------------------------------------------------------
    // OnGUI
    //------------------------------------------------------------
    void OnGUI()
    {
        // 登録用ボタン（リクエストトークンの取得）
        if( GUI.Button( m_LoginRect, "Get Request Token" ) )
        {
            SysTwitter.I.GetRequestToken();
        }

        // 入力されたPINを取得
        m_PIN = GUI.TextField( m_PinInputRect, m_PIN );
        // PINコードを元にアクセストークンの取得
        if( GUI.Button( m_PinEnterRect, "Enter PIN" ) )
        {
            SysTwitter.I.GetAccessToken( m_PIN );
        }

        // ツイート
        m_TweetText = GUI.TextField( m_TweetInputRect, m_TweetText );
        if( GUI.Button( m_TweetPostRect, "Post Tweet" ) )
        {
            SysTwitter.I.PostTweet( m_TweetText );
        }
    }

    //------------------------------------------------------------
    // member
    //------------------------------------------------------------
    public Rect     m_LoginRect;
    public Rect     m_PinInputRect;
    public Rect     m_PinEnterRect;
    public Rect     m_TweetInputRect;
    public Rect     m_TweetPostRect;

    public string   m_PIN;
    public string   m_TweetText;
}
//================================================================================
// End of File
//================================================================================
