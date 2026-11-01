using MixedReality.Toolkit;
using UnityEngine;

/// <summary>
/// 見ているあいだと掴んでいるあいだ、オブジェクトの色を変える（MRTK3 の StatefulInteractable 用）。
/// アイトラッキングが OFF のときは、頭の向きで「見ている」と判定される。
///
/// Changes the color while the object is looked at or grabbed (for MRTK3 StatefulInteractable, e.g. ObjectManipulator).
/// When eye tracking is off, head gaze is used to decide what you are looking at.
/// </summary>
[RequireComponent(typeof(StatefulInteractable))]
public class GazeHighlight : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField] Color normalColor = new Color(0.85f, 0.85f, 0.85f);
    [SerializeField] Color gazeColor = new Color(0.3f, 0.75f, 1f);
    [SerializeField] Color grabColor = new Color(1f, 0.6f, 0.2f);

    StatefulInteractable interactable;
    Renderer targetRenderer;
    MaterialPropertyBlock block;

    void Awake()
    {
        interactable = GetComponent<StatefulInteractable>();
        targetRenderer = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (targetRenderer == null) return;
        var color = interactable.isSelected ? grabColor
            : interactable.IsGazeHovered || interactable.IsGazePinchHovered ? gazeColor
            : normalColor;
        targetRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColor, color);
        targetRenderer.SetPropertyBlock(block);
    }
}
