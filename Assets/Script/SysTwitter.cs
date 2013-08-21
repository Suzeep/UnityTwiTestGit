//#define USE_CULTURE

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;             // for CultureInfo
using System.Text;
using System.Text.RegularExpressions;   // for Regex
using System.Security.Cryptography;     // for HMACSHA1

public class SysTwitter : Singleton<SysTwitter>
{
    //------------------------------------------------------------
    // 定数
    //------------------------------------------------------------
    private const string    STR_AUTH_IDX = "Authorization";

    private const string    STR_REQ_TOKEN_URL   = "https://api.twitter.com/oauth/request_token?oauth_callback=oob";
    private const string    STR_OAUTH_URL       = "http://api.twitter.com/oauth/authorize?oauth_token={0}";
    private const string    STR_ACCESS_TOKEN_URL= "https://api.twitter.com/oauth/access_token";

    private const string    STR_POST_TWEET_URL  = "https://api.twitter.com/1.1/statuses/update.json";
    public const int        MAX_TWEET_TEXT_LENGTH = 140;

    public const string     STR_PPREFS_USER_ID              = "TwitterUserID";
    public const string     STR_PPREFS_USER_NAME            = "TwitterUserScreenName";
    public const string     STR_PPREFS_USER_ATOKEN          = "TwitterUserAccessToken";
    public const string     STR_PPREFS_USER_ATOKEN_SECRET   = "TwitterUserAccessTokenSecret";

    //------------------------------------------------------------
    // 初期化
    //------------------------------------------------------------
    protected override void Initialize()
    {
        LoadUserInfo();
    }

    //------------------------------------------------------------
    // PlayerPrefsに必要情報があるかチェックしてあれば取得
    //------------------------------------------------------------
    public void     LoadUserInfo()
    {
        m_UserId            = PlayerPrefs.GetString( STR_PPREFS_USER_ID );
        m_ScreenName        = PlayerPrefs.GetString( STR_PPREFS_USER_NAME );
        m_AccessToken       = PlayerPrefs.GetString( STR_PPREFS_USER_ATOKEN );
        m_AccessTokenSecret = PlayerPrefs.GetString( STR_PPREFS_USER_ATOKEN_SECRET );

        if( !string.IsNullOrEmpty(m_UserId) &&
            !string.IsNullOrEmpty(m_ScreenName) &&
            !string.IsNullOrEmpty(m_AccessToken) &&
            !string.IsNullOrEmpty(m_AccessTokenSecret)
        )
        {
            string log = "LoadTwitterUserInfo - succeeded";
            log += "\n    UserId : " + m_UserId;
            log += "\n    ScreenName : " + m_ScreenName;
            log += "\n    Token : " + m_AccessToken;
            log += "\n    TokenSecret : " + m_AccessTokenSecret;
            Debug.Log( log );
        }
    }

