using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 指で押すボタン（XRI の XRPokeFilter + XRSimpleInteractable）の見た目。
/// 押し込んだ深さに合わせてキャップを奥へ動かし、触れている・押したで色を変える。
/// 押したときの処理は XRSimpleInteractable の Select Entered に付ける。
///
/// Visual for a finger-pressed button (XRI XRPokeFilter + XRSimpleInteractable).
/// Moves the cap in by the poke depth and tints it on hover / press.
/// Hook the action to the XRSimpleInteractable's Select Entered event.
/// </summary>
[RequireComponent(typeof(XRPokeFilter))]
public class PokeButtonVisual : MonoBehaviour
{
    [SerializeField] Transform cap;
    [SerializeField] float pressDepth = 0.012f;
    [SerializeField] Color normalColor = new Color(0.85f, 0.3f, 0.3f);
    [SerializeField] Color hoverColor = new Color(1f, 0.6f, 0.5f);
    [SerializeField] Color pressedColor = new Color(0.35f, 0.85f, 0.45f);

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    XRPokeFilter filter;
    XRBaseInteractable interactable;
    Renderer capRenderer;
    MaterialPropertyBlock block;
    Vector3 capStart;

    void Awake()
    {
        filter = GetComponent<XRPokeFilter>();
        interactable = GetComponent<XRBaseInteractable>();
        if (cap == null) { enabled = false; return; }
        capRenderer = cap.GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        capStart = cap.localPosition;
    }

    void Update()
    {
        var state = filter.pokeStateData;
        var strength = state != null ? state.Value.interactionStrength : 0f;
        cap.localPosition = capStart + Vector3.forward * (pressDepth * strength);

        if (capRenderer == null) return;
        var color = interactable != null && interactable.isSelected ? pressedColor
            : interactable != null && interactable.isHovered ? hoverColor
            : normalColor;
        block.SetColor(BaseColor, color);
        capRenderer.SetPropertyBlock(block);
    }
}
