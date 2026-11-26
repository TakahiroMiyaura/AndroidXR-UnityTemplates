using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Android の Pointer Capture で、トラックパッド（AURA のパックなど）の指ごとの絶対座標を受け取る。
/// 権限も AndroidManifest の変更も要らない。C# から View の requestPointerCapture を呼び、
/// View.OnCapturedPointerListener（AndroidJavaProxy）で MotionEvent を受ける。
/// - 座標はパッドの値がそのまま来る（AURA のパック: X 0〜1440、Y 0〜2880。右手で持つと右端が 1440、上が 0）
/// - コールバックは Android の UI スレッドで来るので、lock して受け渡す。メインスレッドからは Read で読む
/// - キャプチャ中は OS の操作（カーソル・ランチャーを出すジェスチャ）が止まる。Stop で解くと戻る
/// - アプリが前面でなくなると Android が自動で解く。Wanted のあいだは CheckCaptured（1 秒に 1 回）で取り直す
/// - パッドの無い端末でもエラーにはならない（イベントが来ないだけ）。Editor では何もしない
///
/// Reads the absolute position of each finger on a touchpad (e.g. the AURA puck) through Android pointer capture.
/// No permission or manifest change is needed: request capture on the focused View and listen with an
/// OnCapturedPointerListener (AndroidJavaProxy). Callbacks arrive on the Android UI thread; read them with Read.
/// While captured, the system cursor and launcher gestures stop; Stop gives them back.
/// Android drops capture when the app loses focus, so call CheckCaptured about once a second while Wanted.
/// </summary>
public static class TrackpadCapture
{
    public struct Finger
    {
        public int Id;
        public float X;
        public float Y;
    }

    public const int MaxFingers = 10;

    // MotionEvent の値
    const int ActionUp = 1, ActionCancel = 3, ActionPointerUp = 6;

    static readonly object gate = new object();
    static readonly Finger[] fingers = new Finger[MaxFingers];
    static int fingerCount;
    static int eventCount;
    static bool captured;
    static bool wanted;

#if UNITY_ANDROID && !UNITY_EDITOR
    static Listener listener;
    static AndroidJavaObject view;

    // Android の UI スレッドで呼ばれる
    class Listener : AndroidJavaProxy
    {
        readonly Finger[] scratch = new Finger[MaxFingers];

        public Listener() : base("android.view.View$OnCapturedPointerListener") { }

        public bool onCapturedPointer(AndroidJavaObject v, AndroidJavaObject e)
        {
            try
            {
                var action = e.Call<int>("getActionMasked");
                var count = e.Call<int>("getPointerCount");
                // 指が離れたイベントにも、離れた指の座標が入っている。それは数えない
                var lifted = action == ActionPointerUp ? e.Call<int>("getActionIndex") : -1;
                var allUp = action == ActionUp || action == ActionCancel;
                var n = 0;
                for (var i = 0; i < count && n < MaxFingers && !allUp; i++)
                {
                    if (i == lifted) continue;
                    scratch[n].Id = e.Call<int>("getPointerId", i);
                    scratch[n].X = e.Call<float>("getX", i);
                    scratch[n].Y = e.Call<float>("getY", i);
                    n++;
                }
                lock (gate)
                {
                    System.Array.Copy(scratch, fingers, n);
                    fingerCount = n;
                    eventCount++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TrackpadCapture] event failed: " + ex.Message);
            }
            finally
            {
                e?.Dispose();
                v?.Dispose();
            }
            return true;
        }
    }
#endif

    /// <summary>Start から Stop までのあいだ true / True between Start and Stop</summary>
    public static bool Wanted => wanted;

    /// <summary>最後に確かめたとき、キャプチャを持っていたか / Whether capture was held at the last check</summary>
    public static bool Captured { get { lock (gate) return captured; } }

    /// <summary>今パッドに触れている指を into に写し、その本数を返す。events は受け取ったイベントの累計</summary>
    public static int Read(Finger[] into, out int events)
    {
        lock (gate)
        {
            var n = Mathf.Min(fingerCount, into.Length);
            System.Array.Copy(fingers, into, n);
            events = eventCount;
            return n;
        }
    }

    public static void Start()
    {
        if (wanted) return;
        wanted = true;
        lock (gate) fingerCount = 0;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidApplication.InvokeOnUIThread(() =>
        {
            try
            {
                using var window = AndroidApplication.currentActivity.Call<AndroidJavaObject>("getWindow");
                // フォーカスのある View（GameActivity では GameActivitySurfaceView）。無ければ DecorView
                view?.Dispose();
                view = window.Call<AndroidJavaObject>("getCurrentFocus") ?? window.Call<AndroidJavaObject>("getDecorView");
                listener ??= new Listener();
                view.Call("setOnCapturedPointerListener", listener);
                view.Call("requestPointerCapture");
                Debug.Log("[TrackpadCapture] requested");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TrackpadCapture] request failed: " + ex.Message);
            }
        });
#endif
    }

    public static void Stop()
    {
        if (!wanted) return;
        wanted = false;
        lock (gate)
        {
            fingerCount = 0;
            captured = false;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidApplication.InvokeOnUIThread(() =>
        {
            try
            {
                view?.Call("releasePointerCapture");
                Debug.Log("[TrackpadCapture] released");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TrackpadCapture] release failed: " + ex.Message);
            }
        });
#endif
    }

    /// <summary>
    /// キャプチャを持っているかを確かめ（結果は少し後に Captured に入る）、外れていれば取り直す。Wanted のあいだ 1 秒に 1 回呼ぶ。
    /// Checks whether capture is still held (Captured updates a moment later) and asks again if lost.
    /// </summary>
    public static void CheckCaptured()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!wanted || view == null) return;
        AndroidApplication.InvokeOnUIThread(() =>
        {
            try
            {
                if (!wanted) return;
                var has = view.Call<bool>("hasPointerCapture");
                lock (gate)
                {
                    if (has != captured) Debug.Log("[TrackpadCapture] held=" + has);
                    captured = has;
                    if (!has) fingerCount = 0;
                }
                if (!has) view.Call("requestPointerCapture");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[TrackpadCapture] check failed: " + ex.Message);
            }
        });
#endif
    }
}
