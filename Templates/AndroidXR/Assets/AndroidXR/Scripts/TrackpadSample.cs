using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// トラックパッド（AURA のパックなど）で正面のキューブを動かすサンプル。2 通りの取り方を切り替えて比べる。
///
/// RAW: ON  … Android の Pointer Capture（TrackpadCapture）。指ごとの絶対座標が来る（OS の操作は止まる）
///   - 指を置いた位置から縦に動かすと大きさ、横に動かすと水平の向きが変わる（置いた位置との差で決めるので数え過ぎ・数え落としが無い）
///   - 最初に 60 単位（AURA のパックで約 2.5 mm）動いた向きで、そのタッチを縦か横に固定する
/// RAW: OFF … OpenXR のマウス（Android XR: Mouse Interaction Profile。XR_ANDROID_mouse_interaction）。click・scroll だけが来る
///   - 1 本指で触れる（click）と色が変わる。2 本指で上下になぞる（scroll.y）と、1 回なぞるごとに 15 度回る
///   - scroll は約 18 Hz でしか変わらず、同じ値が数フレーム続く。毎フレーム足すと数え過ぎるので、「1 回なぞる＝1 段」にしている
///   - パックが無ければ Input System の Mouse（Bluetooth マウスなど）を使う（左ボタン・ホイール）
///
/// パネル（指の点・ランプ・更新回数）は実行時に作る。RAW の切り替えは ToggleRaw（指で押すボタンの Select Entered につなぐ）。
///
/// Drives the cube with a touchpad (e.g. the AURA puck) and compares two ways to read it.
/// RAW ON: Android pointer capture, absolute finger positions (system gestures stop). Vertical slide = size, horizontal = yaw,
/// measured from where the finger landed, and each touch is locked to one axis after the first 60 units of travel.
/// RAW OFF: OpenXR mouse interaction (click / scroll only). Tap = next color, one two-finger swipe = one 15-degree step.
/// Falls back to Input System's Mouse when there is no puck. The panel is built at runtime.
/// </summary>
public class TrackpadSample : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("RAW: ON / OFF のボタン。ラベルをこの上に出す / The RAW toggle button (the label goes above it)")]
    [SerializeField] Transform rawButton;
    [SerializeField] bool startWithRaw;

    [Header("Panel")]
    [Tooltip("手の届く左手前に置く。RAW のボタンはパネルの右横 / Within reach, front left; the RAW button sits on its right")]
    [SerializeField] Vector3 panelPosition = new Vector3(-0.32f, 1.3f, 0.5f);
    [SerializeField] float panelYaw = -35f;
    [Tooltip("1 単位あたりの大きさ（m）。パネルは 560 x 700 単位 / Meters per canvas unit (the panel is 560 x 700 units)")]
    [SerializeField] float panelScale = 0.0007f;

    [Header("RAW (pointer capture)")]
    [Tooltip("パッドの大きさ（AURA のパック: 1440 x 2880）/ Pad size in capture units")]
    [SerializeField] Vector2 padSize = new Vector2(1440f, 2880f);
    [Tooltip("縦か横に固定するまでの移動量 / Travel before a touch locks to one axis")]
    [SerializeField] float axisLockDistance = 60f;
    [SerializeField] float minSize = 0.05f;
    [SerializeField] float maxSize = 0.5f;
    [Tooltip("最小から最大までに要る縦の移動量（パッドの縦の長さに対する割合）/ Vertical travel for min to max, as a fraction of the pad height")]
    [SerializeField] float sizeTravel = 0.6f;
    [Tooltip("パッドの横幅いっぱい動かしたときの回転 / Yaw for a full pad-width slide")]
    [SerializeField] float yawPerWidth = 180f;

    [Header("RAW OFF (mouse)")]
    [SerializeField] float stepDegrees = 15f;
    [Tooltip("scroll が 0 のままこの秒数たったら、1 回のなぞりが終わったとみなす / Gap that ends a swipe")]
    [SerializeField] float swipeGap = 0.25f;
    [Tooltip("1 回のなぞりで 1 段回るのに要る scroll の合計 / Scroll needed for one step in a swipe")]
    [SerializeField] float swipeThreshold = 0.5f;

    static readonly Color[] Palette =
    {
        new Color(0.85f, 0.85f, 0.85f), new Color(0.95f, 0.35f, 0.3f), new Color(0.3f, 0.8f, 0.4f),
        new Color(0.3f, 0.55f, 0.95f), new Color(0.95f, 0.8f, 0.25f), new Color(0.75f, 0.4f, 0.9f),
    };
    static readonly Color[] FingerColors =
    {
        new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.85f, 1f), new Color(1f, 0.45f, 0.75f), new Color(0.5f, 1f, 0.5f),
    };
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    bool raw;

    // RAW
    readonly TrackpadCapture.Finger[] fingers = new TrackpadCapture.Finger[TrackpadCapture.MaxFingers];
    int anchorId = -1;
    Vector2 anchorPos;
    float anchorSize, anchorYaw;
    enum Axis { None, Vertical, Horizontal }
    Axis axis;
    int lastEvents, rateEvents;
    float nextCheck;

    // RAW OFF
    InputDevice mouseDevice;
    ButtonControl click, secondaryClick;
    Vector2Control scroll;
    bool isPuck;
    float nextSearch;
    bool clickWas;
    Vector2 lastScroll;
    float lastScrollTime = -10f, swipeSum;
    bool swipeStepped;
    int scrollChanges;
    int colorIndex;

    // stats
    float rateTimer;
    int rate;

    // UI
    Text modeText, helpText, statsText, scrollText, buttonLabel;
    GameObject rawGroup, mouseGroup;
    RectTransform padRect;
    Image[] dots;
    Image clickLamp, secondaryLamp;
    Renderer targetRenderer;
    MaterialPropertyBlock block;
    float size;

    void Start()
    {
        if (target == null) { enabled = false; return; }
        targetRenderer = target.GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        size = target.localScale.x;
        BuildPanel();
        SetRaw(startWithRaw);
        ApplyColor();
    }

    void OnDisable()
    {
        TrackpadCapture.Stop();
    }

    /// <summary>RAW の ON / OFF を切り替える（ボタンの Select Entered から呼ぶ）/ Toggles RAW (hook to the button)</summary>
    public void ToggleRaw() => SetRaw(!raw);

    public void SetRaw(bool on)
    {
        raw = on;
        anchorId = -1;
        if (on) TrackpadCapture.Start();
        else TrackpadCapture.Stop();
        TrackpadCapture.Read(fingers, out lastEvents);
        rateEvents = lastEvents;
        rawGroup.SetActive(on);
        mouseGroup.SetActive(!on);
        modeText.text = on ? "RAW: ON  (Android pointer capture)" : "RAW: OFF  (OpenXR mouse)";
        helpText.text = on
            ? "Slide up/down: size    Slide left/right: turn\nThe first 2.5 mm of travel picks the direction.\nSystem cursor and launcher gestures are paused."
            : "Tap: next color    Two-finger swipe up/down: turn 15°\nSystem cursor and launcher gestures work as usual.";
        if (rawButton != null) helpText.text += "\nPress the button on the right to switch RAW.";
        if (buttonLabel != null) buttonLabel.text = (on ? "RAW: ON" : "RAW: OFF") + "\n<size=20>press to switch</size>";
    }

    void Update()
    {
        if (raw) UpdateRaw();
        else UpdateMouse();

        rateTimer += Time.unscaledDeltaTime;
        if (rateTimer >= 1f)
        {
            if (raw)
            {
                rate = Mathf.RoundToInt((lastEvents - rateEvents) / rateTimer);
                rateEvents = lastEvents;
            }
            else
            {
                rate = Mathf.RoundToInt(scrollChanges / rateTimer);
                scrollChanges = 0;
            }
            rateTimer = 0f;
        }
    }

    // ---------------- RAW: ON ----------------

    void UpdateRaw()
    {
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + 1f;
            TrackpadCapture.CheckCaptured();
        }

        var count = TrackpadCapture.Read(fingers, out lastEvents);
        var anchor = -1;
        for (var i = 0; i < count; i++) if (fingers[i].Id == anchorId) anchor = i;

        if (anchor < 0)
        {
            // 新しいタッチ（または操作していた指が離れた）。最初の指を基準にする
            anchorId = -1;
            if (count > 0)
            {
                anchor = 0;
                anchorId = fingers[0].Id;
                anchorPos = new Vector2(fingers[0].X, fingers[0].Y);
                anchorSize = size;
                anchorYaw = target.eulerAngles.y;
                axis = Axis.None;
            }
        }

        if (anchor >= 0)
        {
            // Y は上が 0 なので、上へ動かすと正になるよう anchor - now
            var dx = fingers[anchor].X - anchorPos.x;
            var dy = anchorPos.y - fingers[anchor].Y;
            if (axis == Axis.None && Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) >= axisLockDistance)
                axis = Mathf.Abs(dy) >= Mathf.Abs(dx) ? Axis.Vertical : Axis.Horizontal;
            if (axis == Axis.Vertical)
            {
                size = Mathf.Clamp(anchorSize + dy / (padSize.y * sizeTravel) * (maxSize - minSize), minSize, maxSize);
                target.localScale = Vector3.one * size;
            }
            else if (axis == Axis.Horizontal)
            {
                target.rotation = Quaternion.Euler(0f, anchorYaw + dx / padSize.x * yawPerWidth, 0f);
            }
        }

        for (var i = 0; i < dots.Length; i++)
        {
            var on = i < count;
            dots[i].enabled = on;
            if (!on) continue;
            var p = new Vector2(fingers[i].X / padSize.x, 1f - fingers[i].Y / padSize.y);
            var rt = dots[i].rectTransform;
            rt.anchoredPosition = new Vector2((p.x - 0.5f) * padRect.sizeDelta.x, (p.y - 0.5f) * padRect.sizeDelta.y);
            rt.localScale = Vector3.one * (i == anchor ? 1.3f : 1f);
            dots[i].color = FingerColors[i % FingerColors.Length];
        }

        var sb = new StringBuilder();
        sb.Append("updates/s: ").Append(rate).Append("    fingers: ").Append(count);
        sb.Append("\ncapture: ").Append(TrackpadCapture.Captured ? "held" : "not held (no touchpad, or waiting)");
        sb.Append("\nlock: ").Append(anchor < 0 ? "-" : axis == Axis.None ? "(deciding)" : axis.ToString());
        sb.Append("    size: ").Append((size * 100f).ToString("F0")).Append(" cm");
        statsText.text = sb.ToString();
    }

    // ---------------- RAW: OFF ----------------

    void UpdateMouse()
    {
        FindMouse();
        var c = click != null && click.isPressed;
        var s = secondaryClick != null && secondaryClick.isPressed;
        var v = scroll != null ? scroll.ReadValue() : Vector2.zero;

        if (c && !clickWas)
        {
            colorIndex = (colorIndex + 1) % Palette.Length;
            ApplyColor();
        }
        clickWas = c;

        if (v != lastScroll) scrollChanges++;
        if (isPuck)
        {
            // パック: 値が変わったときだけ足し、1 回のなぞりで 1 段だけ回す
            if (v.y != 0f)
            {
                if (Time.unscaledTime - lastScrollTime > swipeGap) { swipeSum = 0f; swipeStepped = false; }
                if (v != lastScroll) swipeSum += v.y;
                lastScrollTime = Time.unscaledTime;
                if (!swipeStepped && Mathf.Abs(swipeSum) >= swipeThreshold)
                {
                    swipeStepped = true;
                    Turn(Mathf.Sign(swipeSum));
                }
            }
        }
        else if (v.y != 0f)
        {
            // マウスのホイール: 値は毎フレームの差分なので、0 でないフレームごとに 1 段
            Turn(Mathf.Sign(v.y));
        }
        lastScroll = v;

        clickLamp.color = c ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.25f, 0.25f, 0.25f);
        secondaryLamp.color = s ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.25f, 0.25f, 0.25f);
        scrollText.text = $"scroll  x {v.x,6:F2}   y {v.y,6:F2}";
        statsText.text = $"value changes/s: {rate}\nsource: {(mouseDevice == null ? "not found (searching every second)" : mouseDevice.layout)}";
    }

    void Turn(float sign)
    {
        target.rotation = Quaternion.Euler(0f, target.eulerAngles.y + sign * stepDegrees, 0f);
    }

    void FindMouse()
    {
        // XR_ANDROID_mouse_interaction のデバイスは OpenXR のセッションが始まってから現れる。毎フレームは探さない
        if (mouseDevice != null && mouseDevice.added && (isPuck || Time.unscaledTime < nextSearch)) return;
        if (Time.unscaledTime < nextSearch) return;
        nextSearch = Time.unscaledTime + 1f;

        foreach (var d in InputSystem.devices)
        {
            if (d.layout == null || !d.layout.Contains("AndroidMouseInteraction")) continue;
            Use(d, d.TryGetChildControl("click") as ButtonControl, d.TryGetChildControl("secondaryClick") as ButtonControl,
                d.TryGetChildControl("scroll") as Vector2Control, puck: true);
            return;
        }
        // パックが無い端末: Bluetooth マウスなど（AURA のパックの Mouse は値が動かない）
        var m = Mouse.current;
        if (m != null) Use(m, m.leftButton, m.rightButton, m.scroll, puck: false);
    }

    void Use(InputDevice d, ButtonControl c, ButtonControl s, Vector2Control v, bool puck)
    {
        if (d == mouseDevice) return;
        mouseDevice = d;
        click = c;
        secondaryClick = s;
        scroll = v;
        isPuck = puck;
        Debug.Log($"[TrackpadSample] mouse source: {d.layout} ({d.displayName})");
    }

    void ApplyColor()
    {
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColor, Palette[colorIndex]);
        block.SetColor(ColorId, Palette[colorIndex]);
        targetRenderer.SetPropertyBlock(block);
    }

    // ---------------- panel ----------------

    void BuildPanel()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var circle = CircleSprite();

        var canvas = NewCanvas("Trackpad Panel", new Vector2(560f, 700f), panelScale);
        canvas.transform.SetPositionAndRotation(panelPosition, Quaternion.Euler(0f, panelYaw, 0f));
        var root = (RectTransform)canvas.transform;
        NewImage(root, "Background", Vector2.zero, root.sizeDelta, new Color(0.05f, 0.07f, 0.1f, 0.85f));
        NewText(root, font, "Title", new Vector2(0f, 310f), new Vector2(520f, 50f), 36, "Touchpad").fontStyle = FontStyle.Bold;
        modeText = NewText(root, font, "Mode", new Vector2(0f, 262f), new Vector2(520f, 40f), 26, "");
        helpText = NewText(root, font, "Help", new Vector2(0f, -225f), new Vector2(520f, 90f), 20, "");
        statsText = NewText(root, font, "Stats", new Vector2(0f, -305f), new Vector2(520f, 80f), 20, "");

        // RAW: ON — パッドの形（縦長 1:2）と指ごとの点
        rawGroup = NewGroup(root, "Raw");
        var frame = NewImage((RectTransform)rawGroup.transform, "Pad Frame", new Vector2(0f, 20f), new Vector2(184f, 364f), new Color(0.6f, 0.7f, 0.8f));
        padRect = NewImage(frame.rectTransform, "Pad", Vector2.zero, new Vector2(176f, 356f), new Color(0.12f, 0.15f, 0.2f)).rectTransform;
        NewText(padRect, font, "Top", new Vector2(0f, 160f), new Vector2(170f, 30f), 16, "top").color = new Color(0.6f, 0.65f, 0.7f);
        dots = new Image[TrackpadCapture.MaxFingers];
        for (var i = 0; i < dots.Length; i++)
        {
            dots[i] = NewImage(padRect, $"Finger {i}", Vector2.zero, new Vector2(30f, 30f), Color.white);
            dots[i].sprite = circle;
            dots[i].enabled = false;
        }

        // RAW: OFF — click / secondaryClick のランプと scroll の値
        mouseGroup = NewGroup(root, "Mouse");
        var mg = (RectTransform)mouseGroup.transform;
        clickLamp = NewImage(mg, "Click Lamp", new Vector2(-170f, 120f), new Vector2(40f, 40f), Color.gray);
        clickLamp.sprite = circle;
        NewText(mg, font, "Click Label", new Vector2(40f, 120f), new Vector2(360f, 40f), 22, "click (one finger)").alignment = TextAnchor.MiddleLeft;
        secondaryLamp = NewImage(mg, "Secondary Lamp", new Vector2(-170f, 60f), new Vector2(40f, 40f), Color.gray);
        secondaryLamp.sprite = circle;
        NewText(mg, font, "Secondary Label", new Vector2(40f, 60f), new Vector2(360f, 40f), 22, "secondaryClick (two fingers)").alignment = TextAnchor.MiddleLeft;
        scrollText = NewText(mg, font, "Scroll", new Vector2(0f, -10f), new Vector2(480f, 40f), 24, "");
        var note = NewText(mg, font, "Note", new Vector2(0f, -90f), new Vector2(480f, 80f), 18,
            "scroll changes about 18 times a second and only\nsends movement, so it is counted per swipe here.");
        note.color = new Color(0.6f, 0.65f, 0.7f);

        if (rawButton != null)
        {
            // ボタンの上のラベル
            var label = NewCanvas("RAW Button Label", new Vector2(200f, 70f), 0.001f);
            label.transform.SetParent(rawButton, false);
            label.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            NewImage((RectTransform)label.transform, "Background", Vector2.zero, new Vector2(200f, 70f), new Color(0.05f, 0.07f, 0.1f, 0.85f));
            buttonLabel = NewText((RectTransform)label.transform, font, "Label", Vector2.zero, new Vector2(200f, 70f), 30, "");
        }
    }

    // ワールド空間の Canvas（1 単位 = scale m）
    static Canvas NewCanvas(string name, Vector2 size, float scale)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one * scale;
        return canvas;
    }

    static GameObject NewGroup(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = parent.sizeDelta;
        return go;
    }

    static Image NewImage(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Text NewText(RectTransform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize, string text)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = Color.white;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    static Sprite CircleSprite()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Circle", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
            var a = Mathf.Clamp01(n / 2f - d);
            pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