    //------------------------------------------------------------
    // リクエストトークンを取得
    //------------------------------------------------------------
    public void     GetRequestToken()
    {
        if( string.IsNullOrEmpty(m_ConsumerKey) || string.IsNullOrEmpty(m_ConsumerSecret) )
        {
            Debug.LogError( "please input your consumer key and consumer secret." );
        }
        else
        {   // コルーチンでまわす
            StartCoroutine( "coGetRequestToken" );
        }

    }
    //------------------------------------------------------------
    // リクエストトークンを取得（コルーチン）
    //------------------------------------------------------------
    private IEnumerator    coGetRequestToken()
    {
        // クリア
        m_Headers.Clear();
        // ヘッダを作成
        m_Headers[STR_AUTH_IDX] = makeRequestTokenHeader();

        // ダミーバイト
        byte[] dummy = new byte[1];
        dummy[0] = 0;

        // WWW生成
        WWW web = new WWW( STR_REQ_TOKEN_URL, dummy, m_Headers );
        // 実行
        yield return web;

        // 結果をチェック
        if( !string.IsNullOrEmpty( web.error ) )
        {   // エラー
            Debug.Log( string.Format("GetRequestToken - failed. error : {0}", web.error) );
        }
        else    // エラーなし
        {
            // トークン文字列の取得
            m_ReqToken = Regex.Match( web.text, @"oauth_token=([^&]+)" ).Groups[1].Value;
            m_ReqTokenSecret = Regex.Match( web.text, @"oauth_token_secret=([^&]+)" ).Groups[1].Value;

            // トークンが正常に取得出来ていれば成功
            if( !string.IsNullOrEmpty(m_ReqToken) &&
                !string.IsNullOrEmpty(m_ReqTokenSecret) )
            {
                string log = "OnRequestTokenCallback - succeeded";
                log += "\n    Token : " + m_ReqToken;
                log += "\n    TokenSecret : " + m_ReqTokenSecret;
                Debug.Log( log );

                // 認証ページをオープン
                openAuthorizationPage( m_ReqToken );
            }
            else{   // トークン取れなかったらエラー扱い
                Debug.Log( string.Format("GetRequestToken - failed. response : {0}", web.text) );
            }
        }
    }
    //------------------------------------------------------------
    // リクエストトークン取得用ヘッダーの生成
    //------------------------------------------------------------
    public string      makeRequestTokenHeader()
    {
        // クリア
        m_HeaderParams.Clear();

        // ヘッダの準備
        m_HeaderParams.Add( "oauth_version", "1.0" );
        m_HeaderParams.Add( "oauth_nonce", GenerateNonce() );
        m_HeaderParams.Add( "oauth_timestamp", GenerateTimeStamp() );
        m_HeaderParams.Add( "oauth_signature_method", "HMAC-SHA1" );
        m_HeaderParams.Add( "oauth_consumer_key", m_ConsumerKey );
        m_HeaderParams.Add( "oauth_consumer_secret", m_ConsumerSecret );

        // リクエストトークン取得時に必要な特別な値
        m_HeaderParams.Add( "oauth_callback", "oob" );

        // パラメータにシグネチャ追加
        string signature = GenerateSignature(
            "POST",
            STR_REQ_TOKEN_URL,
            m_HeaderParams          // パラメータ
        );
        m_HeaderParams.Add( "oauth_signature", signature );

        // アルファベット順にソートしつつ必要なパラメータを選出
        SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in m_HeaderParams )
        {
            foreach( string oauth_header_param in OAUTH_HEADER_PARAMS )
            {
                if( oauth_header_param.Contains( param.Key ) ){
                    sortedParams.Add( param.Key, param.Value );
                }
            }
        }

        // ソートされたパラメータをエスケープしてガッチャンコ
        StringBuilder headerBuilder = new StringBuilder();
        bool bFirst = true;
        foreach( var item in sortedParams )
        {
            if( bFirst ){
                bFirst = false;
                headerBuilder.AppendFormat(
                    "{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value));
            }
            else{   // 2番目以降の値は , 付き
                headerBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value)
                );
            }
        }

        // 完成
        string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

        return ret;
    }

    //------------------------------------------------------------
    // アクセストークンを取得
    //------------------------------------------------------------
    public void     GetAccessToken( string pin )
    {
        // コルーチンでまわす
        StartCoroutine( coGetAccessToken(pin) );
    }
    //------------------------------------------------------------
    // リクエストトークンを取得（コルーチン）
    //------------------------------------------------------------
    private IEnumerator     coGetAccessToken( string pin )
    {
        // クリア
        m_Headers.Clear();
        // ヘッダを作成
        m_Headers[STR_AUTH_IDX] = makeAccessTokenHeader( pin );

        // ダミーバイト
        byte[] dummy = new byte[1];
        dummy[0] = 0;

        // WWW生成
        WWW web = new WWW( STR_ACCESS_TOKEN_URL, dummy, m_Headers );
        // 実行
        yield return web;

        // 結果をチェック
        if( !string.IsNullOrEmpty( web.error ) )
        {   // エラー
            Debug.Log( string.Format("GetAccessToken - failed. error : {0}", web.error) );
        }
        else    // エラーなし
        {
            // トークン文字列とユーザーID,名前の取得
            m_AccessToken       = Regex.Match(web.text, @"oauth_token=([^&]+)").Groups[1].Value;
            m_AccessTokenSecret = Regex.Match(web.text, @"oauth_token_secret=([^&]+)").Groups[1].Value;
            m_UserId            = Regex.Match(web.text, @"user_id=([^&]+)").Groups[1].Value;
            m_ScreenName        = Regex.Match(web.text, @"screen_name=([^&]+)").Groups[1].Value;

            if( !string.IsNullOrEmpty(m_AccessToken) &&
                !string.IsNullOrEmpty(m_AccessTokenSecret) &&
                !string.IsNullOrEmpty(m_UserId) &&
                !string.IsNullOrEmpty(m_ScreenName) )
            {
                string log = "OnAccessTokenCallback - succeeded";
                log += "\n    UserId : " + m_UserId;
                log += "\n    ScreenName : " + m_ScreenName;
                log += "\n    Token : " + m_AccessToken;
                log += "\n    TokenSecret : " + m_AccessTokenSecret;
                Debug.Log( log );

                // PlayerPrefsに保存
                PlayerPrefs.SetString( STR_PPREFS_USER_ID, m_UserId );
                PlayerPrefs.SetString( STR_PPREFS_USER_NAME, m_ScreenName );
                PlayerPrefs.SetString( STR_PPREFS_USER_ATOKEN, m_AccessToken );
                PlayerPrefs.SetString( STR_PPREFS_USER_ATOKEN_SECRET, m_AccessTokenSecret );
            }
            else{   // トークン取れなかったらエラー扱い
                Debug.Log( string.Format("GetAccessToken - failed. response : {0}", web.text) );
            }
        }
    }
    //------------------------------------------------------------
    // アクセストークンを取得
    //------------------------------------------------------------
    public string   makeAccessTokenHeader( string pin )
    {
        // クリア
        m_HeaderParams.Clear();

        // ヘッダの準備
        m_HeaderParams.Add( "oauth_version", "1.0" );
        m_HeaderParams.Add( "oauth_nonce", GenerateNonce() );
        m_HeaderParams.Add( "oauth_timestamp", GenerateTimeStamp() );
        m_HeaderParams.Add( "oauth_signature_method", "HMAC-SHA1" );
        m_HeaderParams.Add( "oauth_consumer_key", m_ConsumerKey );
        m_HeaderParams.Add( "oauth_consumer_secret", m_ConsumerSecret );

        // アクセストークン取得時に必要な特別な値
        m_HeaderParams.Add( "oauth_token", m_ReqToken );
        m_HeaderParams.Add( "oauth_verifier", pin );

        // パラメータにシグネチャ追加
        string signature = GenerateSignature(
            "POST",
            STR_ACCESS_TOKEN_URL,
            m_HeaderParams          // パラメータ
        );
        m_HeaderParams.Add( "oauth_signature", signature );

        // アルファベット順にソートしつつ必要なパラメータを選出
        SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in m_HeaderParams )
        {
            foreach( string oauth_header_param in OAUTH_HEADER_PARAMS )
            {
                if( oauth_header_param.Contains( param.Key ) ){
                    sortedParams.Add( param.Key, param.Value );
                }
            }
        }

        // ソートされたパラメータをエスケープしてガッチャンコ
        StringBuilder headerBuilder = new StringBuilder();
        bool bFirst = true;
        foreach( var item in sortedParams )
        {
            if( bFirst ){
                bFirst = false;
                headerBuilder.AppendFormat(
                    "{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value));
            }
            else{   // 2番目以降の値は , 付き
                headerBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value)
                );
            }
        }

        // 完成
        string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

        return ret;
    }

    //------------------------------------------------------------
    // ツイート
    //------------------------------------------------------------
    public void     PostTweet( string text )
    {
        if( string.IsNullOrEmpty(text) || text.Length > 140 ){
            Debug.Log( string.Format( "PostTweet - text[{0}] is empty or too long.", text) );
        }
        else    // 入力内容が正しい
        {   // コルーチンでまわす
            StartCoroutine( coPostTweet(text) );
        }
    }
    //------------------------------------------------------------
    // ツイート（コルーチン）
    //------------------------------------------------------------
    private IEnumerator     coPostTweet( string text )
    {
        // WWWFormを使う
        WWWForm form = new WWWForm();

        // これが重要？
        form.AddField( "status", text );

        // ヘッダを作成
        m_Headers = form.headers;
        m_Headers[STR_AUTH_IDX] = makePostTweetHeader( text );

        // WWW生成
        WWW web = new WWW( STR_POST_TWEET_URL, form.data, m_Headers );

        yield return web;

        if( !string.IsNullOrEmpty(web.error) ){
            Debug.Log( string.Format("PostTweet - failed. {0}", web.error) );
        }
        else
        {   // エラーチェック
            string error = Regex.Match( web.text, @"<error>([^&]+)</error>" ).Groups[1].Value;
            if( !string.IsNullOrEmpty(error) ){
                Debug.Log( string.Format("PostTweet - failed. {0}", error) );
            }
            else{   // ツイート成功
                Debug.Log( "OnPostTweet - success." );
            }
        }
    }
    //------------------------------------------------------------
    // ツイート用のヘッダを作成
    //------------------------------------------------------------
    public string   makePostTweetHeader( string text )
    {
        // クリア
        m_HeaderParams.Clear();

        // ヘッダの準備
        m_HeaderParams.Add( "oauth_version", "1.0" );
        m_HeaderParams.Add( "oauth_nonce", GenerateNonce() );
        m_HeaderParams.Add( "oauth_timestamp", GenerateTimeStamp() );
        m_HeaderParams.Add( "oauth_signature_method", "HMAC-SHA1" );
        m_HeaderParams.Add( "oauth_consumer_key", m_ConsumerKey );
        m_HeaderParams.Add( "oauth_consumer_secret", m_ConsumerSecret );

        // ツイート時に必要なアクセストークンを追加
        m_HeaderParams.Add( "oauth_token", m_AccessToken );
        m_HeaderParams.Add( "oauth_token_secret", m_AccessTokenSecret );
        m_HeaderParams.Add( "status", text );

        // パラメータにシグネチャ追加
        string signature = GenerateSignature(
            "POST",
            STR_POST_TWEET_URL,
            m_HeaderParams          // パラメータ
        );
        m_HeaderParams.Add( "oauth_signature", signature );

        // アルファベット順にソートしつつ必要なパラメータを選出
        SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in m_HeaderParams )
        {
            foreach( string oauth_header_param in OAUTH_HEADER_PARAMS )
            {
                if( oauth_header_param.Contains( param.Key ) ){
                    sortedParams.Add( param.Key, param.Value );
                }
            }
        }

        // ソートされたパラメータをエスケープしてガッチャンコ
        StringBuilder headerBuilder = new StringBuilder();
        bool bFirst = true;
        foreach( var item in sortedParams )
        {
            if( bFirst ){
                bFirst = false;
                headerBuilder.AppendFormat(
                    "{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value));
            }
            else{   // 2番目以降の値は , 付き
                headerBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value)
                );
            }
        }

        // 完成
        string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

        return ret;
    }

    //------------------------------------------------------------
    // シグネチャ生成
    //------------------------------------------------------------
    public string      GenerateSignature(
        string reqType,
        string url,
        Dictionary<string, string> parameters
    )
    {
        // 計算に必要なパラメータ用のDictionary
        Dictionary<string, string> nonSecretParams = new Dictionary<string, string>();

        // パラメータチェック
        foreach( KeyValuePair<string, string> param in parameters )
        {
            bool found = false;

            foreach( string secretParam in SECRET_PARAMS )
            {
                // シークレット見つかった？
                if( secretParam == param.Key )
                {
                    found = true;
                    break;
                }
            }
            // シークレット系以外のパラメータのリスト化
            if( !found ){
                nonSecretParams.Add( param.Key, param.Value );
            }
        }

        // 計算の元となる文字列の作成
        string base_str = string.Format(
#if USE_CULTURE
            CultureInfo.InvariantCulture,
#endif
            "{0}&{1}&{2}",
            reqType,
            UrlEncode( NormalizeUrl( url ) ),
            makeStringForSignature( nonSecretParams )
        );

        // ハッシュ生成用のキー
        string key = string.Format(
#if USE_CULTURE
            CultureInfo.InvariantCulture,
#endif
            "{0}&{1}",
            UrlEncode( parameters["oauth_consumer_secret"] ),
            parameters.ContainsKey("oauth_token_secret") ? UrlEncode(parameters["oauth_token_secret"]) : string.Empty
        );

        Debug.LogError("sig key : " + key);

        // ハッシュ生成
        HMACSHA1 hmacsha1 = new HMACSHA1( Encoding.UTF8.GetBytes(key) );

        string str_signature = Convert.ToBase64String(
            hmacsha1.ComputeHash(
                Encoding.UTF8.GetBytes( base_str )
            )
        );

        return str_signature;
    }

    //------------------------------------------------------------
    // Nonce生成
    //------------------------------------------------------------f
    public string      GenerateNonce()
    {
#if false
        Random rand = new Random();
        byte[] nonce_b = new byte[64];

        rand.NextBytes( nonce_b );
        string nonce = Math.Abs( BitConverter.ToInt64(nonce_b, 0) ).ToString();

        return nonce;
#else
        // Just a simple implementation of a random number between 123400 and 9999999
        return new System.Random().Next(123400, int.MaxValue).ToString("X", CultureInfo.InvariantCulture);
#endif
    }

    //------------------------------------------------------------
    // タイムスタンプ生成
    //------------------------------------------------------------
    public string   GenerateTimeStamp()
    {
        // Default implementation of UNIX time of the current UTC time
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

        return Convert.ToInt64(
            ts.TotalSeconds,
            CultureInfo.CurrentCulture
        )
        .ToString( CultureInfo.CurrentCulture );
    }

    //------------------------------------------------------------
    // URLを正規化して返す
    //------------------------------------------------------------
    public string   NormalizeUrl( string url )
    {
        Uri uri = new Uri(url);

        string normalizedUrl = string.Format(
#if USE_CULTURE
            CultureInfo.InvariantCulture,
#endif
            "{0}://{1}",
            uri.Scheme,
            uri.Host
        );

        if( !( (uri.Scheme == "http" && uri.Port == 80) ||
               (uri.Scheme == "https" && uri.Port == 443)
            )
        )
        {
            normalizedUrl += ":" + uri.Port;
        }

        normalizedUrl += uri.AbsolutePath;

        return normalizedUrl;
    }


    //------------------------------------------------------------
    // URLエンコード(OAuthに特化)
    //------------------------------------------------------------
    public string   UrlEncode( string value )
    {
        if( string.IsNullOrEmpty(value) ){
            return string.Empty;
        }

        value = Uri.EscapeDataString( value );

        // OAuth用にアルファベットを大文字にする 例：%2F
        value = Regex.Replace(
            value,
            "(%[0-9a-f][0-9a-f])",
            c => c.Value.ToUpper()
        );

        // HttpUtility.UrlEncodeメソッドではエンコードされない文字のエンコード
        value = value
            .Replace("(", "%28")
            .Replace(")", "%29")
            .Replace("$", "%24")
            .Replace("!", "%21")
            .Replace("*", "%2A")
            .Replace("'", "%27");

        // ？
        value = value.Replace("%7E", "~");

        return value;
    }

    //------------------------------------------------------------
    // パラメータリストを繋いでシグネチャ計算用の文字列を作る
    //------------------------------------------------------------
    private string   makeStringForSignature( IEnumerable<KeyValuePair<string, string>> parameters )
    //private string   makeStringForSignature( Dictionary<string, string> parameters )
    {
        StringBuilder parameterString = new StringBuilder();

        // アルファベット順にソート
        SortedDictionary<string, string> paramsSorted = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in parameters )
        {
            paramsSorted.Add( param.Key, param.Value );
        }

        // a=b&c=d&...
        foreach( var item in paramsSorted )
        {
            if( parameterString.Length > 0 ){
                parameterString.Append("&");
            }

            parameterString.Append(
                string.Format(
#if USE_CULTURE
                    CultureInfo.InvariantCulture,
#endif
                    "{0}={1}",
                    UrlEncode( item.Key ),
                    UrlEncode( item.Value )
                )
            );
        }

        // エスケープが必要なのでエスケープして返す
        return UrlEncode( parameterString.ToString() );
    }

    //------------------------------------------------------------
    // 取得したリクエストトークンを元に認証用ページを開く
    //------------------------------------------------------------
    private void    openAuthorizationPage( string reqToken )
    {
        Application.OpenURL( string.Format(STR_OAUTH_URL, reqToken) );
    }
    //------------------------------------------------------------
    // 指定したURLをブラウザでオープン
    //------------------------------------------------------------
    public void     OpenURL( string url )
    {
        Application.OpenURL( url );
    }

    //------------------------------------------------------------
    // member
    //------------------------------------------------------------
    public string   m_ConsumerKey;
    public string   m_ConsumerSecret;

    private Dictionary<string, string>  m_HeaderParams = new Dictionary<string, string>();
    private Hashtable                   m_Headers = new Hashtable();

    private string  m_ReqToken;
    private string  m_ReqTokenSecret;
    private string  m_AccessToken;
    private string  m_AccessTokenSecret;
    private string  m_UserId;
    private string  m_ScreenName;

    private string  m_PIN;

    // シグネチャ計算に必要ないパラメータ
    private static readonly string[]    SECRET_PARAMS = new[]
    {
        "oauth_consumer_secret",
        "oauth_token_secret",
        "oauth_signature",
    };
    // OAuthに必要なヘッダパラメータ
    private static readonly string[]    OAUTH_HEADER_PARAMS = new[]
    {
        "oauth_version",
        "oauth_nonce",
        "oauth_timestamp",
        "oauth_signature_method",
        "oauth_consumer_key",
        "oauth_token",
        "oauth_verifier",
    };
}
//================================================================================
// End of File
//================================================================================
